using CitizenFX.Core;
using CitizenFX.Core.Native;
using Hypnonema.Shared.Media;

namespace Hypnonema.Client.Media.Audio;

public sealed class OcclusionProbe
{
    private const int WorldGeometryFlag = 1;

    private const int ProbeIntervalMs = 1000;

    private const float SourceHitToleranceMeters = 1.5f;

    private const float EarHeightMeters = 0.7f;

    private readonly MediaPlayer mediaPlayer;

    private int nextProbeAt;

    private int pendingHandle;

    public OcclusionProbe(MediaPlayer mediaPlayer)
    {
        this.mediaPlayer = mediaPlayer;
    }

    public float Occlusion { get; private set; }

    public void Poll()
    {
        if (pendingHandle != 0)
        {
            CollectResult();

            return;
        }

        var now = API.GetGameTimer();

        if (now < nextProbeAt) return;

        nextProbeAt = now + ProbeIntervalMs;

        StartProbe();
    }

    private void StartProbe()
    {
        if (!mediaPlayer.TryGetPosition(out var source)) return;

        var ped = API.GetPlayerPed(API.PlayerId());
        var origin = API.GetEntityCoords(ped, false) + new Vector3(0f, 0f, EarHeightMeters);

        var ignoredEntity = mediaPlayer.Target is ModelTarget ? mediaPlayer.GetEntity()?.Handle ?? 0 : 0;

        pendingHandle = API.StartShapeTestLosProbe(
            origin.X, origin.Y, origin.Z,
            source.X, source.Y, source.Z,
            WorldGeometryFlag,
            ignoredEntity,
            4);
    }

    private void CollectResult()
    {
        var hit = false;
        var endCoords = Vector3.Zero;
        var surfaceNormal = Vector3.Zero;
        var entityHit = 0;

        // 1 means the trace is still running; anything else releases the handle.
        if (API.GetShapeTestResult(pendingHandle, ref hit, ref endCoords, ref surfaceNormal, ref entityHit) == 1)
            return;

        pendingHandle = 0;

        if (!hit || !mediaPlayer.TryGetPosition(out var source))
        {
            Occlusion = 0f;

            return;
        }

        Occlusion = World.GetDistance(endCoords, source) > SourceHitToleranceMeters ? 1f : 0f;
    }
}
