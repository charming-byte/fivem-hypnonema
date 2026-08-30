using System;
using Hypnonema.Shared;

namespace Hypnonema.Client.ObjectPools;

public sealed class BrowserPoolOptions
{
    public BrowserPoolOptions(string url, TimeSpan loadTimeout, int width = 1280, int height = 720)
    {
        Url = url;
        LoadTimeout = loadTimeout;
        Dimension = new Size2(width, height);
    }

    public string Url { get; }

    public Size2 Dimension { get; }

    public TimeSpan LoadTimeout { get; }
}