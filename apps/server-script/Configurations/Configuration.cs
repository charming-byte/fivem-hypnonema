using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Hypnonema.Shared;
using Hypnonema.Shared.Configurations;
using Hypnonema.Shared.Diagnostics;
using Hypnonema.Shared.Media;
using Vector2 = Hypnonema.Shared.Vector2;
using Vector3 = Hypnonema.Shared.Vector3;

namespace Hypnonema.Server.Configurations;

public sealed class Configuration : IConfiguration
{
    private static readonly Regex RendererFileRegex = new(@"hypnonema_texture_renderer\d+\.+gfx");

    private static readonly Screen DefaultScreen = new()
    {
        Id = "10a1f6a5-5c2b-47f3-86c5-b88c7e61dff5",
        Name = "Hypnonema",
        Scaleform = new ScaleformRenderConfiguration
        {
            Transform = new ScaleformTransform
            {
                Position = new Vector3 { X = -1678.949f, Y = -928.3431f, Z = 20.629032f },
                Rotation = new Vector3 { X = 0f, Y = 0f, Z = -140f },
                Scale = new Vector3 { X = 0.969999969f, Y = 0.484999985f, Z = -0.1f }
            },
            Texture = new ScaleformTexture { Dimension = new Size2(1280, 720), Position = Vector2.Zero }
        },
        RenderDistance = 200f
    };

    public static string ConfigurationDirectory =
        Path.Combine(API.GetResourcePath(API.GetCurrentResourceName()), "config");

    private Configuration(string commandName, float defaultRange, bool disableIdleCam,
        LogLevel logLevel, List<Model> models, List<Screen> screens, int scaleformLimit,
        string duiUrl, int duiWidth, int duiHeight, TimeSpan duiTimeout, int maxQueueSize, bool permissionsEnabled,
        bool commandRestricted, YouTubeAdQuorumConfiguration youtubeAdQuorum)
    {
        CommandName = commandName;
        DefaultRange = defaultRange;
        DisableIdleCam = disableIdleCam;
        LogLevel = logLevel;
        Models = models;
        Screens = screens;
        ScaleformLimit = scaleformLimit;
        DuiUrl = duiUrl;
        DuiWidth = duiWidth;
        DuiHeight = duiHeight;
        DuiTimeout = duiTimeout;
        MaxQueueSize = maxQueueSize;
        PermissionsEnabled = permissionsEnabled;
        CommandRestricted = commandRestricted;
        YouTubeAdQuorum = youtubeAdQuorum;
    }

    public static Configuration Default => new(
        "hypnonema",
        200f,
        true,
        LogLevel.Information,
        [],
        [DefaultScreen],
        5,
        $"nui://{API.GetCurrentResourceName()}/wwwroot/index.html",
        1280,
        720,
        TimeSpan.FromSeconds(5),
        10,
        true,
        false,
        new YouTubeAdQuorumConfiguration()
    );

    public TimeSpan DuiTimeout { get; set; }

    public YouTubeAdQuorumConfiguration YouTubeAdQuorum { get; }

    public string CommandName { get; }

    public float DefaultRange { get; }

    public bool DisableIdleCam { get; }

    public bool CommandRestricted { get; }

    public LogLevel LogLevel { get; }

    public List<Model> Models { get; }

    public List<Screen> Screens { get; }

    public int MaxQueueSize { get; }

    public int ScaleformLimit { get; }

    public string DuiUrl { get; }

    public int DuiWidth { get; }

    public int DuiHeight { get; }

    public bool PermissionsEnabled { get; }

    public static Configuration Load()
    {
        var configuration = Default;

        try
        {
            var screens = LoadConfiguration<ScreensConfiguration>(ConfigurationDirectory, "screens.yaml").Screens;
            var models = LoadConfiguration<ModelsConfiguration>(ConfigurationDirectory, "models.yaml").Models;
            var config = LoadConfiguration<ResourceConfiguration>(ConfigurationDirectory, "config.yaml");
            var scaleformLimit = GetAmountOfUsableTextureRenderers();

            configuration = new Configuration(config.CommandName, config.DefaultRange, config.DisableIdleCam,
                config.LogLevel, models, screens, scaleformLimit,
                config.Dui.Url, config.Dui.Width, config.Dui.Height, config.Dui.Timeout, config.MaxQueueSize,
                config.PermissionsEnabled, config.CommandRestricted, config.YoutubeAdQuorum);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"^1Loading configuration failed. Error: {exception}^7");
            Debug.WriteLine("^1Using default configuration.^7");
            Debug.WriteLine($"^1{configuration}^7");
        }

        return configuration;
    }

    private static T LoadConfiguration<T>(string path, string fileName)
    {
        return Yaml.Deserialize<T>(File.ReadAllText(Path.Combine(path, fileName)));
    }

    private static int GetAmountOfUsableTextureRenderers()
    {
        var streamDirectory = Path.Combine(API.GetResourcePath(API.GetCurrentResourceName()), "stream");

        return Directory.GetFiles(streamDirectory, "*.gfx").Where(path => RendererFileRegex.IsMatch(path)).ToList()
            .Count;
    }

    public override string ToString()
    {
        var builder = new StringBuilder();

        builder.AppendLine("Configuration:");
        builder.AppendLine("--------------");

        foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(this))
        {
            var name = descriptor.Name;
            var value = descriptor.GetValue(this);

            builder.AppendLine($"{name}: {value}");
        }

        builder.AppendLine("--------------");

        return builder.ToString();
    }
}