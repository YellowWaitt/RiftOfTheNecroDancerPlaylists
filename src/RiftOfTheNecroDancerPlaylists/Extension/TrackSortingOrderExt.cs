using System;
using System.Linq;
using Shared.TrackSelection;

namespace RiftOfTheNecroDancerPlaylists.Extension;

internal static class TrackSortingOrderExt
{
    static readonly TrackSortingOrder[] PlaylistsOrders = [
        // TrackSortingOrder.Character,
        // TrackSortingOrder.IntensityAscending,
        // TrackSortingOrder.IntensityDescending,
        // TrackSortingOrder.BPMAscending,
        // TrackSortingOrder.BPMDescending,
        TrackSortingOrder.TitleAscending,
        // TrackSortingOrder.TitleDescending,
        // TrackSortingOrder.ArtistAscending,
        TrackSortingOrder.ArtistDescending,
        // TrackSortingOrder.LetterGradeAscending,
        // TrackSortingOrder.LetterGradeDescending,
        // TrackSortingOrder.PlayCountAscending,
        // TrackSortingOrder.PlayCountDescending,
        // TrackSortingOrder.MostRecentlyPlayed,
        // TrackSortingOrder.DateAdded
    ];
    static readonly TrackSortingOrder[] UserPlaylistOrders = [
        TrackSortingOrder.Character,
        TrackSortingOrder.IntensityAscending,
        // TrackSortingOrder.IntensityDescending,
        TrackSortingOrder.BPMAscending,
        // TrackSortingOrder.BPMDescending,
        TrackSortingOrder.TitleAscending,
        // TrackSortingOrder.TitleDescending,
        TrackSortingOrder.ArtistAscending,
        // TrackSortingOrder.ArtistDescending,
        // TrackSortingOrder.LetterGradeAscending,
        TrackSortingOrder.LetterGradeDescending,
        // TrackSortingOrder.PlayCountAscending,
        TrackSortingOrder.PlayCountDescending,
        TrackSortingOrder.MostRecentlyPlayed,
        TrackSortingOrder.DateAdded
    ];

    public static bool IsSortingOrderAvailableForPlaylists(TrackSortingOrder order)
    {
        return PlaylistsOrders.Contains(order);
    }

    public static bool IsSortingOrderAvailableForUserPlaylist(TrackSortingOrder order)
    {
        return UserPlaylistOrders.Contains(order);
    }
}
