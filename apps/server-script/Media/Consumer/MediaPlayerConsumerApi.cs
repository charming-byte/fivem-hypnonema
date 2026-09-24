using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using Hypnonema.Server.Communications;
using Hypnonema.Server.Rpc;
using Hypnonema.Shared;
using Hypnonema.Shared.Communications;
using Hypnonema.Shared.Media;
using Hypnonema.Shared.Media.Consumer;
using Hypnonema.Shared.Serialization;
using Serilog;

namespace Hypnonema.Server.Media.Consumer;

public sealed class MediaPlayerConsumerApi
{
    private readonly IEventManager eventManager;
    private readonly Dictionary<int, List<(string Event, Delegate Handler)>> eventSubscriptionsByHandle = new();

    private readonly ILogger logger;

    private readonly IMediaPlayerBuilder mediaPlayerBuilder;

    private readonly IReadOnlyList<Model> models;

    private readonly MediaPlayerRegistry registry;

    private readonly IReadOnlyList<Screen> screens;

    private readonly Action<string, object[]> triggerEvent;

    public MediaPlayerConsumerApi(
        IMediaPlayerBuilder mediaPlayerBuilder,
        MediaPlayerRegistry registry,
        IEventManager eventManager,
        ILogger logger,
        IReadOnlyList<Screen> screens,
        IReadOnlyList<Model> models,
        Action<string, object[]> triggerEvent)
    {
        this.mediaPlayerBuilder = mediaPlayerBuilder ?? throw new ArgumentNullException(nameof(mediaPlayerBuilder));
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        this.eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.screens = screens ?? throw new ArgumentNullException(nameof(screens));
        this.models = models ?? throw new ArgumentNullException(nameof(models));
        this.triggerEvent = triggerEvent ?? throw new ArgumentNullException(nameof(triggerEvent));

        this.registry.MediaPlayerAdded += OnMediaPlayerAdded;
        this.registry.MediaPlayerRemoved += OnMediaPlayerRemoved;
    }

    public int Create(string targetJson, string trackJson)
    {
        var target = Serializer.Deserialize<Target>(targetJson, Serializer.Settings)
                     ?? throw new ArgumentException("Target JSON must not be null.", nameof(targetJson));

        var trackRequest = Serializer.Deserialize<ConsumerCreateTrackRequest>(trackJson, Serializer.Settings)
                           ?? throw new ArgumentException("Track JSON must not be null.", nameof(trackJson));

        return Create(target, trackRequest.Url, trackRequest.Title, trackRequest.ThumbnailUrl);
    }

    public int Create(Target target, string url, string title, string thumbnailUrl)
    {
        var track = BuildTrack(url, title, thumbnailUrl);

        var resolvedTarget = ResolveTarget(target);

        if (resolvedTarget is ModelTarget modelTarget) EnsureRenderTargetIsFree(modelTarget);

        var mediaPlayer = mediaPlayerBuilder.Create(track, resolvedTarget);

        registry.Add(mediaPlayer);

        RpcManager.Emit(RpcEvents.CreateMediaPlayer, null, MediaPlayerDTO.From(mediaPlayer));

        return mediaPlayer.Handle.Value;
    }

    public void Enqueue(int handle, string trackJson)
    {
        var trackRequest = Serializer.Deserialize<ConsumerCreateTrackRequest>(trackJson, Serializer.Settings)
                           ?? throw new ArgumentException("Track JSON must not be null.", nameof(trackJson));

        Enqueue(handle, trackRequest.Url, trackRequest.Title, trackRequest.ThumbnailUrl);
    }

    public void Enqueue(int handle, string url, string title, string thumbnailUrl)
    {
        var track = BuildTrack(url, title, thumbnailUrl);

        RequireMediaPlayer(handle).Enqueue(track);
    }

    public void SetQueue(int handle, string tracksJson)
    {
        var trackRequests = Serializer.Deserialize<List<ConsumerCreateTrackRequest>>(tracksJson, Serializer.Settings)
                            ?? throw new ArgumentException("Tracks JSON must not be null.", nameof(tracksJson));

        var tracks = trackRequests.Select(r => BuildTrack(r.Url, r.Title, r.ThumbnailUrl)).ToList();

        SetQueue(handle, tracks);
    }

    public void SetQueue(int handle, IReadOnlyList<Track> tracks)
    {
        RequireMediaPlayer(handle).SetQueue(tracks);
    }

    public void SetPaused(int handle, bool paused)
    {
        RequireMediaPlayer(handle).SetPaused(paused);
    }

