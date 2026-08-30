using System;
using System.Collections.Generic;
using Hypnonema.Shared.Media.Audio;

namespace Hypnonema.Shared.Media;

public interface IMediaPlayer : IDisposable
{
    public Handle Handle { get; }

    public string Label { get; }

    public Target Target { get; }

    public ITrack Track { get; }

    public Queue<ITrack> Queue { get; }

    public ScaleformRenderConfiguration Scaleform { get; }

    public bool Looped { get; }

    public bool Muted { get; }

    public bool Paused { get; }

    public bool AdActive { get; }

    public bool Video { get; }

    public int MaxQueuedTracks { get; }

    public AudioConfiguration Audio { get; }

    public RenderMode RenderMode { get; }

    void SetAudioConfiguration(AudioConfiguration audio);

    void SetPaused(bool paused);

    void SetLooped(bool looped);

    void SetMuted(bool muted);

    void SetRenderMode(RenderMode renderMode);

    void SetVideoEnabled(bool isVideoEnabled);

    void Enqueue(ITrack track);

    void SetQueue(IEnumerable<ITrack> tracks);

    void SkipCurrentTrack();

    void Seek(int position);

    void SetScaleformSettings(ScaleformRenderConfiguration renderConfiguration);

    void Stop();

    void SetDuration(int duration);

    void Play(ITrack track);
}

public record Handle(int Value)
{
    public int Value { get; } = Value;

    public override string ToString()
    {
        return Value.ToString();
    }
}