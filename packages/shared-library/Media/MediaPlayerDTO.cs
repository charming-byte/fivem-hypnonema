using System.Collections.Generic;
using Hypnonema.Shared.Media.Audio;

namespace Hypnonema.Shared.Media;

public class MediaPlayerDTO(
    Handle handle,
    string label,
    Target target,
    ITrack track,
    Queue<ITrack> queue,
    ScaleformRenderConfiguration scaleformRenderConfiguration,
    bool looped,
    bool muted,
    bool paused,
    bool videoEnabled,
    RenderMode renderMode,
    AudioConfiguration audio)
{
    public Handle Handle { get; } = handle;

    public string Label { get; } = label;

    public Target Target { get; } = target;

    public ITrack Track { get; } = track;

    public Queue<ITrack> Queue { get; } = queue;

    public ScaleformRenderConfiguration ScaleformRenderConfiguration { get; } = scaleformRenderConfiguration;

    public AudioConfiguration Audio { get; } = audio;

    public bool Looped { get; } = looped;

    public bool Muted { get; } = muted;

    public bool Paused { get; } = paused;

    public bool VideoEnabled { get; } = videoEnabled;

    public RenderMode RenderMode { get; } = renderMode;

    public static MediaPlayerDTO From(IMediaPlayer mediaPlayer)
    {
        return new MediaPlayerDTO(
            mediaPlayer.Handle,
            mediaPlayer.Label,
            mediaPlayer.Target,
            mediaPlayer.Track,
            mediaPlayer.Queue,
            mediaPlayer.Scaleform,
            mediaPlayer.Looped,
            mediaPlayer.Muted,
            mediaPlayer.Paused,
            mediaPlayer.Video,
            mediaPlayer.RenderMode,
            mediaPlayer.Audio);
    }
}