    public void Skip(int handle)
    {
        RequireMediaPlayer(handle).SkipCurrentTrack();
    }

    public void Seek(int handle, int position)
    {
        RequireMediaPlayer(handle).Seek(position);
    }

    public void SetLoop(int handle, bool looped)
    {
        RequireMediaPlayer(handle).SetLooped(looped);
    }

    public void SetMuted(int handle, bool muted)
    {
        RequireMediaPlayer(handle).SetMuted(muted);
    }

    public void SetVideoEnabled(int handle, bool enabled)
    {
        RequireMediaPlayer(handle).SetVideoEnabled(enabled);
    }

    public void SetScaleformSettings(int handle, string settingsJson)
    {
        var settings = Serializer.Deserialize<ScaleformRenderConfiguration>(settingsJson, Serializer.Settings)
                       ?? throw new ArgumentException("Scaleform settings JSON must not be null.",
                           nameof(settingsJson));

        RequireMediaPlayer(handle).SetScaleformSettings(settings);
    }

    public void SetRenderMode(int handle, string renderMode)
    {
        if (!Enum.TryParse<RenderMode>(renderMode, true, out var parsedRenderMode))
            throw new ArgumentException($"'{renderMode}' is not a valid render mode.", nameof(renderMode));

        RequireMediaPlayer(handle).SetRenderMode(parsedRenderMode);
    }

    public void Stop(int handle)
    {
        RequireMediaPlayer(handle).Stop();
    }

    public IReadOnlyList<ConsumerMediaPlayerDto> GetMediaPlayers()
    {
        return registry.All.Select(ConsumerMediaPlayerDto.From).ToList();
    }

    public ConsumerMediaPlayerDto? GetMediaPlayer(int handle)
    {
        var mediaPlayer = registry.Get(new Handle(handle));

        return mediaPlayer is null ? null : ConsumerMediaPlayerDto.From(mediaPlayer);
    }

    public IReadOnlyList<ConsumerMediaPlayerDto> GetMediaPlayersByName(string name)
    {
        return registry.GetByName(name).Select(ConsumerMediaPlayerDto.From).ToList();
    }

    public string GetMediaPlayersJson()
    {
        return Serializer.Serialize(GetMediaPlayers(), Serializer.Settings);
    }

    public string? GetMediaPlayerJson(int handle)
    {
        var mediaPlayer = GetMediaPlayer(handle);

        return mediaPlayer is null ? null : Serializer.Serialize(mediaPlayer, Serializer.Settings);
    }

    public string GetMediaPlayersByNameJson(string name)
    {
        return Serializer.Serialize(GetMediaPlayersByName(name), Serializer.Settings);
    }

    public void RegisterExports(ExportDictionary exports)
    {
        exports.Add(ConsumerExports.Create, new Func<string, string, int>(Create));

        exports.Add(ConsumerExports.Enqueue, new Action<int, string>(Enqueue));

        exports.Add(ConsumerExports.SetQueue, new Action<int, string>(SetQueue));

        exports.Add(ConsumerExports.SetPaused, new Action<int, bool>(SetPaused));

        exports.Add(ConsumerExports.Skip, new Action<int>(Skip));

        exports.Add(ConsumerExports.Seek, new Action<int, int>(Seek));

        exports.Add(ConsumerExports.SetLoop, new Action<int, bool>(SetLoop));

        exports.Add(ConsumerExports.SetMuted, new Action<int, bool>(SetMuted));

        exports.Add(ConsumerExports.SetVideoEnabled, new Action<int, bool>(SetVideoEnabled));

        exports.Add(ConsumerExports.SetScaleformSettings, new Action<int, string>(SetScaleformSettings));

        exports.Add(ConsumerExports.SetRenderMode, new Action<int, string>(SetRenderMode));

        exports.Add(ConsumerExports.Stop, new Action<int>(Stop));

        exports.Add(ConsumerExports.GetMediaPlayers, new Func<string>(GetMediaPlayersJson));

        exports.Add(ConsumerExports.GetMediaPlayer, new Func<int, string?>(GetMediaPlayerJson));

        exports.Add(ConsumerExports.GetMediaPlayersByName, new Func<string, string>(GetMediaPlayersByNameJson));
    }

    private static Track BuildTrack(string url, string title, string thumbnailUrl)
    {
        var track = new Track(url, title, thumbnailUrl ?? string.Empty, null);

        if (!track.IsValid()) throw new ArgumentException($"Track url '{url}' is not a valid url.", nameof(url));

        return track;
    }

