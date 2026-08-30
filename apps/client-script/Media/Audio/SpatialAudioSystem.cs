using CitizenFX.Core.Native;
using Hypnonema.Client.Dui;

namespace Hypnonema.Client.Media.Audio;

public sealed class SpatialAudioSystem : IAudioSystem
{
    private bool initialized;

    public SpatialAudioSystem(Browser browser, MediaPlayer mediaPlayer, float maxDistance, float minDistance)
    {
        Browser = browser;
        MediaPlayer = mediaPlayer;
        MaxDistance = maxDistance;
        MinDistance = minDistance;

        TryInitialize();
    }

    public void CalculateVolume()
    {
        if (!initialized && !TryInitialize()) return;

        Browser.SendMessage(Shared.Dui.DuiEvents.CalculateSpatialAudioVolume,
            new { PlayerPosition = API.GetEntityCoords(API.GetPlayerPed(API.PlayerId()), false) });
    }

    public Browser Browser { get; }

    public float MaxDistance { get; }

    public float MinDistance { get; }

    public MediaPlayer MediaPlayer { get; }

    private bool TryInitialize()
    {
        if (!MediaPlayer.TryGetPosition(out var sourcePosition)) return false;

        Browser.SendMessage(Shared.Dui.DuiEvents.InitSpatialAudioSystem,
            new { SourcePosition = sourcePosition, MaxDistance, MinDistance });

        initialized = true;

        return true;
    }
}