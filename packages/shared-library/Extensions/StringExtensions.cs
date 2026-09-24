using System;
using System.Globalization;
using System.Text;

namespace Hypnonema.Shared.Extensions;

public static class StringExtensions
{
    public static bool IsValidUrl(this string s)
    {
        return Uri.TryCreate(s, UriKind.Absolute, out var uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    public static string Pluralize(this string str, int value, string extension = "s", CultureInfo? culture = null)
    {
        var val = value.ToString(culture ?? CultureInfo.InvariantCulture);
        return value == 1 ? $"{val} {str}" : $"{val} {str}{extension}";
    }

    public static string ToSlug(this string value)
    {
        // Hand-rolled instead of Regex: FiveM's Mono CLR fails to verify System.Text.RegularExpressions
        // at runtime on the client (Regex.ValidateMatchTimeout is inaccessible), so any Regex call throws.
        var lower = (value ?? string.Empty).ToLowerInvariant();
        var sb = new StringBuilder(lower.Length);
        var pendingDash = false;

        foreach (var c in lower)
        {
            if (c is >= 'a' and <= 'z' || c is >= '0' and <= '9')
            {
                if (pendingDash && sb.Length > 0) sb.Append('-');
                pendingDash = false;
                sb.Append(c);
            }
            else
            {
                pendingDash = true;
            }
        }

        return sb.Length > 0 ? sb.ToString() : "screen";
    }
}