    private Target ResolveTarget(Target target)
    {
        switch (target)
        {
            case ScreenTarget screenTarget:
                var screen = screens.FirstOrDefault(s =>
                    string.Equals(s.Name, screenTarget.Screen.Name, StringComparison.OrdinalIgnoreCase));

                if (screen is null) throw new ConsumerScreenNotFoundException(screenTarget.Screen.Name);

                return new ScreenTarget(screen);

            case ModelTarget modelTarget:
                var model = models.FirstOrDefault(m => m.Prop == modelTarget.Model.Prop);

                if (model is null) throw new ConsumerModelNotFoundException(modelTarget.Model.Prop);

                return new ModelTarget(model);

            default:
                throw new ConsumerUnsupportedTargetException(target.Type);
        }
    }

    private void EnsureRenderTargetIsFree(ModelTarget modelTarget)
    {
        var renderTargetInUse = registry.All.Any(mp =>
            mp.Target is ModelTarget existing &&
            string.Equals(existing.Model.RenderTarget, modelTarget.Model.RenderTarget,
                StringComparison.OrdinalIgnoreCase));

        if (renderTargetInUse) throw new ConsumerRenderTargetInUseException(modelTarget.Model.RenderTarget);
    }

    private IMediaPlayer RequireMediaPlayer(int handle)
    {
        return registry.Get(new Handle(handle)) ?? throw new ConsumerMediaPlayerNotFoundException(handle);
    }

    private void OnMediaPlayerAdded(IMediaPlayer mediaPlayer)
    {
        var subscriptions = new List<(string Event, Delegate Handler)>();

        void Subscribe<TDelegate>(string @event, TDelegate handler) where TDelegate : Delegate
        {
            eventManager.On(MediaPlayerEventScope.For(mediaPlayer.Handle, @event), handler);
            subscriptions.Add((@event, handler));
        }

        Subscribe(MediaPlayerEvents.Played,
            (Action<CommunicationMessage, ITrack>)((_, track) => OnTrackChanged(mediaPlayer, track)));

        Subscribe(MediaPlayerEvents.TrackSkipped,
            (Action<CommunicationMessage, ITrack, ITrack>)((_, _, next) => OnTrackChanged(mediaPlayer, next)));

        Subscribe(MediaPlayerEvents.Paused,
            (Action<CommunicationMessage, ITrack>)((_, _) =>
                OnPlaybackStateChanged(mediaPlayer, ConsumerPlaybackState.Paused)));

        Subscribe(MediaPlayerEvents.Resumed,
            (Action<CommunicationMessage, ITrack>)((_, _) =>
                OnPlaybackStateChanged(mediaPlayer, ConsumerPlaybackState.Playing)));

        Subscribe(MediaPlayerEvents.QueueChanged,
            (Action<CommunicationMessage>)(_ => OnQueueChanged(mediaPlayer)));

        eventSubscriptionsByHandle[mediaPlayer.Handle.Value] = subscriptions;

        object? ownerResource = null;

        triggerEvent(ConsumerEvents.MediaPlayerCreated,
            [mediaPlayer.Handle.Value, mediaPlayer.Target.Type.ToString(), ownerResource]);
    }

    private void OnMediaPlayerRemoved(IMediaPlayer mediaPlayer)
    {
        if (eventSubscriptionsByHandle.TryGetValue(mediaPlayer.Handle.Value, out var subscriptions))
        {
            eventSubscriptionsByHandle.Remove(mediaPlayer.Handle.Value);

            foreach (var subscription in subscriptions)
                eventManager.Off(MediaPlayerEventScope.For(mediaPlayer.Handle, subscription.Event),
                    subscription.Handler);
        }
        else
        {
            logger.Warning("MediaPlayer '{handle}' removed but had no tracked Consumer event subscriptions.",
                mediaPlayer.Handle);
        }

        triggerEvent(ConsumerEvents.MediaPlayerDestroyed, [mediaPlayer.Handle.Value]);
    }

    private void OnTrackChanged(IMediaPlayer mediaPlayer, ITrack track)
    {
        triggerEvent(ConsumerEvents.TrackChanged,
            [mediaPlayer.Handle.Value, track.Url, track.Title, track.ThumbnailUrl]);
    }

    private void OnPlaybackStateChanged(IMediaPlayer mediaPlayer, ConsumerPlaybackState state)
    {
        triggerEvent(ConsumerEvents.PlaybackStateChanged, [mediaPlayer.Handle.Value, state.ToString()]);
    }

    private void OnQueueChanged(IMediaPlayer mediaPlayer)
    {
        triggerEvent(ConsumerEvents.QueueChanged, [mediaPlayer.Handle.Value]);
    }
}