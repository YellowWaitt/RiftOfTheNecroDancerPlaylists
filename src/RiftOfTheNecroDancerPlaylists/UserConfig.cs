using BepInEx.Configuration;

namespace RiftOfTheNecroDancerPlaylists;

internal class UserConfig
{
    public ConfigEntry<int> MinTracksToShowArtistPlaylist;
    public ConfigEntry<int> MinTracksToShowCreatorPlaylist;
    public ConfigEntry<bool> RoundBpm;
    public ConfigEntry<bool> TurnOnModOnForNonTestedVersion;

    public void Initialize(ConfigFile config)
    {
        MinTracksToShowArtistPlaylist = config.Bind(
            "Playlists", "Tracks to show artist playlist", 1,
            new ConfigDescription(
                "The minimum number of track that an artist playlist must contain to be shown.",
                new AcceptableValueRange<int>(1, 10))
        );
        MinTracksToShowCreatorPlaylist = config.Bind(
            "Playlists", "Tracks to show creator playlist", 1,
            new ConfigDescription(
                "The minimum number of track that a creator playlist must contain to be shown.",
                new AcceptableValueRange<int>(1, 10))
        );
        RoundBpm = config.Bind(
            "Tracks", "Round BPM", true,
            "Round the BPM displayed on tracks."
        );
        TurnOnModOnForNonTestedVersion = config.Bind(
            "Config", "Turn on mod for non tested game version", true,
            "If you do not want to wait for the mod to update when new game update arise turn this on (recommanded). "
            + "Turn this off if you encounter issues and restart the game."
        );
    }
}
