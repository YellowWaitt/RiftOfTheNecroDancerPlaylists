using TicToc.Localization;

namespace RiftOfTheNecroDancerPlaylists.Extension;

internal static class LocalizerExt
{
    public static string GetFormattedTracks(int playlistSize)
    {
        if (playlistSize == 1)
        {
            return Localizer.GetText("ROTNDP_PlaylistOneTrack");
        }
        return Localizer.GetFormattedText("ROTNDP_PlaylistSize", [playlistSize]);
    }
}
