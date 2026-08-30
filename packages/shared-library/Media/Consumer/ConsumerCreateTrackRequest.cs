namespace Hypnonema.Shared.Media.Consumer;

public sealed class ConsumerCreateTrackRequest
{
    public string Url { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string ThumbnailUrl { get; set; } = string.Empty;
}