using System;
using Hypnonema.Shared;

namespace Hypnonema.Server.Media;

public sealed class AdQuorumPlaybackController
{
    private readonly Func<bool> canHoldPlaybackAtStart;
    private readonly Action holdPlaybackAtStart;
    private readonly Func<bool> isPaused;
    private readonly Func<bool> isYouTube;
    private readonly Action<bool> setPaused;
    private readonly AdQuorumTracker tracker;

    private bool holdingPlaybackAtStart;
    private bool pausedForAdQuorum;

    public AdQuorumPlaybackController(double threshold, TimeSpan waitTimeout, Func<bool> isYouTube,
        Func<bool> isPaused, Func<bool> canHoldPlaybackAtStart, Action<bool> setPaused, Action holdPlaybackAtStart,
        Func<DateTimeOffset>? utcNow = null)
    {
        this.isYouTube = isYouTube ?? throw new ArgumentNullException(nameof(isYouTube));
        this.isPaused = isPaused ?? throw new ArgumentNullException(nameof(isPaused));
        this.canHoldPlaybackAtStart = canHoldPlaybackAtStart ??
                                      throw new ArgumentNullException(nameof(canHoldPlaybackAtStart));
        this.setPaused = setPaused ?? throw new ArgumentNullException(nameof(setPaused));
        this.holdPlaybackAtStart = holdPlaybackAtStart ??
                                   throw new ArgumentNullException(nameof(holdPlaybackAtStart));
        tracker = new AdQuorumTracker(threshold, waitTimeout, utcNow);
    }

    public void AddViewer(string viewerHandle)
    {
        tracker.AddViewer(viewerHandle);
        Update();
    }

    public void RemoveViewer(string viewerHandle)
    {
        tracker.RemoveViewer(viewerHandle);
        Update();
    }

    public void ReportAdStatus(string viewerHandle, YoutubeAdStatus status)
    {
        if (!isYouTube()) return;

        tracker.ReportAdStatus(viewerHandle, status);
        Update();
    }

    public bool IsAdActive => pausedForAdQuorum;

    public void TrackChanged()
    {
        tracker.Reset();
        holdingPlaybackAtStart = false;
        ReleaseQuorumPause();
    }

    public void Update()
    {
        tracker.Update();

        var shouldPause = isYouTube() && tracker.ShouldWait;

        if (shouldPause)
        {
            var shouldHoldAtStart = tracker.IsPreRollQuorumReached && canHoldPlaybackAtStart() &&
                                    !holdingPlaybackAtStart &&
                                    (!isPaused() || pausedForAdQuorum);

            if (shouldHoldAtStart)
            {
                pausedForAdQuorum = true;
                holdingPlaybackAtStart = true;
                holdPlaybackAtStart();

                return;
            }

            if (!isPaused())
            {
                pausedForAdQuorum = true;
                setPaused(true);
            }

            return;
        }

        ReleaseQuorumPause();
    }

    private void ReleaseQuorumPause()
    {
        if (!pausedForAdQuorum) return;

        pausedForAdQuorum = false;
        holdingPlaybackAtStart = false;
        if (isPaused()) setPaused(false);
    }
}