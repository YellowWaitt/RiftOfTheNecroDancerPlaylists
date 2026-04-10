using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using RiftOfTheNecroDancerPlaylists.Patches;
using Shared.TrackSelection;

namespace RiftOfTheNecroDancerPlaylists;

[Serializable]
internal class Settings
{
    public int PlaylistsSortingOrder = (int)TrackSortingOrder.ArtistDescending;
    public int TracksSortingOrder = (int)TrackSortingOrder.TitleAscending;
    public int TracksCustomSortingOrder = (int)TrackSortingOrder.Character;
    public int PlaylistModeSelected = (int)PlaylistMode.ArtistPlaylists;

    public void Serialize()
    {
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(Plugin.Path.SettingsJson, json, Encoding.UTF8);
    }

    public static Settings Deserialize()
    {
        return Utils.Deserialize<Settings>(Plugin.Path.SettingsJson);
    }
}
