using System;
using Hypnonema.Shared.Communications;
using Hypnonema.Shared.Extensions;
using Hypnonema.Shared.Media;
using Newtonsoft.Json;

namespace Hypnonema.Client.Media;

public sealed class Track : ITrack
{
    [JsonConstructor]
    public Track()
    {
    }

    public Track(string url, string title, string thumbnailUrl, IClient? requestedBy, int duration = -1)
    {
        Url = url;
        Title = title;
        ThumbnailUrl = thumbnailUrl;
        Duration = duration;
        RequestedBy = requestedBy;
        Position = 0;
    }

    [JsonIgnore] public bool IsLiveStream => Duration == -1;

    public int Duration { get; set; }

    public string Title { get; set; }

    public string Url { get; set; }

    public string ThumbnailUrl { get; set; }

    public double Position { get; set; }

    public IClient? RequestedBy { get; set; }

    public bool HasEnded()
    {
        return Duration != -1 && Position >= Duration;
    }

    public void SetDuration(int duration)
    {
        Duration = duration;
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
    }

    public void Resume()
    {
    }

    public void Seek(double time)
    {
        if (IsLiveStream) return;

        Position = time;
    }

    public void Reset()
    {
        Position = 0;
    }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Url) && Url.IsValidUrl();
    }

    public void OnTick()
    {
        // Position is authoritative from the server; it's only ever set via Seek()
        // (driven by the periodic SyncTrackPosition RPC), never predicted locally here.
    }

    public event TrackEndedEventHandler? Ended;
}