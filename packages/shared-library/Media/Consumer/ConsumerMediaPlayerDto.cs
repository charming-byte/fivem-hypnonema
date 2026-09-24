using System.Collections.Generic;
using System.Linq;

namespace Hypnonema.Shared.Media.Consumer;

/// <summary>
/// Consumer-facing snapshot of a media player's state. Field-for-field parity with the internal
/// <see cref="MediaPlayerDTO" /> (the internal RPC contract), but deliberately its own type so Hypnonema can change
/// that internal DTO freely without breaking the Consumer contract.
/// </summary>
public sealed class ConsumerMediaPlayerDto(
    int handle,
    string label,
    Target target,
    ConsumerTrackDto track,
    IReadOnlyList<ConsumerTrackDto> queue,
    ScaleformRenderConfiguration scaleformRenderConfiguration,
    bool looped,
    bool muted,
    bool paused,
    bool videoEnabled,
    RenderMode renderMode)
{
    public int Handle { get; } = handle;

    public string Label { get; } = label;

    public Target Target { get; } = target;

    public ConsumerTrackDto Track { get; } = track;

    public IReadOnlyList<ConsumerTrackDto> Queue { get; } = queue;

    public ScaleformRenderConfiguration ScaleformRenderConfiguration { get; } = scaleformRenderConfiguration;

    public bool Looped { get; } = looped;

    public bool Muted { get; } = muted;

    public bool Paused { get; } = paused;

    public bool VideoEnabled { get; } = videoEnabled;

    public RenderMode RenderMode { get; } = renderMode;

    public static ConsumerMediaPlayerDto From(IMediaPlayer mediaPlayer)
    {
        return new ConsumerMediaPlayerDto(
            mediaPlayer.Handle.Value,
            mediaPlayer.Label,
            mediaPlayer.Target,
            ConsumerTrackDto.From(mediaPlayer.Track),
            mediaPlayer.Queue.Select(ConsumerTrackDto.From).ToList(),
            mediaPlayer.Scaleform,
            mediaPlayer.Looped,
            mediaPlayer.Muted,
            mediaPlayer.Paused,
            mediaPlayer.Video,
            mediaPlayer.RenderMode);
    }
}