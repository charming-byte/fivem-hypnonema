namespace Hypnonema.Shared.Media.Audio;

public sealed class DefaultAudioConfiguration : AudioConfiguration
{
    public override AudioMode Mode => AudioMode.Default;

    public DistanceAttenuation Attenuation { get; set; } = DistanceAttenuation.Default;

    public static DefaultAudioConfiguration Default => new();
}