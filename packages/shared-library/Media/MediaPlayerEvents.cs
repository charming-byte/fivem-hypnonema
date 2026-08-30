namespace Hypnonema.Shared.Media;

public static class MediaPlayerEvents
{
    public const string Paused = "paused";

    public const string AdActiveChanged = "adActiveChanged";

    public const string Resumed = "resumed";

    public const string LoopChanged = "loopChanged";

    public const string MutedChanged = "mutedChanged";

    public const string RenderModeChanged = "renderModeChanged";

    public const string VideoEnabled = "videoEnabled";

    public const string VideoDisabled = "videoDisabled";

    public const string Enqueued = "enqueued";

    public const string QueueChanged = "queueChanged";

    public const string TrackSkipped = "trackSkipped";

    public const string TrackPositionChanged = "trackPositionChanged";

    public const string TrackDurationChanged = "trackDurationChanged";

    public const string Played = "played";

    public const string ScaleformSettingsChanged = "scaleformSettingsChanged";

    /// <summary>
    ///     A screen this media player renders on had its geometry hot-edited (ticket 08). Carries the new
    ///     <see cref="ScaleformRenderConfiguration" />; the client moves the live drawable in place rather
    ///     than rebuilding it.
    /// </summary>
    public const string ScreenGeometryChanged = "screenGeometryChanged";

    public const string Stopped = "stopped";

    public const string SyncedTrackPosition = "syncedTrackPosition";

    public const string AudioConfigurationChanged = "audioConfigurationChanged";
}