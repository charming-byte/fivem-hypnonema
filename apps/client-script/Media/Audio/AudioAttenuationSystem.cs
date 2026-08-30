using Hypnonema.Client.Dui;
using Hypnonema.Shared.Media.Audio;

namespace Hypnonema.Client.Media.Audio;

public sealed class AudioAttenuationSystem : IAudioSystem
{
    private const float OcclusionSmoothing = 0.25f;

    private readonly DistanceAttenuation attenuation;

    private readonly OcclusionProbe occlusionProbe;

    private float occlusion;

    public AudioAttenuationSystem(Browser browser, MediaPlayer mediaPlayer, DistanceAttenuation attenuation)
    {
        Browser = browser;
        MediaPlayer = mediaPlayer;

        this.attenuation = attenuation;

        occlusionProbe = new OcclusionProbe(mediaPlayer);
    }

    public void CalculateVolume()
    {
        Browser.SetVolume(GetVolume());
    }

    public float MaxDistance => attenuation.MaxDistance;

    public float MinDistance => attenuation.MinDistance;

    public Browser Browser { get; }

    public MediaPlayer MediaPlayer { get; }

    private float GetVolume()
    {
        var baseVolume = MediaPlayer.BaseVolume;

        if (baseVolume <= 0f) return 0f;

        occlusionProbe.Poll();

        occlusion += (occlusionProbe.Occlusion - occlusion) * OcclusionSmoothing;

        // Resolving the distance walks the world entity pool, so read it exactly once per calculation.
        return attenuation.GetVolume(MediaPlayer.GetDistance(), occlusion) * baseVolume;
    }
}
