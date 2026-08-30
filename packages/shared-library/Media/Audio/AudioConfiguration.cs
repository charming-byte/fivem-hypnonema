namespace Hypnonema.Shared.Media.Audio;

public abstract class AudioConfiguration
{
    public abstract AudioMode Mode { get; }

    public static bool AreEqual(AudioConfiguration? a, AudioConfiguration? b)
    {
        return (a, b) switch
        {
            (DefaultAudioConfiguration x, DefaultAudioConfiguration y) => x.Attenuation == y.Attenuation,
            _ => false
        };
    }
}