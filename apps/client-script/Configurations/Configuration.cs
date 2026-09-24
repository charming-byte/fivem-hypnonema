using System;
using System.Collections.Generic;
using Hypnonema.Shared;
using Hypnonema.Shared.Configurations;
using Hypnonema.Shared.Diagnostics;
using Hypnonema.Shared.Media;

namespace Hypnonema.Client.Configurations;

public sealed class Configuration : IConfiguration
{
    public Configuration(string commandName, float defaultRange, bool disableIdleCam,
        LogLevel logLevel, List<Model> models, List<Screen> screens, int scaleformLimit, string gfxResourceName,
        string duiUrl, int duiWidth, int duiHeight, TimeSpan duiTimeout, int maxQueueSize, bool development,
        string allowedOrigins, bool permissionsEnabled, bool commandRestricted)
    {
        CommandName = commandName;
        DefaultRange = defaultRange;
        DisableIdleCam = disableIdleCam;
        LogLevel = logLevel;
        Models = models;
        Screens = screens;
        ScaleformLimit = scaleformLimit;
        GfxResourceName = gfxResourceName;
        DuiUrl = duiUrl;
        DuiWidth = duiWidth;
        DuiHeight = duiHeight;
        DuiTimeout = duiTimeout;
        MaxQueueSize = maxQueueSize;
        Development = development;
        AllowedOrigins = allowedOrigins;
        PermissionsEnabled = permissionsEnabled;
        CommandRestricted = commandRestricted;
    }

    public TimeSpan DuiTimeout { get; }

    public bool Development { get; }

    public string AllowedOrigins { get; }
    public bool YouTubeVideoProxyEnabled { get; }

    public string CommandName { get; }
    public float DefaultRange { get; }
    public bool DisableIdleCam { get; }
    public int MaxQueueSize { get; }
    public LogLevel LogLevel { get; }
    public List<Model> Models { get; }
    public List<Screen> Screens { get; }
    public int ScaleformLimit { get; }
    public string GfxResourceName { get; }
    public string DuiUrl { get; }
    public int DuiWidth { get; }
    public int DuiHeight { get; }
    public bool PermissionsEnabled { get; }
    public bool CommandRestricted { get; }
}