using System;
using System.Globalization;
using System.Reflection;

namespace RiftOfTheNecroDancerPlaylists;

internal static class Utils
{
    public static MethodInfo Method<T>(string method)
    {
        return typeof(T).GetMethod(
            method,
            BindingFlags.NonPublic | BindingFlags.Instance
        );
    }

    public static FieldInfo Field<T>(string field)
    {
        return typeof(T).GetField(
            field,
            BindingFlags.NonPublic | BindingFlags.Instance
        );
    }

    public static TimeSpan ParseDuration(string duration)
    {
        if (string.IsNullOrWhiteSpace(duration))
            return TimeSpan.Zero;

        string[] formats =
        [
            @"h\:m\:s",
            @"hh\:mm\:ss",
            @"m\:s",
            @"mm\:ss",
            @"s",
            @"ss"
        ];

        if (TimeSpan.TryParseExact(duration.Trim(), formats, CultureInfo.InvariantCulture, out TimeSpan result))
        {
            return result;
        }

        return TimeSpan.Zero;
    }

    public static string FormatDuration(TimeSpan duration)
    {
        int hours = duration.Hours + duration.Days * 24;
        int minutes = duration.Minutes;
        int seconds = duration.Seconds;

        if (hours > 0)
            return $"{hours}:{minutes:D2}:{seconds:D2}";
        else
            return $"{minutes}:{seconds:D2}";
    }
}
