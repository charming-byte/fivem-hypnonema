using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Hypnonema.Client.Communications;
using Hypnonema.Client.Configurations;
using Hypnonema.Client.Rpc;
using Hypnonema.Shared;
using Hypnonema.Shared.Communications;
using Hypnonema.Shared.Diagnostics;
using Hypnonema.Shared.Media;
using Hypnonema.Shared.Media.Audio;

namespace Hypnonema.Client.Media;

public sealed class MediaPlayerManager
{
    private readonly ILogger logger;
    private readonly bool disableIdleCam;
    private readonly ITickManager tickManager;
    private readonly Configuration configuration;
    private readonly IEventManager eventManager;
    private readonly Dictionary<Handle, MediaPlayer> mediaPlayers;

    private bool isResetting;

    public MediaPlayerManager(ILogger logger, List<MediaPlayer> initialMediaPlayers, ITickManager tickManager,
        Configuration configuration, IEventManager eventManager)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.tickManager = tickManager ?? throw new ArgumentNullException(nameof(tickManager));
        this.eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        disableIdleCam = configuration.DisableIdleCam;
        mediaPlayers =
            new Dictionary<Handle, MediaPlayer>(initialMediaPlayers.ToDictionary(p => p.Handle, p => p));

        RpcManager.On<MediaPlayerDTO>(RpcEvents.CreateMediaPlayer, OnCreateMediaPlayer);
        RpcManager.On<List<Screen>>(RpcEvents.ScreensUpdated, OnScreensUpdated);

        tickManager.Add(OnTick);
    }

    public event Action<MediaPlayer>? MediaPlayerAdded;

    public event Action<MediaPlayer>? MediaPlayerRemoved;

    public List<MediaPlayer> GetMediaPlayers()
    {
        return mediaPlayers.Any() ? mediaPlayers.Values.ToList() : [];
    }

    public MediaPlayer? Get(Handle handle)
    {
        return mediaPlayers.TryGetValue(handle, out var mediaPlayer) ? mediaPlayer : null;
    }

    public async Task<bool> ResetAsync()
    {
        if (isResetting)
        {
            logger.Warning("Reset already in progress; ignoring request.");
            return false;
        }

        isResetting = true;

        try
        {
            logger.Debug("Resetting media players...");

            foreach (var mediaPlayer in mediaPlayers.Values.ToList())
                RemoveMediaPlayer(mediaPlayer);

            var mediaPlayerDtos = await RpcManager.Request<List<MediaPlayerDTO>>(RpcEvents.GetMediaPlayers);

            foreach (var dto in mediaPlayerDtos)
                AddMediaPlayer(dto);

            logger.Debug($"Reset complete; {mediaPlayers.Count} media player(s) active.");

            return true;
        }
        catch (Exception exception)
        {
            logger.Error(exception, $"Failed to reset media players: {exception.Message}");

            return false;
        }
        finally
        {
            isResetting = false;
        }
    }

    private async Task OnTick()
    {
        var isAnyPlayerActive = mediaPlayers.Values.Any(m => m is { CurrentState: ActiveVisibilityState });

        if (isAnyPlayerActive && disableIdleCam)
            API.DisableIdleCamera(true);
        else
            API.DisableIdleCamera(false);

        await BaseScript.Delay(1000);
    }

    private void OnCreateMediaPlayer(CommunicationMessage message, MediaPlayerDTO mediaPlayerDto)
    {
        AddMediaPlayer(mediaPlayerDto);
    }

    private void OnScreensUpdated(CommunicationMessage message, List<Screen> screens)
    {
        // Push edits onto a MediaPlayer already playing on the screen without tearing it down: geometry moves
        // the live drawable in place (ticket 08), render distance / audio update the running parameters.
        foreach (var mediaPlayer in mediaPlayers.Values)
        {
            if (mediaPlayer.Target is not ScreenTarget screenTarget) continue;

            var updated = screens.FirstOrDefault(s => s.ResolveId() == screenTarget.Screen.ResolveId());
            if (updated == null) continue;

            if (screenTarget.Screen.Scaleform != updated.Scaleform)
            {
                screenTarget.Screen.Scaleform = updated.Scaleform;
                mediaPlayer.ApplyScreenGeometry(updated.Scaleform);
            }

            screenTarget.Screen.RenderDistance = updated.RenderDistance;

            if (AudioConfiguration.AreEqual(screenTarget.Screen.Audio, updated.Audio)) continue;

            screenTarget.Screen.Audio = updated.Audio;
            mediaPlayer.SetAudioConfiguration(updated.Audio);
        }
    }

    private void OnMediaPlayerStopped(CommunicationMessage message, MediaPlayer mediaPlayer)
    {
        RemoveMediaPlayer(mediaPlayer);

        logger.Debug($"MediaPlayer '{mediaPlayer}' stopped");
    }

    private void AddMediaPlayer(MediaPlayerDTO mediaPlayerDto)
    {
        if (mediaPlayers.ContainsKey(mediaPlayerDto.Handle))
        {
            logger.Warning($"Attempted to create an already existing mediaPlayer {mediaPlayerDto.Handle}");
            return;
        }

        var options = MediaPlayerOptions.FromDTO(mediaPlayerDto, configuration.MaxQueueSize, tickManager, eventManager);
        var mediaPlayer = new MediaPlayer(configuration, options);

        eventManager.On(mediaPlayer.GetScopedEvent(MediaPlayerEvents.Stopped), OnMediaPlayerStopped);
        mediaPlayers[mediaPlayerDto.Handle] = mediaPlayer;

        logger.Debug($"Created mediaPlayer '{mediaPlayer.Label}' with handle {mediaPlayer.Handle}");

        MediaPlayerAdded?.Invoke(mediaPlayer);
    }

    private void RemoveMediaPlayer(MediaPlayer mediaPlayer)
    {
        eventManager.Off(mediaPlayer.GetScopedEvent(MediaPlayerEvents.Stopped), OnMediaPlayerStopped);
        mediaPlayers.Remove(mediaPlayer.Handle);

        MediaPlayerRemoved?.Invoke(mediaPlayer);

        mediaPlayer.Dispose();
    }
}