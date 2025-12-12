using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Shared.TrackData;
using StbImageSharp;
using StbImageWriteSharp;

namespace RiftOfTheNecroDancerPlaylists;

internal static class AlbumArtCache
{
    private static readonly string _processedAlbumsDir = Path.Combine(Plugin.Path.Cache, "albums");
    private static readonly object _lock = new();
    private static readonly HashSet<string> _grayedOutAlbumArts = [];
    private static readonly HashSet<string> _processing = [];

    public static void Initialize()
    {
        Directory.CreateDirectory(_processedAlbumsDir);

        foreach (FileInfo file in new DirectoryInfo(_processedAlbumsDir).GetFiles("*.png"))
        {
            _grayedOutAlbumArts.Add(Path.GetFileNameWithoutExtension(file.Name));
        }
    }

    public static string GrayedOutAlbumArt(ITrackMetadata track)
    {
        var processedName = Path.GetFileName(Path.GetDirectoryName(track.AlbumArtUrl));
        var processedPath = Path.ChangeExtension(Path.Combine(_processedAlbumsDir, processedName), ".png");
        var trackAlbumPath = new Uri(track.AlbumArtUrl).LocalPath;

        lock (_lock)
        {
            if (_grayedOutAlbumArts.Contains(processedName)
                && File.GetLastWriteTime(trackAlbumPath) <= File.GetLastWriteTime(processedPath))
            {
                return processedPath;
            }
            if (_processing.Contains(processedName))
            {
                return track.AlbumArtUrl;
            }
            _processing.Add(processedName);
        }

        Task.Run(async () =>
        {
            try
            {
                var bytes = await File.ReadAllBytesAsync(trackAlbumPath);
                var image = ImageResult.FromMemory(bytes, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
                var pixels = image.Data;

                for (int i = 0; i < pixels.Length; i += 4)
                {
                    var r = pixels[i];
                    var g = pixels[i + 1];
                    var b = pixels[i + 2];

                    var gray = r * 0.3f + g * 0.59f + b * 0.11f;
                    gray = gray * 0.8f + 0.1f;
                    var grayByte = (byte)gray;

                    pixels[i] = grayByte;
                    pixels[i + 1] = grayByte;
                    pixels[i + 2] = grayByte;
                }

                var writer = new ImageWriter();
                using var output = File.OpenWrite(processedPath);
                writer.WritePng(pixels, image.Width, image.Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, output);

                lock (_lock)
                {
                    _grayedOutAlbumArts.Add(processedName);
                    _processing.Remove(processedName);
                }
            }
            catch (Exception e)
            {
                Plugin.Logger.LogWarning($"Exception while processing {processedName}: {e.Message}");
                if (File.Exists(processedPath)) File.Delete(processedPath);
                lock (_lock)
                {
                    _processing.Remove(processedName);
                }
            }
        });

        return track.AlbumArtUrl;
    }
}
