using BepInEx.Configuration;

namespace RiftOfTheNecroDancerPlaylists;

internal static class UserConfig
{
    public static ConfigEntry<int> MinTracksToShowArtistPlaylist;
    public static ConfigEntry<int> MinTracksToShowCreatorPlaylist;
    public static ConfigEntry<bool> ShowUncompletedTracksPlaylist;
    public static ConfigEntry<bool> TrackInAllDifficulties;
    public static ConfigEntry<bool> RoundBpm;
    public static ConfigEntry<bool> TurnOnModOnForNonTestedVersion;

    public static void Initialize(ConfigFile config)
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
        ShowUncompletedTracksPlaylist = config.Bind(
            "Playlists", "Display uncompleted tracks", true,
            "Display a playlist that contains all uncompleted tracks."
        );
        TrackInAllDifficulties = config.Bind(
            "Tracks", "Display tracks for all difficulties", true,
            "Display all tracks whether they are available for selected difficulty or not."
        );
        RoundBpm = config.Bind(
            "Tracks", "Round BPM", true,
            "Round the BPM displayed on tracks."
        );
        TurnOnModOnForNonTestedVersion = config.Bind(
            "Config", "Turn on mod for non tested game version", false,
            "If you do not want to wait for the mod to update when new game update arise turn this on. "
            + "This may lead to game crash. Or not, you'll see."
        );
    }
}
