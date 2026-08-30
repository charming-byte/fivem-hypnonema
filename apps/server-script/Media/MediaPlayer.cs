using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using Hypnonema.Server.Communications;
using Hypnonema.Server.Rpc;
using Hypnonema.Shared;
using Hypnonema.Shared.Communications;
using Hypnonema.Shared.Configurations;
using Hypnonema.Shared.Media;
using Serilog;

namespace Hypnonema.Server.Media;

public sealed class MediaPlayer : MediaPlayerBase
{
    private const double PreRollStartWindowSeconds = 5;

    private readonly List<IClient> activeClients = [];
    private readonly AdQuorumPlaybackController adQuorumPlayback;

    private readonly ILogger logger;
    private readonly List<EventSubscription> rpcHandlers = [];
    private readonly ScreenRepository? screenRepository;

    public MediaPlayer(ILogger logger, MediaPlayerOptions options, YouTubeAdQuorumConfiguration youtubeAdQuorum,
        ScreenRepository? screenRepository = null) :
        base(options)
    {
        this.logger = logger;
        this.screenRepository = screenRepository;
        adQuorumPlayback = new AdQuorumPlaybackController(
            youtubeAdQuorum.Threshold,
            youtubeAdQuorum.WaitTimeout,
            () => YouTubeMedia.IsYouTubeUrl(Track.Url),
            () => Paused,
            () => Track.Position <= PreRollStartWindowSeconds,
            SetAdQuorumPaused,
            HoldPlaybackAtStart);

        rpcHandlers.AddRange(RpcMethodBinder.Bind(this, logger, GetScopedEvent));

        TickManager.Add(OnTick);
        TickManager.Add(SyncTrackPosition);
    }

    public override void Dispose()
    {
        base.Dispose();

        TickManager.Remove(OnTick);
        TickManager.Remove(SyncTrackPosition);

        UnregisterEventHandlers();
    }

    public override void Enqueue(ITrack track)
    {
        base.Enqueue(track);

        EmitRpc(MediaPlayerRpcEvents.Play, track);
    }

    public override void Stop()
    {
        base.Stop();

        EmitRpc(MediaPlayerRpcEvents.Stop);
    }

    public override void SetPaused(bool paused)
    {
        base.SetPaused(paused);

        EmitRpc(MediaPlayerRpcEvents.SetPaused, Paused);
    }

    public override void SetLooped(bool looped)
    {
        base.SetLooped(looped);

        EmitRpc(MediaPlayerRpcEvents.SetLoop, Looped);
    }

    public override void SetQueue(IEnumerable<ITrack> tracks)
    {
        base.SetQueue(tracks);

        EmitRpc(MediaPlayerRpcEvents.SetQueue, Queue);
    }

    public override void SetMuted(bool muted)
    {
        base.SetMuted(muted);

        EmitRpc(MediaPlayerRpcEvents.SetMuted, Muted);
    }

    public override void SetVideoEnabled(bool isVideoEnabled)
    {
        base.SetVideoEnabled(isVideoEnabled);

        EmitRpc(MediaPlayerRpcEvents.SetVideoEnabled, Video);
    }

    public override void SetRenderMode(RenderMode renderMode)
    {
        base.SetRenderMode(renderMode);

        EmitRpc(MediaPlayerRpcEvents.SetRenderMode, RenderMode);
    }

    public override void SetScaleformSettings(ScaleformRenderConfiguration renderConfiguration)
    {
        base.SetScaleformSettings(renderConfiguration);

        EmitRpc(MediaPlayerRpcEvents.SetScaleformSettings, Scaleform);
    }

    public override void Seek(int position)
    {
        base.Seek(position);

        EmitRpc(MediaPlayerRpcEvents.Seek, position);
    }

    public override void SkipCurrentTrack()
    {
        base.SkipCurrentTrack();

        EmitRpc(MediaPlayerRpcEvents.PlayNext, Track);
    }

    private void UnregisterEventHandlers()
    {
        RpcMethodBinder.Unbind(rpcHandlers);
    }

    private async Task SyncTrackPosition()
    {
        var disconnectedClients = activeClients.Where(c => !BaseServer.Instance.IsClientConnected(c)).ToList();

        foreach (var client in disconnectedClients)
            RemoveViewer(client.Handle);

        if (!Paused && !Track.HasEnded())
            activeClients.ForEach(p => EmitRpc(MediaPlayerRpcEvents.SyncTrackPosition, p, Track.Position));

        await BaseScript.Delay(1000);
    }

