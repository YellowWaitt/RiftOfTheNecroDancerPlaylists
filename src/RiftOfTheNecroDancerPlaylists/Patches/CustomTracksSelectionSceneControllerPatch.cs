using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RiftOfTheNecroDancerPlaylists.Extension;
using Shared;
using Shared.PlayerData;
using Shared.RiftInput;
using Shared.TrackData;
using Shared.TrackSelection;
using Shared.UGC.Placeholder;
using Shared.UGC.Steam;
using TicToc.Localization;
using TicToc.Localization.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiftOfTheNecroDancerPlaylists.Patches;

[HarmonyPatch(typeof(CustomTracksSelectionSceneController))]
internal static class CustomTracksSelectionSceneControllerPatch
{
    private const string _playlistIdPrefix = "__playlist_id_";
    private const string _allPlaylistId = "__playlist_all";

    private static PlaylistCollection _playlistCollection;
    private static PlaylistMode _playlistMode;
    private static SelectedPlaylist _selectedPlaylist = new();
    private static SortingOrders _sortingOrders;

    private static GameObject _playlistModeLabel;

    private static readonly MethodInfo _fillInTrackDetails = Utils.Method<CustomTracksSelectionSceneController>("FillInTrackDetails");
    private static readonly MethodInfo _fillInTracksToDisplayForCurrentDifficulty = Utils.Method<CustomTracksSelectionSceneController>("FillInTracksToDisplayForCurrentDifficulty");
    private static readonly MethodInfo _getTrackMetadataIndexFromLevelId = Utils.Method<CustomTracksSelectionSceneController>("GetTrackMetadataIndexFromLevelId");
    private static readonly MethodInfo _handleCycleTrackSortingOrder = Utils.Method<CustomTracksSelectionSceneController>("HandleCycleTrackSortingOrder");

    private static readonly FieldInfo _actionBindingView_associatedAction = Utils.Field<ActionBindingView>("_associatedAction");
    private static readonly FieldInfo _customTrackSelectionOptionGroup_infiniteScrollBar = Utils.Field<CustomTrackSelectionOptionGroup>("_infiniteScrollBar");


    private static string PlaylistIdFromName(string name)
    {
        return $"{_playlistIdPrefix}{name}";
    }

    private static bool InsidePlaylist()
    {
        return _selectedPlaylist.HasPlaylist();
    }

    private static void SwitchSortingOrder(CustomTracksSelectionSceneController instance, ref TrackSortingOrder sortingOrder)
    {
        if (InsidePlaylist() || _playlistMode == PlaylistMode.NoPlaylists)
        {
            sortingOrder = _sortingOrders.TracksSortingOrder - 1;
        }
        else
        {
            sortingOrder = _sortingOrders.PlaylistsSortingOrder - 1;
        }
        _handleCycleTrackSortingOrder.Invoke(instance, []);
        // HandleTrackMetadataReSort is called by HandleCycleTrackSortingOrder
        _fillInTrackDetails.Invoke(instance, []);
    }

