using System;

namespace Hypnonema.Server.Media;

public static class YouTubeMedia
{
    public static bool IsYouTubeUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;

        var host = uri.IdnHost;

        return IsHostOrSubdomain(host, "youtube.com") ||
               IsHostOrSubdomain(host, "youtube-nocookie.com") ||
               IsHostOrSubdomain(host, "youtubeeducation.com") ||
               IsHostOrSubdomain(host, "youtu.be");
    }

    private static bool IsHostOrSubdomain(string host, string expectedHost)
    {
        return host.Equals(expectedHost, StringComparison.OrdinalIgnoreCase) ||
               host.EndsWith("." + expectedHost, StringComparison.OrdinalIgnoreCase);
    }
}