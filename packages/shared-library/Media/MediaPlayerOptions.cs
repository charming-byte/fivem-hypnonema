using System.Collections.Generic;
using Hypnonema.Shared.Communications;
using Hypnonema.Shared.Media.Audio;

namespace Hypnonema.Shared.Media;

public class MediaPlayerOptions(
    Handle handle,
    string label,
    ITrack track,
    Target target,
    bool videoEnabled,
    bool looped,
    bool muted,
    RenderMode renderMode,
    bool paused,
    ScaleformRenderConfiguration scaleformRenderConfiguration,
    int maxQueuedTracks,
    ITickManager tickManager,
    IEventManager eventManager,
    Queue<ITrack> queue,
    AudioConfiguration audio)
{
    public Handle Handle { get; } = handle;
    public string Label { get; } = label;
    public ITrack Track { get; } = track;
    public Target Target { get; } = target;
    public bool VideoEnabled { get; } = videoEnabled;
    public bool Looped { get; } = looped;
    public bool Muted { get; } = muted;
    public RenderMode RenderMode { get; } = renderMode;
    public bool Paused { get; } = paused;
    public ScaleformRenderConfiguration ScaleformRenderConfiguration { get; } = scaleformRenderConfiguration;
    public int MaxQueuedTracks { get; } = maxQueuedTracks;
    public ITickManager TickManager { get; } = tickManager;
    public IEventManager EventManager { get; } = eventManager;

    public AudioConfiguration Audio { get; } = audio;

    public Queue<ITrack> Queue { get; } = queue;

    public static MediaPlayerOptions Default(Handle handle, string label, ITrack track, Target target,
        int maxQueuedTracks,
        ScaleformRenderConfiguration scaleformRenderConfiguration, ITickManager tickManager, IEventManager eventManager, AudioConfiguration audio)
    {
        var renderMode = target is ScreenTarget ? RenderMode.Scaleform : RenderMode.RenderTarget;
        return new MediaPlayerOptions(handle, label, track, target, true, false, false, renderMode, false,
            scaleformRenderConfiguration, maxQueuedTracks, tickManager, eventManager, new Queue<ITrack>(),
            audio);
    }

    public static MediaPlayerOptions FromDTO(MediaPlayerDTO dto, int maxQueuedTracks, ITickManager tickManager,
        IEventManager eventManager)
    {
        return new MediaPlayerOptions(dto.Handle, dto.Label, dto.Track, dto.Target, dto.VideoEnabled, dto.Looped,
            dto.Muted, dto.RenderMode, dto.Paused, dto.ScaleformRenderConfiguration, maxQueuedTracks, tickManager,
            eventManager,
            dto.Queue, dto.Audio);
    }
}