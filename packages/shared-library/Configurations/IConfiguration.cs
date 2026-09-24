using System.Collections.Generic;
using Hypnonema.Shared.Diagnostics;
using Hypnonema.Shared.Media;

namespace Hypnonema.Shared.Configurations;

public interface IConfiguration
{
    public string CommandName { get; }

    public float DefaultRange { get; }

    public bool DisableIdleCam { get; }

    public int MaxQueueSize { get; }

    public LogLevel LogLevel { get; }

    public List<Model> Models { get; }

    public List<Screen> Screens { get; }

    public int ScaleformLimit { get; }

    public string DuiUrl { get; }

    public int DuiWidth { get; }

    public int DuiHeight { get; }

    public bool PermissionsEnabled { get; }

    public bool CommandRestricted { get; }
}