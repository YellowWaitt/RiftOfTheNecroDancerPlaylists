using Shared.UGC.Steam;
using Steamworks;
using UnityEngine;

namespace RiftOfTheNecroDancerPlaylists.Extension;

internal static class SteamWorkshopUgcTrackProviderExt
{
    public static void SearchArtist(string artistName)
    {
        var search_url = $"https://steamcommunity.com/workshop/browse/?appid=2073250&searchtext={artistName}";
        if (SteamWorkshopUgcTrackProvider.Available && SteamUtils.IsOverlayEnabled())
        {
            SteamFriends.ActivateGameOverlayToWebPage(search_url);
        }
        else
        {
            Application.OpenURL(search_url);
        }
    }
}
