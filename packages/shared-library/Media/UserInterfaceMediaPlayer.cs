using System.Collections.Generic;

namespace Hypnonema.Shared.Media;

public sealed class UserInterfaceMediaPlayer
{
    public UserInterfaceMediaPlayer(float distance, float volume, string error, string label, uint handle,
        Target target, ITrack? currentTrack, Queue<ITrack> queue, bool looped, bool muted, bool paused,
        bool adActive, bool videoEnabled, RenderMode renderMode,
        ScaleformRenderConfiguration scaleform)
    {
        Distance = distance;
        Volume = volume;
        Error = error;
        Label = label;
        Handle = handle;
        Target = target;
        CurrentTrack = currentTrack;
        Queue = queue;
        Looped = looped;
        Muted = muted;
        Paused = paused;
        AdActive = adActive;
        VideoEnabled = videoEnabled;
        RenderMode = renderMode;
        Scaleform = scaleform;
    }

    public float Distance { get; private set; }

    public float Volume { get; }

    public string? Error { get; }

    public uint Handle { get; private set; }

    public string Label { get; }

    public Target Target { get; private set; }

    public ITrack? CurrentTrack { get; private set; }

    public Queue<ITrack>? Queue { get; private set; }

    public bool Looped { get; }

    public bool Muted { get; }

    public bool Paused { get; }

    public bool AdActive { get; }

    public bool VideoEnabled { get; }

    public RenderMode RenderMode { get; }

    public ScaleformRenderConfiguration Scaleform { get; set; }

    public static UserInterfaceMediaPlayer Default(float distance, string label, uint handle, Target target)
    {
        var renderMode = target is ModelTarget ? RenderMode.RenderTarget : RenderMode.Scaleform;
        var scaleformRenderConfiguration = target is ScreenTarget screenTarget
            ? screenTarget.Screen.Scaleform
            : ScaleformRenderConfiguration.Default;

        return new UserInterfaceMediaPlayer(distance, 1f, string.Empty, label, handle, target, null,
            new Queue<ITrack>(), false, false, false, false, true, renderMode, scaleformRenderConfiguration);
    }

    public static UserInterfaceMediaPlayer From(Screen screen, float distance, uint handle)
    {
        return Default(distance, screen.Name, handle, new ScreenTarget(screen));
    }
}