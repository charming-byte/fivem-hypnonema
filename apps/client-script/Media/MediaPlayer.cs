using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Hypnonema.Client.Communications;
using Hypnonema.Client.Configurations;
using Hypnonema.Client.Diagnostics;
using Hypnonema.Client.Extensions;
using Hypnonema.Client.Rpc;
using Hypnonema.Shared;
using Hypnonema.Shared.Communications;
using Hypnonema.Shared.Diagnostics;
using Hypnonema.Shared.Media;
using Vector3 = CitizenFX.Core.Vector3;

namespace Hypnonema.Client.Media;

public sealed class MediaPlayer : MediaPlayerBase
{
    private const int PositionCacheDurationMs = 250;

    private readonly Configuration configuration;

    private readonly ILogger logger;

    private readonly List<EventSubscription> nuiSubscriptions = [];

    private readonly List<EventSubscription> rpcHandlers = [];

    private Vector3? cachedPosition;

    private bool disposed;

    private int positionCacheExpiry;

    public MediaPlayer(Configuration configuration, MediaPlayerOptions options) : base(options)
    {
        logger = new Logger(ToString());
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        rpcHandlers.AddRange(RpcMethodBinder.Bind(this, GetScopedEvent));
        nuiSubscriptions.AddRange(NuiCallbackBinder.Bind(this, GetScopedEvent));

        CurrentState = new PassiveVisibilityState(this, logger, TickManager, EventManager, configuration);
        CurrentState.OnEnterState();
    }

    public string Error { get; private set; }

    public MediaPlayerVisibilityState CurrentState { get; private set; }

    public float BaseVolume { get; private set; } = 1.0f;

    [NuiCallback(Nui.NuiEvents.Seek)]
    private void OnSeek(Nui.SeekEventArgs e)
    {
        EmitRpc(MediaPlayerRpcEvents.Seek, e.Position);
    }

    [NuiCallback(Nui.NuiEvents.Stop)]
    private void OnStop(Nui.MediaPlayerEventArgs _)
    {
        EmitStop();
    }

    [NuiCallback(Nui.NuiEvents.PlayNext)]
    private void OnPlayNext(Nui.MediaPlayerEventArgs _)
    {
        EmitRpc(MediaPlayerRpcEvents.PlayNext);
    }

    [NuiCallback(Nui.NuiEvents.SetMuted)]
    private void OnSetMuted(Nui.SetMutedEventArgs e)
    {
        EmitRpc(MediaPlayerRpcEvents.SetMuted, e.Muted);
    }

    // Base volume is deliberately local to this client and never leaves it, so no RPC is emitted.
    [NuiCallback(Nui.NuiEvents.SetVolume)]
    private void OnSetVolume(Nui.SetVolumeEventArgs e)
    {
        SetBaseVolume(e.Volume);
    }

    [NuiCallback(Nui.NuiEvents.SetPaused)]
    private void OnSetPaused(Nui.SetPausedEventArgs e)
    {
        EmitPause(e.Paused);
    }

    public void EmitPause(bool paused)
    {
        EmitRpc(MediaPlayerRpcEvents.SetPaused, paused);
    }

    [NuiCallback(Nui.NuiEvents.SetLooped)]
    private void OnSetLooped(Nui.SetLoopedEventArgs e)
    {
        EmitRpc(MediaPlayerRpcEvents.SetLoop, e.Looped);
    }

    [NuiCallback(Nui.NuiEvents.SetRenderMode)]
    private void OnSetRenderMode(Nui.SetRenderModeEventArgs e)
    {
        EmitRpc(MediaPlayerRpcEvents.SetRenderMode, e.RenderMode);
    }

    [NuiCallback(Nui.NuiEvents.SetVideoEnabled)]
    private void OnSetVideoEnabled(Nui.SetVideoEnabledEventArgs e)
    {
        EmitRpc(MediaPlayerRpcEvents.SetVideoEnabled, e.VideoEnabled);
    }

    [NuiCallback(Nui.NuiEvents.SetScaleformOptions)]
    private void OnSetScaleformOptions(Nui.SetScaleformOptionsEventArgs e)
    {
        EmitRpc(MediaPlayerRpcEvents.SetScaleformSettings, e.Scaleform);
    }

