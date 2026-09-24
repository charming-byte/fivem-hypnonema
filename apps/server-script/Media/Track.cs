using System;
using System.Diagnostics;
using Hypnonema.Shared.Communications;
using Hypnonema.Shared.Extensions;
using Hypnonema.Shared.Media;
using Newtonsoft.Json;

namespace Hypnonema.Server.Media;

public sealed class Track : ITrack
{
    private readonly Stopwatch stopwatch;

    private bool isPaused;
    private double pausedAtElapsedSeconds;
    private double seekOffsetSeconds;
    private double seekReferenceElapsed;

    public Track(string url, string title, string thumbnailUrl, IClient? requestedBy, int duration = -1)
    {
        Url = url;
        Title = title;
        ThumbnailUrl = thumbnailUrl;
        Duration = duration;
        RequestedBy = requestedBy;

        stopwatch = Stopwatch.StartNew();
    }

    [JsonConstructor]
    public Track(string url, string title, string thumbnailUrl, IClient? requestedBy, double position, int duration)
        : this(url, title, thumbnailUrl, requestedBy, duration)
    {
        seekOffsetSeconds = position;
        seekReferenceElapsed = stopwatch.Elapsed.TotalSeconds;
        pausedAtElapsedSeconds = seekReferenceElapsed;
    }

    [JsonIgnore] public bool IsLive => Duration == -1;

    public event TrackEndedEventHandler? Ended;

    public void OnTick()
    {
        if (!IsLive && Position >= Duration) Ended?.Invoke(this, EventArgs.Empty);
    }

    public int Duration { get; private set; }

    public bool HasEnded()
    {
        return !IsLive && Position >= Duration;
    }

    public string Title { get; private set; }

    public string Url { get; private set; }

    public string ThumbnailUrl { get; private set; }

    public double Position
    {
        get
        {
            var elapsed = isPaused ? pausedAtElapsedSeconds : stopwatch.Elapsed.TotalSeconds;
            var rawPosition = seekOffsetSeconds + (elapsed - seekReferenceElapsed);

            return !IsLive && rawPosition >= Duration ? Duration : rawPosition;
        }
    }

    public IClient? RequestedBy { get; private set; }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Url) && Url.IsValidUrl();
    }

    public void SetDuration(int duration)
    {
        Duration = duration;
    }

    public void Reset()
    {
        seekOffsetSeconds = 0;
        seekReferenceElapsed = stopwatch!.Elapsed.TotalSeconds;
        pausedAtElapsedSeconds = seekReferenceElapsed;
    }

    public void Play(ITrack track)
    {
        if (!track.IsValid()) throw new ArgumentException("Track is not valid.", nameof(track));

        Url = track.Url;
        Title = track.Title;
        ThumbnailUrl = track.ThumbnailUrl;
        Duration = track.Duration;
        RequestedBy = track.RequestedBy;

        Reset();
    }

    public void Pause()
    {
        if (isPaused) return;

        pausedAtElapsedSeconds = stopwatch.Elapsed.TotalSeconds;

        isPaused = true;
    }

    public void Resume()
    {
        if (!isPaused) return;

        var now = stopwatch.Elapsed.TotalSeconds;
        var pausedDuration = now - pausedAtElapsedSeconds;

        seekReferenceElapsed += pausedDuration;

        isPaused = false;
    }

    public void Seek(double time)
    {
        if (IsLive) return;

        if (time < 1) time = 1;

        if (time > Duration) time = Duration;

        var now = stopwatch.Elapsed.TotalSeconds;

        seekOffsetSeconds = time;
        seekReferenceElapsed = now;

        if (isPaused) pausedAtElapsedSeconds = now;
    }

    public override string ToString()
    {
        return
            $"[Track: Title={Title}, Url={Url}, ThumbnailUrl={ThumbnailUrl}, Duration={Duration}, Position={Position}, RequestedBy={RequestedBy}]";
    }

    public void SetRequestingClient(IClient client)
    {
        RequestedBy = client;
    }
}