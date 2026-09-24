using System;
using System.Collections.Generic;
using Hypnonema.Shared;

namespace Hypnonema.Server.Media;

public sealed class AdQuorumTracker
{
    private readonly HashSet<string> activeAdReports = [];
    private readonly HashSet<string> activePreRollReports = [];
    private readonly HashSet<string> activeViewers = [];
    private readonly HashSet<string> expiredAdReports = [];
    private readonly double threshold;
    private readonly Func<DateTimeOffset> utcNow;
    private readonly TimeSpan waitTimeout;

    private DateTimeOffset? waitingStartedAt;

    public AdQuorumTracker(double threshold, TimeSpan waitTimeout, Func<DateTimeOffset>? utcNow = null)
    {
        if (double.IsNaN(threshold) || double.IsInfinity(threshold) || threshold <= 0 || threshold > 1)
            throw new ArgumentOutOfRangeException(nameof(threshold), threshold,
                "The ad quorum threshold must be a finite share greater than 0 and at most 1.");
        if (waitTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(waitTimeout), waitTimeout,
                "The ad quorum wait timeout must be greater than zero.");

        this.threshold = threshold;
        this.waitTimeout = waitTimeout;
        this.utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public bool IsQuorumReached =>
        activeViewers.Count > 0 && (double)activeAdReports.Count / activeViewers.Count >= threshold;

    public bool IsPreRollQuorumReached =>
        activeViewers.Count > 0 && (double)activePreRollReports.Count / activeViewers.Count >= threshold;

    public bool ShouldWait
    {
        get
        {
            if (waitingStartedAt == null) return false;

            return utcNow() - waitingStartedAt.Value < waitTimeout;
        }
    }

    public void AddViewer(string viewerHandle)
    {
        activeViewers.Add(viewerHandle);

        UpdateWaitingState();
    }

    public void RemoveViewer(string viewerHandle)
    {
        activeViewers.Remove(viewerHandle);
        activeAdReports.Remove(viewerHandle);
        activePreRollReports.Remove(viewerHandle);
        expiredAdReports.Remove(viewerHandle);

        UpdateWaitingState();
    }

    public void Update()
    {
        if (waitingStartedAt == null || ShouldWait) return;

        foreach (var viewerHandle in activeAdReports) expiredAdReports.Add(viewerHandle);

        activeAdReports.Clear();
        activePreRollReports.Clear();
        waitingStartedAt = null;
    }

    public void ReportAdStatus(string viewerHandle, YoutubeAdStatus status)
    {
        if (!activeViewers.Contains(viewerHandle)) return;

        if (status != YoutubeAdStatus.Inactive)
        {
            // Still stuck in the ad the wait already timed out on, so this report must not arm the
            // quorum again. It counts again once the viewer reports the ad as over.
            if (expiredAdReports.Contains(viewerHandle)) return;

            activeAdReports.Add(viewerHandle);
            if (status == YoutubeAdStatus.PreRoll)
                activePreRollReports.Add(viewerHandle);
            else
                activePreRollReports.Remove(viewerHandle);
        }
        else
        {
            activeAdReports.Remove(viewerHandle);
            activePreRollReports.Remove(viewerHandle);
            expiredAdReports.Remove(viewerHandle);
        }

        UpdateWaitingState();
    }

    public void Reset()
    {
        activeAdReports.Clear();
        activePreRollReports.Clear();
        expiredAdReports.Clear();
        waitingStartedAt = null;
    }

    private void UpdateWaitingState()
    {
        if (activeAdReports.Count == 0)
        {
            waitingStartedAt = null;

            return;
        }

        if (waitingStartedAt == null && IsQuorumReached)
            waitingStartedAt = utcNow();
    }
}