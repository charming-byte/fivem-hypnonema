using System;

namespace Hypnonema.Shared.Media.Consumer;

public sealed class ConsumerScreenNotFoundException(string screenName)
    : Exception($"No screen named '{screenName}' was found in screens.yaml.")
{
    public string ScreenName { get; } = screenName;
}

public sealed class ConsumerMediaPlayerNotFoundException(int handle)
    : Exception($"No media player with handle '{handle}' was found.")
{
    public int Handle { get; } = handle;
}

public sealed class ConsumerModelNotFoundException(string prop)
    : Exception($"No model with prop '{prop}' was found in models.yaml.")
{
    public string Prop { get; } = prop;
}

public sealed class ConsumerRenderTargetInUseException(string renderTarget)
    : Exception($"RenderTarget '{renderTarget}' is already in use by an active media player.")
{
    public string RenderTarget { get; } = renderTarget;
}

public sealed class ConsumerUnsupportedTargetException(TargetType targetType)
    : Exception($"Target type '{targetType}' is not supported.")
{
    public TargetType TargetType { get; } = targetType;
}