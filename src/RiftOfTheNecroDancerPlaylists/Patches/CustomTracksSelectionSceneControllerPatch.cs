using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
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
    private static PlaylistsJson _userPlaylists;
    private static PlaylistMode _playlistMode;
    private static SelectedPlaylist _selectedPlaylist = new();
    private static SortingOrders _sortingOrders;

    private static GameObject _playlistModeLabel;

    private static readonly MethodInfo _fillInTrackDetails = Utils.Method<CustomTracksSelectionSceneController>("FillInTrackDetails");
    private static readonly MethodInfo _fillInTracksToDisplayForCurrentDifficulty = Utils.Method<CustomTracksSelectionSceneController>("FillInTracksToDisplayForCurrentDifficulty");
    private static readonly MethodInfo _getTrackMetadataIndexFromLevelId = Utils.Method<CustomTracksSelectionSceneController>("GetTrackMetadataIndexFromLevelId");
    private static readonly MethodInfo _handleCycleTrackSortingOrder = Utils.Method<CustomTracksSelectionSceneController>("HandleCycleTrackSortingOrder");
    private static readonly MethodInfo _handleTrackMetadataReSort = Utils.Method<CustomTracksSelectionSceneController>("HandleTrackMetadataReSort");
    private static readonly MethodInfo _handleFilterTrackMetadata = Utils.Method<CustomTracksSelectionSceneController>("FilterTrackMetadata");

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
        if (_playlistMode == PlaylistMode.UserPlaylists && InsidePlaylist())
        {
            sortingOrder = _sortingOrders.TracksCustomSortingOrder - 1;
        }
        else if (_playlistMode == PlaylistMode.NoPlaylists || InsidePlaylist())
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
            sortingOrderText.text += separator + _playlistCollection.Get(_selectedPlaylist.Playlist).Metadata.TrackName;
        }
        else
        {
            sortingOrderText.text += separator + Localizer.GetText($"ROTNDP_PlaylistMode{_playlistMode}Label");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("Start")]
    private static bool Start_Prefix()
    {
        _sortingOrders = new SortingOrders
        {
            PlaylistsSortingOrder = (TrackSortingOrder)Plugin.Settings.PlaylistsSortingOrder,
            TracksSortingOrder = (TrackSortingOrder)Plugin.Settings.TracksSortingOrder,
            TracksCustomSortingOrder = (TrackSortingOrder)Plugin.Settings.TracksCustomSortingOrder
        };
        _playlistMode = (PlaylistMode)Plugin.Settings.PlaylistModeSelected;
        _userPlaylists = PlaylistsJson.Deserialize();
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch("Start")]
    private static System.Collections.IEnumerator Start_Postfix(
        System.Collections.IEnumerator result,
        NavbarExpander ____navbarExpander,
        TMP_Text ____sortingOrderText
    )
    {
        while (result.MoveNext())
            yield return result.Current;

        var bottomRow = ____navbarExpander.gameObject.transform.Find("BottomRow");
        var template = bottomRow.Find("Favorite").gameObject;

        var modifierLayout = bottomRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        modifierLayout.childAlignment = TextAnchor.UpperRight;
        modifierLayout.spacing = 20;
        modifierLayout.childForceExpandWidth = false;
        modifierLayout.reverseArrangement = true;

        var rightPadding = new GameObject("RightPadding");
        rightPadding.transform.SetParent(bottomRow, false);
        rightPadding.transform.SetSiblingIndex(0);
        var layout = rightPadding.AddComponent<LayoutElement>();
        layout.minWidth = 50;

        var playlistModeLabel = UnityEngine.Object.Instantiate(template, bottomRow);
        playlistModeLabel.name = "CyclePlaylistMode";

        var actionBIndingView = playlistModeLabel
            .transform
            .Find("ExpandableKeyContainer")
            .GetComponent<ActionBindingView>();
        MapActionPair mapAction;
        mapAction.MapName = "UI";
        mapAction.ActionName = "CycleMode";
        _actionBindingView_associatedAction.SetValue(actionBIndingView, mapAction);

        var text = playlistModeLabel.transform.Find("Text");
        text.gameObject.GetComponent<UITextMeshProLocalizer>().id = "ROTNDP_CyclePlaylistMode";

        playlistModeLabel.transform.SetSiblingIndex(3);
        _playlistModeLabel = playlistModeLabel;
        _playlistModeLabel.SetActive(!InsidePlaylist());

        if (InsidePlaylist() && _playlistMode == PlaylistMode.UserPlaylists)
        {
            ____sortingOrderText.text = LocalizerExt.GetUserPlaylistOrderName(_sortingOrders.TracksCustomSortingOrder);
        }
        else if (_playlistMode != PlaylistMode.NoPlaylists)
        {
            ____sortingOrderText.text = LocalizerExt.GetPlaylistOrderName(_sortingOrders.PlaylistsSortingOrder);
        }
        DisplayPlaylistModeOrName(____sortingOrderText);
    }

    [HarmonyPrefix]
    [HarmonyPatch("OnDestroy")]
    private static bool OnDestroy_Prefix()
    {
        Plugin.Settings.PlaylistsSortingOrder = (int)_sortingOrders.PlaylistsSortingOrder;
        Plugin.Settings.TracksSortingOrder = (int)_sortingOrders.TracksSortingOrder;
        Plugin.Settings.TracksCustomSortingOrder = (int)_sortingOrders.TracksCustomSortingOrder;
        Plugin.Settings.PlaylistModeSelected = (int)_playlistMode;
        Plugin.Settings.Serialize(); // Is it the good place to do this ?
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("Update")]
    private static bool Update_Prefix(
        CustomTracksSelectionSceneController __instance,
        RiftInputActions ____input,
        ITrackMetadata[] ____displayedTrackMetaDatas,
        bool ____inputDisabledBacking,
        ref int ____selectedTrackIndex,
        ref TrackSortingOrder ____sortingOrder,
        ref TrackFilterOption ____filterOption,
        TMP_Text ____filterOptionText
    )
    {
        if (____inputDisabledBacking)
        {
            return true;
        }
        if (____input.UI.CycleTrackFilter.WasPerformedThisFrame())
        {
            ____filterOption = (TrackFilterOption)(((int)____filterOption + 1) % Enum.GetNames(typeof(TrackFilterOption)).Length);
            if (____filterOption == TrackFilterOption.DLC)
            {
                ____filterOptionText.text = Localizer.GetText("ROTNDP_TrackSelectionFilterOptionUncompletedLabel");
            }
            else
            {
                ____filterOptionText.text = Localizer.GetText($"TrackSelectionFilterOption{____filterOption}Label");
            }
            PlayerSaveController.Instance.SetSelectedCustomMusicFilterOption(____filterOption);
            _handleFilterTrackMetadata.Invoke(__instance, [true]);
            return false;
        }
        if (!InsidePlaylist()
            && (_playlistMode == PlaylistMode.ArtistPlaylists || _playlistMode == PlaylistMode.UserPlaylists)
            && ____input.UI.OpenWorkshopPage.WasPerformedThisFrame()
            && 0 <= ____selectedTrackIndex && ____selectedTrackIndex <= ____displayedTrackMetaDatas.Count()
        )
        {
            var selected = ____displayedTrackMetaDatas[____selectedTrackIndex];
            if (selected.LevelId.StartsWith(_playlistIdPrefix)
                && selected.TrackName != Localizer.GetText("ROTNDP_EditorTracks")
            )
            {
                switch (_playlistMode)
                {
                    case PlaylistMode.ArtistPlaylists:
                        SteamWorkshopUgcTrackProviderExt.SearchArtist(selected.TrackName);
                        break;
                    case PlaylistMode.UserPlaylists:
                        var filedId = SteamWorkshopUgcTrackProvider.LevelIdToFileId(selected.LevelId.Substring(_playlistIdPrefix.Length));
                        SteamWorkshopUgcTrackProvider.GoToCustomTrackPage(filedId.Value);
                        break;
                }
            }
            return false;
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
            _selectedPlaylist.Unselect();
            _playlistModeLabel.SetActive(true);
            _fillInTracksToDisplayForCurrentDifficulty.Invoke(__instance, []);
            ____selectedTrackIndex = (int)_getTrackMetadataIndexFromLevelId.Invoke(__instance, [playlistId]);
            SwitchSortingOrder(__instance, ref ____sortingOrder);
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("HandleCycleTrackSortingOrder")]
    private static bool HandleCycleTrackSortingOrder_Prefix(
        CustomTracksSelectionSceneController __instance,
        ref TrackSortingOrder ____sortingOrder,
        TMP_Text ____sortingOrderText
    )
    {
        static TrackSortingOrder CycleSortingOrder(Func<TrackSortingOrder, bool> available, TrackSortingOrder order)
        {
            int num = (int)order;
            do
            {
                num = (num + 1) % Enum.GetNames(typeof(TrackSortingOrder)).Length;
            } while (!available((TrackSortingOrder)num));
            return (TrackSortingOrder)num;
        }

        if (InsidePlaylist() && _playlistMode == PlaylistMode.UserPlaylists)
        {
            ____sortingOrder = CycleSortingOrder(TrackSortingOrderExt.IsSortingOrderAvailableForUserPlaylist, ____sortingOrder);
            ____sortingOrderText.text = LocalizerExt.GetUserPlaylistOrderName(____sortingOrder);
        }
        else if (InsidePlaylist() || _playlistMode == PlaylistMode.NoPlaylists)
        {
            return true;
        }
        else
        {
            ____sortingOrder = CycleSortingOrder(TrackSortingOrderExt.IsSortingOrderAvailableForPlaylists, ____sortingOrder);
            ____sortingOrderText.text = LocalizerExt.GetPlaylistOrderName(____sortingOrder);
        }
        _handleTrackMetadataReSort.Invoke(__instance, []);
        PlayerSaveController.Instance.SetSelectedTrackSortingOrder(____sortingOrder);
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch("HandleCycleTrackSortingOrder")]
    private static void HandleCycleTrackSortingOrder_Postfix(TrackSortingOrder ____sortingOrder, TMP_Text ____sortingOrderText)
    {
        if (_playlistMode == PlaylistMode.UserPlaylists && InsidePlaylist())
        {
            _sortingOrders.TracksCustomSortingOrder = ____sortingOrder;
        }
        else if (_playlistMode == PlaylistMode.NoPlaylists || InsidePlaylist())
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
                _playlistCollection = new PlaylistCollection(
                    ____customTrackMetadatas,
                    (track) => track.Category == TrackCategory.UgcLocal ? null : _allPlaylistId
                );
                break;
            case PlaylistMode.ArtistPlaylists:
                _playlistCollection = new PlaylistCollection(
                    ____customTrackMetadatas,
                    (track) => track.Category == TrackCategory.UgcLocal ? null : track.ArtistName,
                    minSizeToShow: Plugin.UserConfig.MinTracksToShowArtistPlaylist.Value,
                    parseNames: true
                );
                AddEditorPlaylist(____customTrackMetadatas);
                break;
            case PlaylistMode.StageCreatorPlaylists:
                _playlistCollection = new PlaylistCollection(
                    ____customTrackMetadatas,
                    (track) => track.Category == TrackCategory.UgcLocal ? null : track.StageCreatorName,
                    minSizeToShow: Plugin.UserConfig.MinTracksToShowCreatorPlaylist.Value,
                    parseNames: true
                );
                AddEditorPlaylist(____customTrackMetadatas);
                break;
            case PlaylistMode.UserPlaylists:
                _playlistCollection = new PlaylistCollection(____customTrackMetadatas, _userPlaylists);
                AddEditorPlaylist(____customTrackMetadatas);
                break;
        }
        if (_selectedPlaylist.HasPlaylist() && !_playlistCollection.Contains(_selectedPlaylist.Playlist))
        {
            _selectedPlaylist.Unselect();
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("SortTrackMetadata")]
    private static bool SortTrackMetadata_Prefix(
        ref ITrackMetadata[] ____displayedTrackMetaDatas,
        TrackSortingOrder ____sortingOrder
    )
    {
        if (_playlistMode == PlaylistMode.UserPlaylists
            && InsidePlaylist()
            && ____sortingOrder == TrackSortingOrder.Character
        )
        {
            var playlist = _playlistCollection.Get(_selectedPlaylist.Playlist);
            ____displayedTrackMetaDatas = playlist.Tracks.ToArray();
            return false;
        }
        return true;
    }

    private static void AddEditorPlaylist(List<ITrackMetadata> customTrackMetadatas)
    {
        var playlist = new PlaylistCollection(
            customTrackMetadatas,
            (track) =>
                {
                    if (track.Category == TrackCategory.UgcLocal)
                    {
                        return Localizer.GetText("ROTNDP_EditorTracks");
                    }
                    else
                    {
                        return null;
                    }
                }
        );
        playlist.SetSortOrder(Localizer.GetText("ROTNDP_EditorTracks"), -0.9);
        _playlistCollection.Extend(playlist);
    }

    [HarmonyPrefix]
    [HarmonyPatch("HandleTrackSubmitted")]
    private static bool HandleTrackSubmitted_Prefix(
        CustomTracksSelectionSceneController __instance,
        ref TrackSortingOrder ____sortingOrder,
        string levelId
    )
    {
        if (levelId.StartsWith(_playlistIdPrefix) && !InsidePlaylist())
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
        if (0 <= ____selectedTrackIndex && ____selectedTrackIndex < ____displayedTrackMetaDatas.Count())
        {
            var displayedTrackMetaData = ____displayedTrackMetaDatas[____selectedTrackIndex];
            if (displayedTrackMetaData.LevelId.StartsWith(_playlistIdPrefix))
            {
                ____infoBox.SetActive(false);
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("FillInTracksToDisplayForCurrentDifficulty")]
    private static bool FillInTracksToDisplayForCurrentDifficulty_Prefix(
        ref ITrackMetadata[] ____displayedTrackMetaDatas,
        Difficulty ____selectedDifficulty,
        TrackFilterOption ____filterOption
    )
    {
        List<ITrackMetadata> trackMetadataList = [];
        if (InsidePlaylist())
        {
            DisplayTracks(ref trackMetadataList, _playlistCollection.Get(_selectedPlaylist.Playlist), ____selectedDifficulty);
        }
        else
        {
            AddSteamWorkshopAndEditorPlaceholders(ref trackMetadataList);
            switch (_playlistMode)
            {
                case PlaylistMode.NoPlaylists:
                    if (_playlistCollection.Contains(_allPlaylistId))
                    {
                        DisplayTracks(ref trackMetadataList, _playlistCollection.Get(_allPlaylistId), ____selectedDifficulty);
                    }
                    break;
                case PlaylistMode.ArtistPlaylists:
                    DisplayPlaylists(ref trackMetadataList, _playlistCollection);
                    break;
                case PlaylistMode.StageCreatorPlaylists:
                    DisplayPlaylists(ref trackMetadataList, _playlistCollection);
                    break;
                case PlaylistMode.UserPlaylists:
                    DisplayPlaylists(ref trackMetadataList, _playlistCollection);
                    break;
            }
        }
        ____displayedTrackMetaDatas = ____filterOption switch
        {
            TrackFilterOption.CurrentDifficultyAvailable =>
                trackMetadataList
                    .Where(d => d.GetDifficulty(____selectedDifficulty) != null)
                    .ToArray(),
            TrackFilterOption.UnplayedSongs =>
                trackMetadataList
                    .Where(d => d.GetDifficulty(____selectedDifficulty) != null
                        && PlayerDataUtil.GetAttemptCountForDifficulty(d.LevelId, ____selectedDifficulty) <= 0)
                    .ToArray(),
            TrackFilterOption.Favorites =>
                trackMetadataList
                    .Where(d => PlayerDataUtil.IsLevelFavorite(d.LevelId))
                    .ToArray(),
            TrackFilterOption.DLC =>
                trackMetadataList
                    .Where(d => d.GetDifficulty(____selectedDifficulty) != null
                        && !PlayerDataUtilExt.HasLevelBeenCompletedOnDifficulty(d.LevelId, ____selectedDifficulty))
                    .ToArray(),
            _ => trackMetadataList.ToArray(),
        };
        return false;
    }

    private static void DisplayTracks(
        ref List<ITrackMetadata> trackMetadataList,
        Playlist playlist,
        Difficulty difficulty
    )
    {
#pragma warning disable Harmony003 // Harmony non-ref patch parameters modified
        foreach (ITrackMetadata track in playlist.Tracks)
#pragma warning restore Harmony003 // Harmony non-ref patch parameters modified
        {
            var trackMetadata = new MutableTrackMetadata(track)
            {
                BeatsPerMinute = Utils.RoundBPM(track.BeatsPerMinute),
            };
            if (track.Difficulties.Contains(difficulty))
            {
                trackMetadata.DifficultyInfo[difficulty].BeatsPerMinute = Utils.RoundBPM(trackMetadata.DifficultyInfo[difficulty].BeatsPerMinute);
            }
            trackMetadataList.Add(trackMetadata);
        }
    }

    private static void DisplayPlaylists(ref List<ITrackMetadata> trackMetadataList, PlaylistCollection playlists)
    {
#pragma warning disable Harmony003 // Harmony non-ref patch parameters modified
        foreach (var playlist in playlists.Playlists().Select(playlist => playlist.Playlist))
#pragma warning restore Harmony003 // Harmony non-ref patch parameters modified
        {
            if (playlist.Tracks.Count >= playlist.MinSizeToShow)
            {
                trackMetadataList.Add(playlist.Metadata);
            }
        }
    }

    private static void AddSteamWorkshopAndEditorPlaceholders(ref List<ITrackMetadata> trackMetadataList)
    {
        trackMetadataList.Add(
            new PlaceholderUgcTrackMetadata
            {
                TrackName = Localizer.GetText("CustomMusicBrowseSteamWorkshop"),
                ArtistName = Localizer.GetText("CustomMusicBrowseSteamWorkshopDesc"),
                LevelId = "Placeholder_Browse_Workshop",
                SortOrder = -2.0
            });
        if (SteamWorkshopUgcTrackProvider.IsEditorAvailable)
            trackMetadataList.Add(new PlaceholderUgcTrackMetadata
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

        public PlaylistCollection(
            List<ITrackMetadata> trackMetadatas,
            PlaylistsJson userPlaylists
        )
        {
            BuildUserPlaylists(trackMetadatas, userPlaylists);
        }

        public readonly bool Contains(string playlistName)
        {
            return _playlists.ContainsKey(playlistName);
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
            if (_playlists.TryGetValue(playlistName, out var playlist))
            {
                var newMetadata = new MutableTrackMetadata(playlist.Metadata)
                {
                    SortOrder = sortOrder
                };
                _playlists[playlistName] = new Playlist(newMetadata, playlist.Tracks, playlist.MinSizeToShow);
            }
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
            return (float)Math.Round(playlist.Sum(track => track.BeatsPerMinute ?? 0) / playlist.Count, 0);
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
                var tracks = playlists[playlistName];
                var length = ComputePlaylistLentgh(tracks);
                var meanBpm = ComputePlaylistMeanBpm(tracks);
                var albumArt = tracks.Count > 0 ? tracks[0].AlbumArtUrl : null;
                var metadata = new MutableTrackMetadata()
                {
                    TrackName = playlistName,
                    ArtistName = LocalizerExt.GetFormattedTracks(tracks.Count),
                    AlbumArtUrl = albumArt,
                    LevelId = PlaylistIdFromName(playlistName),
                    TrackLength = length,
                    BeatsPerMinute = meanBpm,
                    DifficultyInfo = {
                        { Difficulty.Easy, new MutableTrackDifficulty() },
                        { Difficulty.Medium, new MutableTrackDifficulty() },
                        { Difficulty.Hard, new MutableTrackDifficulty() },
                        { Difficulty.Impossible, new MutableTrackDifficulty() }
                    }
                };
                _playlists[playlistName] = new Playlist(metadata, tracks, minSizeToShow);
            }
        }

        private void BuildUserPlaylists(
            List<ITrackMetadata> trackMetadatas,
            PlaylistsJson userPlaylists
        )
        {
            var trackMap = trackMetadatas.ToDictionary(track => track.LevelId, track => track);
            foreach (PlaylistJson playlist in userPlaylists.Playlists)
            {
                var tracks = playlist.Tracks
                    .Where(trackMap.ContainsKey)
                    .Select(levelId => trackMap[levelId])
                    .ToList();
                var length = ComputePlaylistLentgh(tracks);
                var meanBpm = ComputePlaylistMeanBpm(tracks);
                var albumArt = tracks.Count > 0 ? (
                    playlist.Cover.IsNullOrWhiteSpace() ?
                        tracks[0].AlbumArtUrl
                        : trackMap[playlist.Cover].AlbumArtUrl
                    ) : null;
                var levelId = playlist.WorkshopId.IsNullOrWhiteSpace() ?
                    playlist.Name
                    : playlist.WorkshopId;
                var metadata = new MutableTrackMetadata()
                {
                    TrackName = playlist.Name,
                    ArtistName = LocalizerExt.GetFormattedTracks(tracks.Count),
                    AlbumArtUrl = albumArt,
                    LevelId = PlaylistIdFromName(levelId),
                    TrackLength = length,
                    BeatsPerMinute = meanBpm,
                    DifficultyInfo = {
                        { Difficulty.Easy, new MutableTrackDifficulty() },
                        { Difficulty.Medium, new MutableTrackDifficulty() },
                        { Difficulty.Hard, new MutableTrackDifficulty() },
                        { Difficulty.Impossible, new MutableTrackDifficulty() }
                    }
                };
                _playlists[levelId] = new Playlist(metadata, tracks, 0);
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

        public void Unselect()
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
        public TrackSortingOrder TracksCustomSortingOrder;
    }
}

internal enum PlaylistMode
{
    NoPlaylists,
    ArtistPlaylists,
    StageCreatorPlaylists,
    UserPlaylists,
}

internal static class PlaylistModeMethods
{
    public static PlaylistMode Next(this PlaylistMode mode)
    {
        return (PlaylistMode)((int)(mode + 1) % Enum.GetNames(typeof(PlaylistMode)).Length);
    }
}
