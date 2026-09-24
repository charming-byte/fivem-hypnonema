namespace Hypnonema.Shared;

public enum YoutubeAdStatus
{
    Inactive,
    PreRoll,
    MidRoll
}

public static class Dui
{
    public class DuiEvents
    {
        public const string Seek = "seek";
        public const string Load = "load";
        public const string SetLooped = "setLooped";
        public const string SetMuted = "setMuted";
        public const string SetVideoEnabled = "setVideoEnabled";
        public const string SetPaused = "setPaused";
        public const string SetAdActive = "setAdActive";
        public const string SetVolume = "setVolume";
        public const string Reset = "reset";
        public const string SynchronizeTime = "synchronizeTime";
        public const string CalculateSpatialAudioVolume = "spatialAudioVolume";
        public const string InitSpatialAudioSystem = "initSpatialAudioSystem";
    }

    public class DuiCallbacks
    {
        public const string OnPlayerEnd = "onPlayerEnd";
        public const string OnPlayerDuration = "onPlayerDuration";
        public const string OnDocumentReady = "onDocumentReady";
        public const string OnPlayerError = "onPlayerError";
        public const string OnPlayerLoaded = "onPlayerLoaded";
        public const string OnYoutubeAdStatus = "onYoutubeAdStatus";
        public const string OnRequestClick = "onRequestClick";
    }

    /// <summary>
    ///     A point inside the browser the page wants clicked through the DUI mouse natives, in
    ///     device pixels relative to the browser's top left corner.
    /// </summary>
    public sealed class DuiClickRequest
    {
        public int X { get; set; }

        public int Y { get; set; }
    }
}