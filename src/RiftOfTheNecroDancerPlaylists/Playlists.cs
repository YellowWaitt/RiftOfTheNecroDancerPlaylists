using System;
using System.Collections.Generic;

namespace RiftOfTheNecroDancerPlaylists;

[Serializable]
internal class PlaylistsJson
{
    public List<PlaylistJson> Playlists = [];

    public static PlaylistsJson Deserialize()
    {
        return Utils.Deserialize<PlaylistsJson>(Plugin.Path.PlaylistsJson);
    }
}

[Serializable]
internal class PlaylistJson
{
    public string Name;
    public string Cover;
    public List<string> Tracks;
}