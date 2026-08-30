using System;

namespace Hypnonema.Client.Extensions;

public static class DateTimeExtensions
{
    public static bool HasTimedOut(this DateTime startTime, TimeSpan timeout)
    {
        return DateTime.UtcNow - startTime >= timeout;
    }
}