    private void EmitRpc(string @event, params object[] payloads)
    {
        EmitRpc(@event, null, payloads);
    }

    private void EmitRpc(string @event, IClient? client, params object[] payloads)
    {
        RpcManager.Emit(GetScopedEvent(@event), client, payloads);
    }

    private void SetAdQuorumPaused(bool paused)
    {
        SetPaused(paused);
    }

    private void HoldPlaybackAtStart()
    {
        Track.Reset();
        SetAdQuorumPaused(true);
    }

    private void RemoveViewer(string clientHandle)
    {
        activeClients.RemoveAll(c => c.Handle == clientHandle);
        adQuorumPlayback.RemoveViewer(clientHandle);
    }

    [RpcMethod(MediaPlayerRpcEvents.ActivatingMediaPlayer)]
    private void OnPlayerStartWatchingMediaPlayer(CommunicationMessage message)
    {
        logger.Verbose(
            $"Received '{MediaPlayerRpcEvents.ActivatingMediaPlayer}' from client '{message.Client}' for mediaPlayer '{this}'.");

        if (activeClients.Any(p => p.Handle == message.Client.Handle))
        {
            logger.Verbose($"Client '{message.Client}' is already watching mediaPlayer '{this}', ignoring.");

            return;
        }

        activeClients.Add(message.Client);
        adQuorumPlayback.AddViewer(message.Client.Handle);

        logger.Debug(
            $"Client '{message.Client}' is now watching mediaPlayer '{this}'. Active clients: {activeClients.Count}.");
    }

    [RpcMethod(MediaPlayerRpcEvents.DeactivatingMediaPlayer)]
    private void OnPlayerStopWatchingMediaPlayer(CommunicationMessage message)
    {
        logger.Verbose(
            $"Received '{MediaPlayerRpcEvents.DeactivatingMediaPlayer}' from client '{message.Client}' for mediaPlayer '{this}'.");

        if (message.Client == null)
        {
            logger.Verbose(
                $"Message for '{MediaPlayerRpcEvents.DeactivatingMediaPlayer}' on mediaPlayer '{this}' had no client attached, ignoring.");

            return;
        }

        RemoveViewer(message.Client.Handle);

        logger.Debug(
            $"Client '{message.Client}' stopped watching mediaPlayer '{this}'. Active clients: {activeClients.Count}.");
    }

    [RpcMethod(MediaPlayerRpcEvents.YoutubeAdStatus)]
    private void OnYoutubeAdStatus(CommunicationMessage message, YoutubeAdStatus status)
    {
        if (message.Client == null)
        {
            logger.Verbose(
                $"Message for '{MediaPlayerRpcEvents.YoutubeAdStatus}' on mediaPlayer '{this}' had no client attached, ignoring.");

            return;
        }

        logger.Debug(
            $"Received '{MediaPlayerRpcEvents.YoutubeAdStatus}' from client '{message.Client}' for mediaPlayer '{this}' (status: {status}).");

        adQuorumPlayback.ReportAdStatus(message.Client.Handle, status);
    }

    [RpcMethod(MediaPlayerRpcEvents.SetScaleformSettings)]
    [PermissionRequired(Permissions.Control)]
    private void OnSetScaleformSettings(CommunicationMessage message,
        ScaleformRenderConfiguration scaleformRenderConfiguration)
    {
        logger.Verbose(
            $"Received '{MediaPlayerRpcEvents.SetScaleformSettings}' from client '{message.Client}' for mediaPlayer '{this}' (settings: {scaleformRenderConfiguration}).");

        SetScaleformSettings(scaleformRenderConfiguration);
    }

