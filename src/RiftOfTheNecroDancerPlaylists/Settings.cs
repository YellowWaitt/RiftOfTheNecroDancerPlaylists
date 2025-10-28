using System;
using System.IO;
using System.Text;
using RiftOfTheNecroDancerPlaylists.Patches;
using Shared.TrackSelection;
using UnityEngine;

namespace RiftOfTheNecroDancerPlaylists;

[Serializable]
internal class Settings
{
    public int PlaylistsSortingOrder = (int)TrackSortingOrder.ArtistDescending;
    public int TracksSortingOrder = (int)TrackSortingOrder.TitleAscending;
    public int PlaylistModeSelected = (int)PlaylistMode.ArtistPlaylists;

    public void Serialize()
    {
        var json = JsonUtility.ToJson(this, true);
        File.WriteAllText(Plugin.Path.SettingsJson, json, Encoding.UTF8);
    }

    public static Settings Deserialize()
    {
        if (!File.Exists(Plugin.Path.SettingsJson))
        {
            return new Settings();
        }

        var json = File.ReadAllText(Plugin.Path.SettingsJson, Encoding.UTF8);
        return JsonUtility.FromJson<Settings>(json);
    }
}
