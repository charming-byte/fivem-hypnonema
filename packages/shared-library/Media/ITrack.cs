using System;
using Hypnonema.Shared.Communications;

namespace Hypnonema.Shared.Media;

public delegate void TrackEndedEventHandler(object sender, EventArgs e);

public interface ITrack
{
    public int Duration { get; }

    public string Title { get; }

    public string Url { get; }

    public string ThumbnailUrl { get; }

    public double Position { get; }

    public IClient? RequestedBy { get; }

    public bool HasEnded();

    void SetDuration(int duration);

    void Play(ITrack track);

    void Pause();

    void Resume();

    void Seek(double time);

    void Reset();

    bool IsValid();

    void OnTick();

    event TrackEndedEventHandler Ended;

    string ToString();
}