    [RpcMethod(MediaPlayerRpcEvents.SaveScaleformSettings)]
    [PermissionRequired(Permissions.ManageScreens)]
    private void OnSaveScaleformSettings(CommunicationMessage message)
    {
        logger.Verbose(
            $"Received '{MediaPlayerRpcEvents.SaveScaleformSettings}' from client '{message.Client}' for mediaPlayer '{this}'.");

        if (Target is not ScreenTarget screenTarget)
        {
            logger.Warning(
                "Rejected save for mediaPlayer '{mediaPlayer}': only screen-targeted media players can be saved.",
                this);

            EmitRpc(MediaPlayerRpcEvents.Error, message.Client,
                "Scaleform settings can only be saved for screen-based media players.");

            return;
        }

        var screen = screenRepository?.All.FirstOrDefault(s => s.Name == screenTarget.Screen.Name);

        if (screen == null)
        {
            logger.Warning(
                "Could not save scaleform for mediaPlayer '{mediaPlayer}': no screen named '{name}' in configuration.",
                this, screenTarget.Screen.Name);

            EmitRpc(MediaPlayerRpcEvents.Error, message.Client,
                "Could not save: screen not found in configuration.");

            return;
        }

        try
        {
            screen.Scaleform = Scaleform;

            screenRepository!.Update(screen);
        }
        catch (ScreenValidationException exception)
        {
            logger.Warning("Rejected scaleform save for mediaPlayer '{mediaPlayer}': {errors}", this,
                string.Join("; ", exception.Errors));

            EmitRpc(MediaPlayerRpcEvents.Error, message.Client, string.Join("; ", exception.Errors));
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Failed to save scaleform settings for mediaPlayer '{mediaPlayer}'.", this);

            EmitRpc(MediaPlayerRpcEvents.Error, message.Client, "Failed to save scaleform settings to disk.");
        }
    }

    [RpcMethod(MediaPlayerRpcEvents.Stop)]
    [PermissionRequired(Permissions.Control)]
    private void OnStop(CommunicationMessage message)
    {
        logger.Verbose(
            $"Received '{MediaPlayerRpcEvents.Stop}' from client '{message.Client}' for mediaPlayer '{this}'.");

        Stop();
    }

    [RpcMethod(MediaPlayerRpcEvents.SetPaused)]
    [PermissionRequired(Permissions.Control)]
    private void OnSetPaused(CommunicationMessage message, bool paused)
    {
        logger.Verbose(
            $"Received '{MediaPlayerRpcEvents.SetPaused}' from client '{message.Client}' for mediaPlayer '{this}' (paused: {paused}).");

        SetPaused(paused);
    }

    [RpcMethod(MediaPlayerRpcEvents.SetLoop)]
    [PermissionRequired(Permissions.Control)]
    private void OnSetLooped(CommunicationMessage message, bool looped)
    {
        logger.Verbose(
            $"Received '{MediaPlayerRpcEvents.SetLoop}' from client '{message.Client}' for mediaPlayer '{this}' (looped: {looped}).");

        SetLooped(looped);
    }

    [RpcMethod(MediaPlayerRpcEvents.SetVideoEnabled)]
    [PermissionRequired(Permissions.Control)]
    private void OnSetVideoEnabled(CommunicationMessage message, bool videoEnabled)
    {
        logger.Verbose(
            $"Received '{MediaPlayerRpcEvents.SetVideoEnabled}' from client '{message.Client}' for mediaPlayer '{this}' (videoEnabled: {videoEnabled}).");

        SetVideoEnabled(videoEnabled);
    }

    [RpcMethod(MediaPlayerRpcEvents.SetMuted)]
    [PermissionRequired(Permissions.Control)]
    private void OnSetMuted(CommunicationMessage message, bool muted)
    {
        logger.Verbose(
            $"Received '{MediaPlayerRpcEvents.SetMuted}' from client '{message.Client}' for mediaPlayer '{this}' (muted: {muted}).");

        SetMuted(muted);
    }

    [RpcMethod(MediaPlayerRpcEvents.Play)]
    [PermissionRequired(Permissions.Use)]
    private void OnPlay(CommunicationMessage message, Track track)
    {
        logger.Verbose(
            $"Received '{MediaPlayerRpcEvents.Play}' from client '{message.Client}' for mediaPlayer '{this}' (track: {track}).");

        try
        {
            track.SetRequestingClient(message.Client);
            Enqueue(track);

            logger.Debug($"Track '{track}' enqueued on mediaPlayer '{this}'. Queue size: {Queue.Count}.");
        }
        catch (Exception exception)
        {
            logger.Error(exception,
                "Error while enqueuing track '{track}' on mediaPlayer '{handle}' for client '{client}'",
                track, Handle, message.Client?.Name);

            EmitRpc(MediaPlayerRpcEvents.Error, message.Client, "Could not enqueue track");
        }
    }

