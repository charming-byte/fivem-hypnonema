using System;
using Hypnonema.Shared.Diagnostics;

namespace Hypnonema.Shared.Configurations;

public sealed class ResourceConfiguration
{
    public string CommandName { get; set; } = "hypnonema";

    public float DefaultRange { get; set; }

    public bool PermissionsEnabled { get; set; } = true;

    public bool CommandRestricted { get; set; }

    public bool DisableIdleCam { get; set; }

    public int MaxQueueSize { get; set; }

    public LogLevel LogLevel { get; set; }

    public YouTubeAdQuorumConfiguration YoutubeAdQuorum { get; set; } = new();

    public DuiConfig Dui { get; set; } = DuiConfig.Default;

    public class DuiConfig
    {
        public string Url { get; set; } = string.Empty;

        public TimeSpan Timeout { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public static DuiConfig Default => new()
        {
            Timeout = TimeSpan.FromSeconds(5), Width = 1280, Height = 720, Url = "nui://hypnonema/wwwroot/index.html"
        };
    }
}