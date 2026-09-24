using Hypnonema.Client.Dui;

namespace Hypnonema.Client.Media.Audio;

public interface IAudioSystem
{
    public Browser Browser { get; }

    public MediaPlayer MediaPlayer { get; }

    public float MaxDistance { get; }

    public float MinDistance { get; }
    public void CalculateVolume();
}