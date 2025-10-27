using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Shared.TrackData;
using Shared.TrackSelection;

namespace RiftOfTheNecroDancerPlaylists.Patches;

[HarmonyPatch(typeof(CustomTrackSelectionOptionGroup))]
internal static class CustomTrackSelectionOptionGroupPatch
{
    private static MethodInfo _navigateTrackList = Utils.Method<CustomTrackSelectionOptionGroup>("NavigateTrackList");

    [HarmonyPrefix]
    [HarmonyPatch("NavigateTrackList")]
    private static bool NavigateTrackList_Prefix(
        CustomTrackSelectionOptionGroup __instance,
        List<ITrackMetadata> ____trackMetaData,
        int ____numActiveTrackOptions,
        int ____selectionIndex,
        int navigationAmount
    )
    {
        if (1 < ____trackMetaData.Count && ____trackMetaData.Count < ____numActiveTrackOptions)
        {
            var newNavigationAmout = 0;
            if (____selectionIndex + navigationAmount < 0)
            {
                newNavigationAmout = ____trackMetaData.Count - (-navigationAmount % ____trackMetaData.Count);
            }
            else if (____selectionIndex + navigationAmount >= ____trackMetaData.Count)
            {
                newNavigationAmout = -____trackMetaData.Count + (navigationAmount % ____trackMetaData.Count);
            }
            if (newNavigationAmout != 0)
            {
                for (int i = 0; i < Math.Abs(newNavigationAmout); ++i)
                {
                    _navigateTrackList.Invoke(__instance, [Math.Sign(newNavigationAmout)]);
                }
                return false;
            }
        }
        return true;
    }
}