    [NuiCallback(Nui.NuiEvents.SaveScaleformSettings)]
    private void OnSaveScaleformSettings(Nui.MediaPlayerEventArgs _)
    {
        EmitRpc(MediaPlayerRpcEvents.SaveScaleformSettings);
    }

    private void UnregisterNuiCallbacks()
    {
        NuiCallbackBinder.Unbind(nuiSubscriptions);
    }

    public float GetDistance()
    {
        return TryGetPosition(out var position)
            ? World.GetDistance(GetLocalPlayerPosition(), position)
            : float.MaxValue;
    }

    public void EmitStop()
    {
        EmitRpc(MediaPlayerRpcEvents.Stop);
    }

    public bool IsInRange()
    {
        return GetDistance() <= configuration.DefaultRange;
    }

    public bool TryGetPosition(out Vector3 position)
    {
        var resolved = ResolvePosition();

        position = resolved ?? Vector3.Zero;

        return resolved.HasValue;
    }

    private Vector3? ResolvePosition()
    {
        var now = API.GetGameTimer();

        if (now < positionCacheExpiry) return cachedPosition;

        cachedPosition = Target switch
        {
            ModelTarget => GetEntity()?.Position,
            ScreenTarget screenTarget => screenTarget.Screen.Scaleform.Transform.Position.ToFxVector3(),
            _ => null
        };

        positionCacheExpiry = now + PositionCacheDurationMs;

        return cachedPosition;
    }

    public Prop? GetEntity()
    {
        if (Target is not ModelTarget modelTarget) return null;

        var modelHash = API.GetHashKey(modelTarget.Model.Prop);
        var playerPosition = GetLocalPlayerPosition();

        var closestHandle = 0;
        var closestDistance = float.MaxValue;

        // The object pool is a plain handle array, so scanning it costs one native call and allocates nothing per prop.
        // GetGamePool is typed as dynamic and boxes its handles, hence the one-off cast and the per-element conversion.
        foreach (var entity in (IEnumerable)API.GetGamePool("CObject"))
        {
            var handle = Convert.ToInt32(entity);

            if (API.GetEntityModel(handle) != modelHash) continue;

            var distance = Vector3.DistanceSquared(playerPosition, API.GetEntityCoords(handle, false));

            if (distance >= closestDistance) continue;

            closestDistance = distance;
            closestHandle = handle;
        }

        return closestHandle == 0 ? null : new Prop(closestHandle);
    }

    private static Vector3 GetLocalPlayerPosition()
    {
        return API.GetEntityCoords(API.GetPlayerPed(API.PlayerId()), false);
    }

    private void EmitRpc(string @event, params object[] payloads)
    {
        RpcManager.Emit(GetScopedEvent(@event), payloads);
    }

    public void SetBaseVolume(float volume)
    {
        if (volume < 0f) volume = 0f;
        if (volume > 1f) volume = 1f;

        BaseVolume = volume;
    }

    public void TransitionTo(MediaPlayerVisibilityState visibilityState)
    {
        logger.Debug($"Transitioning from {CurrentState} to {visibilityState}");

        CurrentState.OnExitState();

        CurrentState = visibilityState;

        CurrentState.OnEnterState();
    }

    public override void Dispose()
    {
        if (disposed) return;
        disposed = true;

        base.Dispose();

        CurrentState.OnExitState();

        UnregisterEventHandlers();
        UnregisterNuiCallbacks();
    }

    private void UnregisterEventHandlers()
    {
        RpcMethodBinder.Unbind(rpcHandlers);
    }

    [RpcMethod(MediaPlayerRpcEvents.SyncTrackPosition)]
    private void OnSyncTrackPosition(CommunicationMessage message, double position)
    {
        Track.Seek(position);
        Emit(MediaPlayerEvents.SyncedTrackPosition, position);
    }

    [RpcMethod(MediaPlayerRpcEvents.Play)]
    private void OnPlay(CommunicationMessage message, Track track)
    {
        try
        {
            Enqueue(track);
        }
        catch (Exception exception)
        {
            logger.Error(exception, $"Failed to enqueue track '{track?.Url}' on {this}.");
        }
    }

