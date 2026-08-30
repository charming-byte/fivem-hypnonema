namespace Hypnonema.Shared.Media;

public static class MediaPlayerRpcEvents
{
    public const string Play = "play";
    public const string Error = "error";
    public const string SetDuration = "setDuration";
    public const string SetScaleformSettings = "setScaleformSettings";
    public const string SaveScaleformSettings = "saveScaleformSettings";
    public const string Seek = "seek";
    public const string Stop = "stop";
    public const string PlayNext = "playNext";
    public const string SyncTrackPosition = "syncTrackPosition";
    public const string SetRenderMode = "setRenderMode";
    public const string SetLoop = "setLooped";
    public const string SetMuted = "setMuted";
    public const string SetVideoEnabled = "setVideoEnabled";
    public const string SetPaused = "setPaused";
    public const string SetQueue = "setQueue";
    public const string SetAdActive = "setAdActive";
    public const string ActivatingMediaPlayer = "attending-media-player";
    public const string DeactivatingMediaPlayer = "leaving-media-player";
    public const string YoutubeAdStatus = "youtube-ad-status";
}