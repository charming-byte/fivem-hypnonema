using System;

namespace Hypnonema.Shared.Configurations;

public sealed class YouTubeAdQuorumConfiguration
{
    public const double DefaultThreshold = 0.5;

    public static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromSeconds(30);

    private double threshold = DefaultThreshold;

    private TimeSpan waitTimeout = DefaultWaitTimeout;

    public double Threshold
    {
        get => threshold;
        set
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0 || value > 1)
                throw new ArgumentOutOfRangeException(nameof(Threshold), value,
                    "The YouTube ad quorum threshold must be a finite share greater than 0 and at most 1.");

            threshold = value;
        }
    }

    public TimeSpan WaitTimeout
    {
        get => waitTimeout;
        set
        {
            if (value <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(WaitTimeout), value,
                    "The YouTube ad quorum wait timeout must be greater than zero.");

            waitTimeout = value;
        }
    }
}