    [RpcMethod(MediaPlayerRpcEvents.PlayNext)]
    private void OnPlayNext(CommunicationMessage message, Track track)
    {
        if (track == null || !track.IsValid())
        {
            logger.Warning($"Ignoring '{MediaPlayerRpcEvents.PlayNext}' on {this}: the received track is not valid.");

            return;
        }

        try
        {
            var previousTrack = Snapshot(Track);

            ReconcileQueue(track);

            Track.Play(track);

            Emit(MediaPlayerEvents.TrackSkipped, previousTrack, track);
        }
        catch (Exception exception)
        {
            logger.Error(exception, $"Failed to advance to track '{track.Url}' on {this}.");
        }
    }

    private void ReconcileQueue(ITrack track)
    {
        lock (QueueLock)
        {
            if (!Queue.Any(queued => queued.Url == track.Url)) return;

            while (Queue.Count > 0)
                if (Queue.Dequeue().Url == track.Url)
                    break;
        }
    }

    private static Track Snapshot(ITrack track)
    {
        return new Track(track.Url, track.Title, track.ThumbnailUrl, track.RequestedBy, track.Duration);
    }

    [RpcMethod(MediaPlayerRpcEvents.SetScaleformSettings)]
    private void OnSetScaleformSettings(CommunicationMessage message, ScaleformRenderConfiguration renderConfiguration)
    {
        SetScaleformSettings(renderConfiguration);
    }

    [RpcMethod(MediaPlayerRpcEvents.SetRenderMode)]
    private void OnSetRenderMode(CommunicationMessage message, RenderMode renderMode)
    {
        try
        {
            SetRenderMode(renderMode);
        }
        catch (Exception exception)
        {
            logger.Error(exception, $"Failed to set render mode '{renderMode}' on {this}.");
        }
    }

    [RpcMethod(MediaPlayerRpcEvents.SetDuration)]
    private void OnSetDuration(CommunicationMessage message, int duration)
    {
        if (Track.Duration == duration) return;

        SetDuration(duration);
    }

    [RpcMethod(MediaPlayerRpcEvents.Stop)]
    private void OnStop(CommunicationMessage message)
    {
        Stop();
    }

    [RpcMethod(MediaPlayerRpcEvents.SetPaused)]
    private void OnSetPaused(CommunicationMessage message, bool paused)
    {
        SetPaused(paused);
    }

    [RpcMethod(MediaPlayerRpcEvents.SetAdActive)]
    private void OnSetAdActive(CommunicationMessage message, bool adActive)
    {
        SetAdActive(adActive);
    }

    [RpcMethod(MediaPlayerRpcEvents.SetLoop)]
    private void OnSetLooped(CommunicationMessage message, bool looped)
    {
        SetLooped(looped);
    }

    [RpcMethod(MediaPlayerRpcEvents.SetQueue)]
    private void OnSetQueue(CommunicationMessage message, List<Track> tracks)
    {
        SetQueue(tracks);
    }

    [RpcMethod(MediaPlayerRpcEvents.SetVideoEnabled)]
    private void OnSetVideoEnabled(CommunicationMessage message, bool videoEnabled)
    {
        SetVideoEnabled(videoEnabled);
    }

    [RpcMethod(MediaPlayerRpcEvents.SetMuted)]
    private void OnSetMuted(CommunicationMessage message, bool muted)
    {
        SetMuted(muted);
    }

    [RpcMethod(MediaPlayerRpcEvents.Seek)]
    private void OnSeek(CommunicationMessage message, int position)
    {
        if (Track.Position == position) return;

        Seek(position);
    }

    protected override async Task OnTick()
    {
        // Not implemented in the client media player, as the server will handle track position updates and send the appropriate RPC to the client.
    }

    protected override void HandleTrackEnd()
    {
        // Not implemented in the client media player, as the server will handle track end events and send the appropriate RPC to the client.
    }

    public override string ToString()
    {
        return $"{Label} ({Handle})";
    }
}