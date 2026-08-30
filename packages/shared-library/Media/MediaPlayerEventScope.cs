namespace Hypnonema.Shared.Media;

public static class MediaPlayerEventScope
{
    public static string For(Handle handle, string eventName)
    {
        return $"mediaPlayer:{handle}:{eventName}";
    }
}