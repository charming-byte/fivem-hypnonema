namespace Hypnonema.Shared.Media.Consumer;

public sealed class ConsumerTrackDto(string url, string title, string thumbnailUrl, int duration, double position)
{
    public string Url { get; } = url;

    public string Title { get; } = title;

    public string ThumbnailUrl { get; } = thumbnailUrl;

    public int Duration { get; } = duration;

    public double Position { get; } = position;

    public static ConsumerTrackDto From(ITrack track)
    {
        return new ConsumerTrackDto(track.Url, track.Title, track.ThumbnailUrl, track.Duration, track.Position);
    }
}