    private static void DisplayPlaylistModeOrName(TMP_Text sortingOrderText)
    {
        const string separator = "    ―    ";
        if (InsidePlaylist())
        {
            sortingOrderText.text += separator + _selectedPlaylist.Playlist;
        }
        else
        {
            sortingOrderText.text += separator + Localizer.GetText($"ROTNDP_PlaylistMode{_playlistMode.ToString()}Label");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("Start")]
    private static bool Start_Prefix()
    {
        _sortingOrders = new SortingOrders
        {
            PlaylistsSortingOrder = (TrackSortingOrder)Plugin.Settings.PlaylistsSortingOrder,
            TracksSortingOrder = (TrackSortingOrder)Plugin.Settings.TracksSortingOrder
        };
        _playlistMode = (PlaylistMode)Plugin.Settings.PlaylistModeSelected;
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch("Start")]
    private static System.Collections.IEnumerator Start_Postfix(
        System.Collections.IEnumerator result,
        GameObject ____collectionsMenuKeybindingLabel,
        TMP_Text ____sortingOrderText
    )
    {
        while (result.MoveNext())
            yield return result.Current;

        // This try to mimic the layout from TrackSelectionSceneController but it is not perfect
        var modifiersLabel = ____collectionsMenuKeybindingLabel;
        var footer = modifiersLabel.GetComponent<Transform>().parent;
        var footerLayout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
        footerLayout.spacing = 10;
        footerLayout.childScaleWidth = true;

        var modifierLayout = modifiersLabel.AddComponent<HorizontalLayoutGroup>();
        modifierLayout.childAlignment = TextAnchor.UpperRight;
        modifierLayout.spacing = 10;
        modifierLayout.childScaleWidth = true;
        modifierLayout.reverseArrangement = true;

        var playlistModeLabel = UnityEngine.Object.Instantiate(modifiersLabel.gameObject, footer);
        playlistModeLabel.name = "CyclePlaylistMode";

        var actionBIndingView = playlistModeLabel.GetComponent<ActionBindingView>();
        MapActionPair mapAction;
        mapAction.MapName = "UI";
        mapAction.ActionName = "CycleMode";
        _actionBindingView_associatedAction.SetValue(actionBIndingView, mapAction);

        var text = playlistModeLabel.transform.Find("Text");
        text.gameObject.GetComponent<UITextMeshProLocalizer>().id = "ROTNDP_CyclePlaylistMode";

        playlistModeLabel.transform.SetSiblingIndex(modifiersLabel.transform.GetSiblingIndex() + 1);
        _playlistModeLabel = playlistModeLabel;
        _playlistModeLabel.SetActive(!InsidePlaylist());

        DisplayPlaylistModeOrName(____sortingOrderText);
    }

    [HarmonyPrefix]
    [HarmonyPatch("OnDestroy")]
    private static bool OnDestroy_Prefix()
    {
        Plugin.Settings.PlaylistsSortingOrder = (int)_sortingOrders.PlaylistsSortingOrder;
        Plugin.Settings.TracksSortingOrder = (int)_sortingOrders.TracksSortingOrder;
        Plugin.Settings.PlaylistModeSelected = (int)_playlistMode;
        Plugin.Settings.Serialize(); // Is it the good place to do this ?
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("Update")]
    private static bool Update_Prefix(
        CustomTracksSelectionSceneController __instance,
        RiftInputActions ____input,
        bool ____inputDisabledBacking,
        ref int ____selectedTrackIndex,
        ref TrackSortingOrder ____sortingOrder
    )
    {
        if (____inputDisabledBacking)
        {
            return true;
        }
        if (!InsidePlaylist() && ____input.UI.CycleMode.WasPerformedThisFrame())
        {
            _playlistMode = _playlistMode.Next();
            SwitchSortingOrder(__instance, ref ____sortingOrder);
            return false;
        }
        if (InsidePlaylist() && ____input.UI.Cancel.WasPerformedThisFrame())
        {
            var playlistId = PlaylistIdFromName(_selectedPlaylist.Playlist);
            _selectedPlaylist.Unslect();
            _playlistModeLabel.SetActive(true);
            _fillInTracksToDisplayForCurrentDifficulty.Invoke(__instance, []);
            ____selectedTrackIndex = (int)_getTrackMetadataIndexFromLevelId.Invoke(__instance, [playlistId]);
            SwitchSortingOrder(__instance, ref ____sortingOrder);
            return false;
        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch("HandleCycleTrackSortingOrder")]
    private static void HandleCycleTrackSortingOrder_Postfix(
        TrackSortingOrder ____sortingOrder,
        TMP_Text ____sortingOrderText
    )
    {
        if (InsidePlaylist() || _playlistMode == PlaylistMode.NoPlaylists)
        {
            _sortingOrders.TracksSortingOrder = ____sortingOrder;
        }
        else
        {
            _sortingOrders.PlaylistsSortingOrder = ____sortingOrder;
        }
        DisplayPlaylistModeOrName(____sortingOrderText);
    }

    [HarmonyPostfix]
    [HarmonyPatch("GetTrackMetadataIndexFromLevelId")]
    private static void GetTrackMetadataIndexFromLevelId_Postfix(
        ITrackMetadata[] ____displayedTrackMetaDatas,
        CustomTrackSelectionOptionGroup ____trackSelectionOptionGroup,
        int __result
    )
    {
        var scrollbar = _customTrackSelectionOptionGroup_infiniteScrollBar.GetValue(____trackSelectionOptionGroup);
        ((DefinableScrollBar)scrollbar).Initialize(
            ____displayedTrackMetaDatas.Count(),
            __result < 0 ? 0 : __result
        );
    }

    // TODO: build the playlists only when necessary
    [HarmonyPrefix]
    [HarmonyPatch("HandleTrackMetadataReSort")]
    private static bool HandleTrackMetadataReSort_Prefix(List<ITrackMetadata> ____customTrackMetadatas)
    {
        switch (_playlistMode)
        {
            case PlaylistMode.NoPlaylists:
                _playlistCollection = new PlaylistCollection(____customTrackMetadatas, (track) => _allPlaylistId, 0);
                break;
            case PlaylistMode.ArtistPlaylists:
                _playlistCollection = new PlaylistCollection(
                    ____customTrackMetadatas,
                    (track) => track.Category == TrackCategory.UgcLocal ? null : track.ArtistName,
                    minSizeToShow: Plugin.UserConfig.MinTracksToShowArtistPlaylist.Value,
                    parseNames: true
                );
                AddDefaultPlaylists(____customTrackMetadatas);
                break;
            case PlaylistMode.StageCreatorPlaylists:
                _playlistCollection = new PlaylistCollection(
                    ____customTrackMetadatas,
                    (track) => track.Category == TrackCategory.UgcLocal ? null : track.StageCreatorName,
                    minSizeToShow: Plugin.UserConfig.MinTracksToShowCreatorPlaylist.Value,
                    parseNames: true
                );
                AddDefaultPlaylists(____customTrackMetadatas);
                break;
        }
        return true;
    }

    private static void AddDefaultPlaylists(List<ITrackMetadata> customTrackMetadatas)
    {
        AddLocalAndNonLocalPlaylists(customTrackMetadatas);
        AddNewTracksPlaylist(customTrackMetadatas);
        if (Plugin.UserConfig.ShowUncompletedTracksPlaylist.Value)
        {
            AddUncompletedTracksPlaylist(customTrackMetadatas);
        }
    }

    private static void AddLocalAndNonLocalPlaylists(List<ITrackMetadata> customTrackMetadatas)
    {
        var playlists = new PlaylistCollection(
            customTrackMetadatas,
            (track) =>
                {
                    if (track.Category == TrackCategory.UgcLocal)
                    {
                        return Localizer.GetText("ROTNDP_EditorTracks");
                    }
                    else
                    {
                        return Localizer.GetText("ROTNDP_AllTracks");
                    }
                }
        );
        playlists.SetSortOrder(Localizer.GetText("ROTNDP_EditorTracks"), -0.9);
        playlists.SetSortOrder(Localizer.GetText("ROTNDP_AllTracks"), -0.8);
        _playlistCollection.Extend(playlists);
    }

    private static void AddNewTracksPlaylist(List<ITrackMetadata> customTrackMetadatas)
    {
        var playlists = new PlaylistCollection(
            customTrackMetadatas,
            (track) =>
                {
                    if (!PlayerDataUtil.HasLevelBeenAttempted(track.LevelId))
                    {
                        return Localizer.GetText("ROTNDP_NewTracks");
                    }
                    else
                    {
                        return null;
                    }
                }
        );
        playlists.SetSortOrder(Localizer.GetText("ROTNDP_NewTracks"), -0.7);
        _playlistCollection.Extend(playlists);
    }

    private static void AddUncompletedTracksPlaylist(List<ITrackMetadata> customTrackMetadatas)
    {
        var playlists = new PlaylistCollection(
           customTrackMetadatas,
           (track) =>
               {
                   if (!PlayerDataUtilExt.HasLevelBeenCompleted(track.LevelId))
                   {
                       return Localizer.GetText("ROTNDP_UncompletedTracks");
                   }
                   else
                   {
                       return null;
                   }
               }
       );
        playlists.SetSortOrder(Localizer.GetText("ROTNDP_UncompletedTracks"), -0.6);
        _playlistCollection.Extend(playlists);
    }

    [HarmonyPrefix]
    [HarmonyPatch("HandleTrackSubmitted")]
    private static bool HandleTrackSubmitted_Prefix(
        CustomTracksSelectionSceneController __instance,
        ref TrackSortingOrder ____sortingOrder,
        string levelId
    )
    {
        if (levelId.StartsWith(_playlistIdPrefix))
        {
            _selectedPlaylist.SetPlaylistFromLevelId(levelId);
            _playlistModeLabel.SetActive(false);
            SwitchSortingOrder(__instance, ref ____sortingOrder);
            return false;
        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch("FillInTrackDetails")]
    private static void FillInTrackDetails_Postfix(
        ITrackMetadata[] ____displayedTrackMetaDatas,
        int ____selectedTrackIndex,
        GameObject ____infoBox
    )
    {
        var displayedTrackMetaData = ____displayedTrackMetaDatas[____selectedTrackIndex];
        if (displayedTrackMetaData.LevelId.StartsWith(_playlistIdPrefix))
        {
            ____infoBox.SetActive(false);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("FillInTracksToDisplayForCurrentDifficulty")]
    private static bool FillInTracksToDisplayForCurrentDifficulty_Prefix(
        ref ITrackMetadata[] ____displayedTrackMetaDatas,
        Difficulty ____selectedDifficulty,
        TrackSortingOrder ____sortingOrder
    )
    {
        List<ITrackMetadata> trackMetadataList = [];
        if (InsidePlaylist())
        {
            DisplayTracks(ref trackMetadataList, _playlistCollection.Get(_selectedPlaylist.Playlist), ____selectedDifficulty, ____sortingOrder);
        }
        else
        {
            AddSteamWorkshopAndEditorPlaceholders(ref trackMetadataList);
            switch (_playlistMode)
            {
                case PlaylistMode.NoPlaylists:
                    DisplayTracks(ref trackMetadataList, _playlistCollection.Get(_allPlaylistId), ____selectedDifficulty, ____sortingOrder);
                    break;
                case PlaylistMode.ArtistPlaylists:
                    DisplayPlaylists(ref trackMetadataList, _playlistCollection);
                    break;
                case PlaylistMode.StageCreatorPlaylists:
                    DisplayPlaylists(ref trackMetadataList, _playlistCollection);
                    break;
            }
        }
        ____displayedTrackMetaDatas = trackMetadataList.ToArray();
        return false;
    }

    private static void DisplayTracks(
        ref List<ITrackMetadata> trackMetadataList,
        Playlist playlist,
        Difficulty difficulty,
        TrackSortingOrder sortingOrder
    )
    {
        var sortOrderForUnvailableTrack =
            TrackSortingOrder.TitleAscending == sortingOrder || TrackSortingOrder.TitleDescending == sortingOrder
            || (!InsidePlaylist() && (TrackSortingOrder.ArtistAscending == sortingOrder || TrackSortingOrder.ArtistDescending == sortingOrder))
            ? 0.0 : -0.5;
#pragma warning disable Harmony003 // Harmony non-ref patch parameters modified
        foreach (ITrackMetadata track in playlist.Tracks)
#pragma warning restore Harmony003 // Harmony non-ref patch parameters modified
        {
            if (track.Difficulties.Contains(difficulty))
            {
                if (Plugin.UserConfig.RoundBpm.Value)
                {
                    var trackMetadata = new MutableTrackMetadata(track);
                    trackMetadata.DifficultyInfo[difficulty].BeatsPerMinute =
                        (float)Math.Round(trackMetadata.DifficultyInfo[difficulty].BeatsPerMinute ?? 0f, 0);
                    trackMetadataList.Add(trackMetadata);
                }
                else
                {
                    trackMetadataList.Add(track);
                }
            }
            else if (Plugin.UserConfig.TrackInAllDifficulties.Value)
            {
                trackMetadataList.Add(new MutableTrackMetadata(track)
                {
                    AlbumArtUrl = AlbumArtCache.GrayedOutAlbumArt(track),
                    BeatsPerMinute = null,
                    TrackLength = null,
                    SortOrder = sortOrderForUnvailableTrack
                });
            }
        }
    }

    private static void DisplayPlaylists(ref List<ITrackMetadata> trackMetadataList, PlaylistCollection playlists)
    {
#pragma warning disable Harmony003 // Harmony non-ref patch parameters modified
        foreach (var playlist in playlists.Playlists().Select(playlist => playlist.Playlist))
#pragma warning restore Harmony003 // Harmony non-ref patch parameters modified
        {
            var playlistCount = playlist.Tracks.Count;
            if (playlistCount >= playlist.MinSizeToShow)
            {
                trackMetadataList.Add(new MutableTrackMetadata(playlist.Metadata)
                {
                    AlbumArtUrl = playlistCount > 0 ? playlist.Tracks[0].AlbumArtUrl : null,
                });
            }
        }
    }

    private static void AddSteamWorkshopAndEditorPlaceholders(ref List<ITrackMetadata> trackMetadataList)
    {
        trackMetadataList.Add(
            new PlaceholderUgcTrackMetadata()
            {
                TrackName = Localizer.GetText("CustomMusicBrowseSteamWorkshop"),
                ArtistName = Localizer.GetText("CustomMusicBrowseSteamWorkshopDesc"),
                LevelId = "Placeholder_Browse_Workshop",
                SortOrder = -2.0
            });
        if (SteamWorkshopUgcTrackProvider.IsEditorAvailable)
            trackMetadataList.Add(new PlaceholderUgcTrackMetadata()
            {
                TrackName = Localizer.GetText("CustomMusicOpenLevelEditor"),
                ArtistName = Localizer.GetText("CustomMusicOpenLevelEditorDesc"),
                LevelId = "Placeholder_Open_Editor",
                SortOrder = -1.0
            });
    }

    private readonly struct PlaylistCollection
    {
        private readonly Dictionary<string, Playlist> _playlists = [];

        public PlaylistCollection(
            List<ITrackMetadata> trackMetadatas,
            Func<ITrackMetadata, string> getKey,
            int minSizeToShow = 1,
            bool parseNames = false
        )
        {
            BuildPlaylists(trackMetadatas, getKey, minSizeToShow, parseNames);
        }

        public readonly Playlist Get(string playlistName)
        {
            return _playlists[playlistName];
        }

        public readonly void Extend(PlaylistCollection other)
        {
            foreach (var entry in other._playlists)
            {
                _playlists[entry.Key] = entry.Value;
            }
        }

        public readonly void SetSortOrder(string playlistName, double sortOrder)
        {
            var playlist = _playlists[playlistName];
            var newMetadata = new MutableTrackMetadata(playlist.Metadata)
            {
                SortOrder = sortOrder
            };
            _playlists[playlistName] = new Playlist(newMetadata, playlist.Tracks, playlist.MinSizeToShow);
        }

        public readonly IEnumerable<(string Name, Playlist Playlist)> Playlists()
        {
            foreach (KeyValuePair<string, Playlist> entry in _playlists)
            {
                var playlistName = entry.Key;
                var playlist = entry.Value;
                yield return (playlistName, playlist);
            }
        }

        private static string ComputePlaylistLentgh(List<ITrackMetadata> playlist)
        {
            var playlistLength =
                playlist
                    .Select(track => Utils.ParseDuration(track.TrackLength ?? ""))
                    .Aggregate(TimeSpan.Zero, (total, next) => total + next);
            return Utils.FormatDuration(playlistLength);
        }

        private static float ComputePlaylistMeanBpm(List<ITrackMetadata> playlist)
        {
            return (float)Math.Round(playlist.Select(track => track.BeatsPerMinute ?? 0).Sum() / playlist.Count, 0);
        }

        private static ITrackMetadata PlaylistMetadata(List<ITrackMetadata> playlist, string playlistName, string length, float bpm)
        {
            return new MutableTrackMetadata()
            {
                TrackName = playlistName,
                ArtistName = LocalizerExt.GetFormattedTracks(playlist.Count),
                LevelId = PlaylistIdFromName(playlistName),
                TrackLength = length,
                BeatsPerMinute = bpm
            };
        }

        private void BuildPlaylists(
            List<ITrackMetadata> trackMetadatas,
            Func<ITrackMetadata, string> getKey,
            int minSizeToShow,
            bool parseNames
        )
        {
            var playlists = new Dictionary<string, List<ITrackMetadata>>();
            if (parseNames)
            {
                var nameParser = new NameParser();
                foreach (ITrackMetadata track in trackMetadatas)
                {
                    var key = getKey(track);
                    if (key is null) continue;
                    nameParser.ParseName(key);
                }
                nameParser.MatchPendingAbbreviations();

                foreach (ITrackMetadata track in trackMetadatas)
                {
                    var key = getKey(track);
                    if (key is null) continue;
                    foreach (var match in nameParser.GetMatches(key))
                    {
                        playlists.GetOrCreate(match).Add(track);
                    }
                }
            }
            else
            {
                foreach (ITrackMetadata track in trackMetadatas)
                {
                    var key = getKey(track);
                    if (key is null) continue;
                    playlists.GetOrCreate(key).Add(track);

                }
            }

            foreach (var playlistName in playlists.Select(entry => entry.Key))
            {
                var playlist = playlists[playlistName];
                var length = ComputePlaylistLentgh(playlist);
                var meanBpm = ComputePlaylistMeanBpm(playlist);
                var metadata = PlaylistMetadata(playlist, playlistName, length, meanBpm);
                _playlists[playlistName] = new Playlist(metadata, playlist, minSizeToShow);
            }
        }
    }

    private readonly struct Playlist(ITrackMetadata metadata, List<ITrackMetadata> tracks, int minSizeToShow)
    {
        public ITrackMetadata Metadata { get; } = metadata;
        public List<ITrackMetadata> Tracks { get; } = tracks;
        public int MinSizeToShow { get; } = minSizeToShow;
    }

    private struct SelectedPlaylist
    {
        public string Playlist { get; private set; }

        public SelectedPlaylist()
        {
            Playlist = null;
        }

        public void SetPlaylistFromLevelId(string levelId)
        {
            Playlist = levelId.Substring(_playlistIdPrefix.Length);
        }

        public void Unslect()
        {
            Playlist = null;
        }

        public readonly bool HasPlaylist()
        {
            return Playlist is not null;
        }
    }

    private struct SortingOrders
    {
        public TrackSortingOrder PlaylistsSortingOrder;
        public TrackSortingOrder TracksSortingOrder;
    }
}

internal enum PlaylistMode
{
    NoPlaylists,
    ArtistPlaylists,
    StageCreatorPlaylists,
}

internal static class PlaylistModeMethods
{
    public static PlaylistMode Next(this PlaylistMode mode)
    {
        return (PlaylistMode)((int)(mode + 1) % Enum.GetNames(typeof(PlaylistMode)).Length);
    }
}