    [RpcMethod(MediaPlayerRpcEvents.Seek)]
    [PermissionRequired(Permissions.Control)]
    private void OnSeek(CommunicationMessage message, int position)
    {
        logger.Verbose(
            $"Received '{MediaPlayerRpcEvents.Seek}' from client '{message.Client}' for mediaPlayer '{this}' (position: {position}).");

        Seek(position);
    }

    [RpcMethod(MediaPlayerRpcEvents.SetRenderMode)]
    [PermissionRequired(Permissions.Control)]
    private void OnSetRenderMode(CommunicationMessage message, RenderMode renderMode)
    {
        logger.Verbose(
            $"Received '{MediaPlayerRpcEvents.SetRenderMode}' from client '{message.Client}' for mediaPlayer '{this}' (renderMode: {renderMode}).");

        try
        {
            SetRenderMode(renderMode);
        }
        catch (Exception exception)
        {
            logger.Error(exception,
                "Error while setting render mode '{renderMode}' on mediaPlayer '{handle}' for client '{client}'",
                renderMode, Handle, message.Client?.Name);

            EmitRpc(MediaPlayerRpcEvents.Error, message.Client, "Could not set render mode");
        }
    }

    [RpcMethod(MediaPlayerRpcEvents.PlayNext)]
    [PermissionRequired(Permissions.Control)]
    private void OnSkipCurrentTrack(CommunicationMessage message)
    {
        logger.Verbose(
            $"Received '{MediaPlayerRpcEvents.PlayNext}' from client '{message.Client}' for mediaPlayer '{this}' (current track: {Track}).");

        try
        {
            SkipCurrentTrack();

            logger.Debug($"Skipped to next track '{Track}' on mediaPlayer '{this}'.");
        }
        catch (Exception exception)
        {
            logger.Error(exception,
                "Error while skipping current track on mediaPlayer '{handle}' for client '{client}'",
                Handle, message.Client?.Name);

            EmitRpc(MediaPlayerRpcEvents.Error, message.Client, "Could not skip to next track");
        }
    }

    [RpcMethod(MediaPlayerRpcEvents.SetDuration)]
    private void OnSetDuration(CommunicationMessage message, int duration)
    {
        logger.Verbose(
            $"Received '{MediaPlayerRpcEvents.SetDuration}' from client '{message.Client}' for mediaPlayer '{this}' (duration: {duration}).");

        if (Track.Duration == duration) return;

        if ((Track.RequestedBy == null && Track.Duration != duration) ||
            (Track.RequestedBy != null && Track.RequestedBy.Handle == message.Client.Handle))
        {
            Track.SetDuration(duration);

            logger.Debug(
                $"Duration for track '{Track}' on mediaPlayer '{this}' set to {duration}ms by client '{message.Client}'.");

            EmitRpc(MediaPlayerRpcEvents.SetDuration, duration);
        }
        else
        {
            logger.Verbose(
                $"Ignored duration update ({duration}ms) from client '{message.Client}' on mediaPlayer '{this}': not the requesting client.");
        }
    }

    protected override async Task OnTick()
    {
        adQuorumPlayback.Update();
        SyncAdActiveState();
        Track.OnTick();

        await BaseScript.Delay(1000);
    }

    private void SyncAdActiveState()
    {
        if (AdActive == adQuorumPlayback.IsAdActive) return;

        SetAdActive(adQuorumPlayback.IsAdActive);
        EmitRpc(MediaPlayerRpcEvents.SetAdActive, AdActive);
    }

    protected override void OnTrackChanged()
    {
        adQuorumPlayback.TrackChanged();
    }

    protected override void HandleTrackEnd()
    {
        if (Looped)
        {
            Track.Reset();

            return;
        }

        ITrack? nextTrack;

        lock (QueueLock)
        {
            nextTrack = Queue.Count > 0 ? Queue.Dequeue() : null;
        }

        if (nextTrack != null)
        {
            Play(nextTrack);

            EmitRpc(MediaPlayerRpcEvents.PlayNext, Track);
        }
        else
        {
            Stop();
        }
    }
}