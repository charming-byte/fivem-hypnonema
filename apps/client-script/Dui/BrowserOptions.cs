using System;
using Hypnonema.Shared;

namespace Hypnonema.Client.Dui;

public sealed class BrowserOptions
{
    public BrowserOptions(string id, TimeSpan loadTimeout, Size2 dimension, string url)
    {
        Id = id;
        Url = url;
        Dimension = dimension;
        LoadTimeout = loadTimeout;
    }

    public string Id { get; }

    public string Url { get; }

    public Size2 Dimension { get; }

    public TimeSpan LoadTimeout { get; }
}