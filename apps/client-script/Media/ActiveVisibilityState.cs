using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using Hypnonema.Client.Communications;
using Hypnonema.Client.Configurations;
using Hypnonema.Client.Dui;
using Hypnonema.Client.Graphics;
using Hypnonema.Client.Media.Audio;
using Hypnonema.Client.ObjectPools;
using Hypnonema.Shared;
using Hypnonema.Shared.Communications;
using Hypnonema.Shared.Diagnostics;
using Hypnonema.Shared.Media;
using Hypnonema.Shared.Media.Audio;
using DuiEvents = Hypnonema.Shared.Dui.DuiEvents;

namespace Hypnonema.Client.Media;

// TODO: Implement Error and timeout handling for loading track
// TODO: Implement Error state
public sealed class ActiveVisibilityState : MediaPlayerVisibilityState
{
    private const int RangeCheckIntervalMs = 500;
    private const int VolumeUpdateIntervalMs = 250;

    private readonly Browser browser;
    private readonly DrawableSlot drawableSlot;
    private readonly EventSubscription[] subscriptions;

    private IAudioSystem audioSystem;

    private ActiveVisibilityState(MediaPlayer mediaPlayer, ILogger logger, Browser browser, IDrawable? drawable,
        IAudioSystem audioSystem, ITickManager tickManager, IEventManager mediaPlayerEventManager,
        Configuration configuration)
        : base(
            mediaPlayer,
            logger,
            tickManager,
            mediaPlayerEventManager,
            configuration)
    {
        this.browser = browser ?? throw new ArgumentNullException(nameof(browser), "browser must not be null");
        this.audioSystem = audioSystem ??
                           throw new ArgumentNullException(nameof(audioSystem), "audioSystem must not be null");

        drawableSlot = new DrawableSlot(mediaPlayer, browser, tickManager, logger);
        subscriptions = BuildSubscriptions();

        if (drawable != null) drawableSlot.Adopt(drawable);

        // browser.PlayerEnd += OnBrowserPlayerEnd;
        browser.PlayerError += OnBrowserPlayerError;
        browser.PlayerDuration += OnBrowserPlayerDuration;
        browser.YoutubeAdStatus += OnBrowserYoutubeAdStatus;

        TickManager.Add(OnCalculateVolume);

        RegisterEvents();

        TickManager.RunAsync(OnFirstTick);
    }

    private EventSubscription[] BuildSubscriptions()
    {
        return
        [
            new EventSubscription(MediaPlayerEvents.TrackPositionChanged,
                new Action<CommunicationMessage, ITrack>(OnMediaPlayerTrackPositionChanged)),
            new EventSubscription(MediaPlayerEvents.SyncedTrackPosition,
                new Action<CommunicationMessage, double>(OnMediaPlayerTrackSynchronized)),
            new EventSubscription(MediaPlayerEvents.Played,
                new Action<CommunicationMessage, ITrack>(OnMediaPlayerPlayedTrack)),
            new EventSubscription(MediaPlayerEvents.LoopChanged,
                new Action<CommunicationMessage, bool>(OnMediaPlayerSetLooped)),
            new EventSubscription(MediaPlayerEvents.Paused,
                new Action<CommunicationMessage, ITrack>(OnMediaPlayerPaused)),
            new EventSubscription(MediaPlayerEvents.Resumed,
                new Action<CommunicationMessage, ITrack>(OnMediaPlayerResumed)),
            new EventSubscription(MediaPlayerEvents.AdActiveChanged,
                new Action<CommunicationMessage, bool>(OnMediaPlayerAdActiveChanged)),
            new EventSubscription(MediaPlayerEvents.MutedChanged,
                new Action<CommunicationMessage, bool>(OnMediaPlayerSetMuted)),
            new EventSubscription(MediaPlayerEvents.VideoEnabled,
                new Action<CommunicationMessage>(OnMediaPlayerVideoEnabled)),
            new EventSubscription(MediaPlayerEvents.VideoDisabled,
                new Action<CommunicationMessage>(OnMediaPlayerVideoDisabled)),
            new EventSubscription(MediaPlayerEvents.RenderModeChanged,
                new Action<CommunicationMessage, RenderMode>(OnMediaPlayerRenderModeChanged)),
            new EventSubscription(MediaPlayerEvents.ScaleformSettingsChanged,
                new Action<CommunicationMessage, ScaleformRenderConfiguration>(OnMediaPlayerSetScaleformSettings)),
            new EventSubscription(MediaPlayerEvents.ScreenGeometryChanged,
                new Action<CommunicationMessage, ScaleformRenderConfiguration>(OnMediaPlayerScreenGeometryChanged)),
            new EventSubscription(MediaPlayerEvents.AudioConfigurationChanged,
                new Action<CommunicationMessage, AudioConfiguration>(OnMediaPlayerAudioConfigurationChanged)),
            new EventSubscription(MediaPlayerEvents.TrackSkipped,
                new Action<CommunicationMessage, Track, Track>(OnMediaPlayerTrackSkipped))
        ];
    }

