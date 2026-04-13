using Shared.TrackSelection;
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

    public static string GetPlaylistOrderName(TrackSortingOrder order)
    {
        return order switch
        {
            TrackSortingOrder.ArtistAscending => Localizer.GetText("ROTNDP_SortingOrderPlaylistSizeAscendingLabel"),
            TrackSortingOrder.ArtistDescending => Localizer.GetText("ROTNDP_SortingOrderPlaylistSizeDescendingLabel"),
            _ => Localizer.GetText($"TrackSelectionSortingOrder{TrackSelectionSceneController.GetTrackSortingOrderEnumName(order)}Label"),
        };
    }

    public static string GetUserPlaylistOrderName(TrackSortingOrder order)
    {
        return order switch
        {
            TrackSortingOrder.Character => Localizer.GetText("ROTNDP_SortingOrderPlaylistOrderLabel"),
            _ => Localizer.GetText($"TrackSelectionSortingOrder{TrackSelectionSceneController.GetTrackSortingOrderEnumName(order)}Label"),
        };
    }
}
