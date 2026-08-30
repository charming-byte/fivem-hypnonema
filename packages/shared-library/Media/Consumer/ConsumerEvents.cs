namespace Hypnonema.Shared.Media.Consumer;

public static class ConsumerEvents
{
    public const string MediaPlayerCreated = "hypnonema:mediaPlayerCreated";

    public const string MediaPlayerDestroyed = "hypnonema:mediaPlayerDestroyed";

    public const string PlaybackStateChanged = "hypnonema:playbackStateChanged";

    public const string TrackChanged = "hypnonema:trackChanged";

    public const string QueueChanged = "hypnonema:queueChanged";
}