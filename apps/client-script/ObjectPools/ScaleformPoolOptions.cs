using System;

namespace Hypnonema.Client.ObjectPools;

public sealed class ScaleformPoolOptions
{
    public ScaleformPoolOptions(TimeSpan loadTimeout)
    {
        LoadTimeout = loadTimeout;
    }

    public TimeSpan LoadTimeout { get; }
}