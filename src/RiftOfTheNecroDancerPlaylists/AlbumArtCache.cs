using System;
using System.Collections.Generic;
using System.IO;
using Shared.TrackData;
using UnityEngine;

namespace RiftOfTheNecroDancerPlaylists;

internal static class AlbumArtCache
{
    private static readonly string _processedAlbumsDir = Path.Combine(Plugin.Path.Cache, "albums");
    private static readonly HashSet<string> _grayedOutAlbumArts = [];

    public static void Initialize()
    {
        Directory.CreateDirectory(_processedAlbumsDir);

        foreach (FileInfo file in new DirectoryInfo(_processedAlbumsDir).GetFiles("*.png"))
        {
            _grayedOutAlbumArts.Add(Path.GetFileNameWithoutExtension(file.Name));
        }
    }

    private static Texture2D LoadAlbumArt(string original)
    {
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(original);
        }
        catch (Exception)
        {
            // This happen when the track is downloading.
            // We just ignore it and everything goes fine.
            return null;
        }
        var albumArt = new Texture2D(1, 1);
        return albumArt.LoadImage(bytes) ? albumArt : null;
    }

    private static Texture2D ConvertToGrayscale(Texture2D original)
    {
        var grayscale = new Texture2D(original.width, original.height);
        var pixels = original.GetPixels();

        for (int i = 0; i < pixels.Length; i++)
        {
            float grayValue = pixels[i].r * 0.3f + pixels[i].g * 0.59f + pixels[i].b * 0.11f;
            grayValue = grayValue * 0.8f + 0.1f;
            pixels[i] = new Color(grayValue, grayValue, grayValue, pixels[i].a);
        }

        grayscale.SetPixels(pixels);
        grayscale.Apply();
        return grayscale;
    }

    // TODO: find a way to not make the game freeze while this is running for high number of albums
    public static string GrayedOutAlbumArt(ITrackMetadata track)
    {
        var processedName = Path.GetFileName(Path.GetDirectoryName(track.AlbumArtUrl));
        var processedPath = Path.ChangeExtension(Path.Combine(_processedAlbumsDir, processedName), ".png");
        var trackAlbumPath = new Uri(track.AlbumArtUrl).LocalPath;
        if (!_grayedOutAlbumArts.Contains(processedName)
            || File.GetLastWriteTime(trackAlbumPath) > File.GetLastWriteTime(processedPath))
        {
            var original = LoadAlbumArt(trackAlbumPath);
            if (original is null)
            {
                return track.AlbumArtUrl;
            }
            var gray = ConvertToGrayscale(original);
            File.WriteAllBytes(processedPath, gray.EncodeToPNG());
            UnityEngine.Object.Destroy(original);
            UnityEngine.Object.Destroy(gray);
            _grayedOutAlbumArts.Add(processedName);
        }
        return processedPath;
    }
}