    private void RegisterEvents()
    {
        foreach (var (@event, handler) in subscriptions) On(@event, handler);
    }

    private void UnregisterEvents()
    {
        foreach (var (@event, handler) in subscriptions) Off(@event, handler);
    }

    private void OnMediaPlayerTrackSkipped(CommunicationMessage message, Track previousTrack, Track nextTrack)
    {
        browser.LoadTrackAsync(nextTrack);
    }

    private void OnMediaPlayerSetScaleformSettings(CommunicationMessage message,
        ScaleformRenderConfiguration scaleformRenderConfiguration)
    {
        if (MediaPlayer.RenderMode == RenderMode.RenderTarget) return;
        RecreateDrawable();
    }

    // Ticket 08: a screen edit that only moved/rotated/resized the face. Nudge the live drawable in place so
    // the running media player never blinks; a rebuild (next range entry) reads the stored config anyway.
    private void OnMediaPlayerScreenGeometryChanged(CommunicationMessage message,
        ScaleformRenderConfiguration scaleformRenderConfiguration)
    {
        drawableSlot.ApplyTransform(scaleformRenderConfiguration.Transform);
    }

    private void OnMediaPlayerAudioConfigurationChanged(CommunicationMessage message, AudioConfiguration audio)
    {
        try
        {
            // Cheap: re-wraps the existing browser and media player, no DUI rebuild. Keeps playback uninterrupted
            // when a screen's audio parameters are edited in the settings menu.
            audioSystem = AudioSystemBuilder.CreateAudioSystem(browser, MediaPlayer);
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"Failed to rebuild the audio system for {MediaPlayer}; keeping the current one.");
        }
    }

    private void OnMediaPlayerPaused(CommunicationMessage message, ITrack track)
    {
        browser.SendMessage(DuiEvents.SetPaused, true);
    }

    private void OnMediaPlayerResumed(CommunicationMessage message, ITrack track)
    {
        browser.SendMessage(DuiEvents.SetPaused, false);
    }

    private void OnMediaPlayerAdActiveChanged(CommunicationMessage message, bool adActive)
    {
        browser.SendMessage(DuiEvents.SetAdActive, adActive);
    }

    private void OnMediaPlayerRenderModeChanged(CommunicationMessage message, RenderMode renderMode)
    {
        RecreateDrawable();
    }

    private void OnMediaPlayerVideoEnabled(CommunicationMessage message)
    {
        browser.SendMessage(DuiEvents.SetVideoEnabled, true);

        if (!drawableSlot.HasDrawable) RecreateDrawable();
    }

    private void OnMediaPlayerVideoDisabled(CommunicationMessage message)
    {
        browser.SendMessage(DuiEvents.SetVideoEnabled, false);

        if (drawableSlot.HasDrawable) drawableSlot.Release();
    }

    private void OnMediaPlayerSetMuted(CommunicationMessage message, bool muted)
    {
        browser.SendMessage(DuiEvents.SetMuted, muted);
    }

    private void OnMediaPlayerSetLooped(CommunicationMessage message, bool looped)
    {
        browser.SendMessage(DuiEvents.SetLooped, looped);
    }

    private void OnMediaPlayerTrackPositionChanged(CommunicationMessage message, ITrack track)
    {
        browser.SendMessage(DuiEvents.Seek, track.Position);
    }

    private void OnMediaPlayerTrackSynchronized(CommunicationMessage message, double position)
    {
        browser.SendMessage(DuiEvents.SynchronizeTime, position);
    }

    private void OnMediaPlayerPlayedTrack(CommunicationMessage message, ITrack track)
    {
        browser.LoadTrackAsync(track);
    }

    private void OnBrowserPlayerEnd(object sender, EventArgs e)
    {
        if (MediaPlayer.Queue.Count == 0)
            MediaPlayer.TransitionTo(new PassiveVisibilityState(MediaPlayer,
                Logger, TickManager, EventManager, Configuration));
    }

    private void RecreateDrawable()
    {
        if (!MediaPlayer.Video)
        {
            drawableSlot.Release();
            return;
        }

        TickManager.RunAsync(drawableSlot.RebuildAsync);
    }

    public static async Task<ActiveVisibilityState> CreateAsync(MediaPlayer context, ILogger logger,
        ITickManager tickManager, IEventManager eventManager, Configuration configuration)
    {
        var browserOptions = new BrowserPoolOptions(
            configuration.DuiUrl,
            configuration.DuiTimeout,
            configuration.DuiWidth,
            configuration.DuiHeight);

        var browser = await BrowserPool.Self.Checkout(browserOptions);

        try
        {
            var drawable = context.Video ? await DrawableBuilder.BuildDrawable(context, browser) : null;

            var audioSystem = AudioSystemBuilder.CreateAudioSystem(browser, context);

            return new ActiveVisibilityState(
                context,
                logger,
                browser,
                drawable,
                audioSystem,
                tickManager,
                eventManager,
                configuration);
        }
        catch (Exception exception)
        {
            logger.Error(exception, $"Failed to activate {context}. Returning the browser to the pool.");

            browser.Reset();
            BrowserPool.Self.CheckIn(browser);

            throw;
        }
    }

    private void OnBrowserPlayerError(object sender, ErrorEventArgs e)
    {
        //TODO: MediaPlayer.SetError(e.Reason);
    }

    private async Task OnFirstTick()
    {
        try
        {
            await browser.LoadTrackAsync(MediaPlayer.Track);

            browser.SendMessage(DuiEvents.SetLooped, MediaPlayer.Looped);
            browser.SendMessage(DuiEvents.SetMuted, MediaPlayer.Muted);
            browser.SendMessage(DuiEvents.SetPaused, MediaPlayer.Paused);
            browser.SendMessage(DuiEvents.SetVideoEnabled, MediaPlayer.Video);
            browser.SendMessage(DuiEvents.SetAdActive, MediaPlayer.AdActive);
        }
        catch (Exception exception)
        {
            Logger.Error(exception);
            // TODO: Inform user
            // TODO: Transition to error state
        }
    }

    public override void OnEnterState()
    {
        base.OnEnterState();

        EmitRpc(MediaPlayerRpcEvents.ActivatingMediaPlayer);
    }

    public override void OnExitState()
    {
        base.OnExitState();

        drawableSlot.Release();

        TickManager.Remove(OnCalculateVolume);

        browser.PlayerEnd -= OnBrowserPlayerEnd;
        browser.PlayerError -= OnBrowserPlayerError;
        browser.PlayerDuration -= OnBrowserPlayerDuration;
        browser.YoutubeAdStatus -= OnBrowserYoutubeAdStatus;

        browser.Reset();

        BrowserPool.Self.CheckIn(browser);

        UnregisterEvents();

        EmitRpc(MediaPlayerRpcEvents.DeactivatingMediaPlayer);
    }

    private void OnBrowserPlayerDuration(object sender, DurationEventArgs args)
    {
        if (args.Duration == MediaPlayer.Track.Duration) return;

        EmitRpc(MediaPlayerRpcEvents.SetDuration, args.Duration);
    }

    private void OnBrowserYoutubeAdStatus(object sender, YoutubeAdStatusEventArgs args)
    {
        EmitRpc(MediaPlayerRpcEvents.YoutubeAdStatus, args.Status);
    }

    private async Task OnCalculateVolume()
    {
        audioSystem.CalculateVolume();

        await BaseScript.Delay(VolumeUpdateIntervalMs);
    }

    public override async Task OnTick()
    {
        if (!MediaPlayer.IsInRange())
            MediaPlayer.TransitionTo(new PassiveVisibilityState(MediaPlayer, Logger, TickManager, EventManager,
                Configuration));

        await BaseScript.Delay(RangeCheckIntervalMs);
    }
}