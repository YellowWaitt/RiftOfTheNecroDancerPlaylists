using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

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

        var parts = duration.Split(':');
        var seconds = 0;
        foreach (var (part, factor) in parts.Zip([1, 60, 60], (p, f) => (p, f)))
        {
            if (!int.TryParse(part, out int value))
            {
                return TimeSpan.Zero;
            }
            seconds = seconds * factor + value;
        }

        return TimeSpan.FromSeconds(seconds);
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

    public static T Deserialize<T>(string filePath) where T : new()
    {
        if (!File.Exists(filePath))
        {
            return new T();
        }

        var json = File.ReadAllText(filePath, Encoding.UTF8);
        return JsonConvert.DeserializeObject<T>(json);
    }
}
