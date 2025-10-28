using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Shared;
using TicToc.Localization;

namespace RiftOfTheNecroDancerPlaylists;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("RiftOfTheNecroDancer.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    internal static readonly Paths Path = new();
    internal static readonly UserConfig UserConfig = new();
    internal static readonly Settings Settings = Settings.Deserialize();

    private static string[] _gameTestedVersions = ["1.8.0"];

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogMessage($"Current build info: {BuildInfoHelper.Instance.BuildId} {BuildInfoHelper.Instance.CommitHash}");

        UserConfig.Initialize(Config);

        var gameVersion = BuildInfoHelper.Instance.BuildId.Split('-')[0];
        var modIsCompatible = _gameTestedVersions.Contains(gameVersion);
        if (!UserConfig.TurnOnModOnForNonTestedVersion.Value && !modIsCompatible)
        {
            Logger.LogWarning(
                $"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} was not loaded due to "
                + $"\"{UserConfig.TurnOnModOnForNonTestedVersion.Definition}\" being off "
                + $"and game version ({gameVersion}) not being tested for it."
            );
            return;
        }
        if (!modIsCompatible)
        {
            Logger.LogWarning(
                $"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} has not been tested for current game version ({gameVersion}). "
                + $"If you encounter any issue turn off the mod using the \"{UserConfig.TurnOnModOnForNonTestedVersion.Definition}\" setting."
            );
        }

        AlbumArtCache.Initialize();
        Localizer.AddKeysFromLocalFile(System.IO.Path.Combine(Path.Assets, "localization.csv"));

        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();

        Logger.LogMessage($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} is loaded !");
    }

    internal struct Paths
    {
        public string Plugin;
        public string Cache;
        public string Assets;
        public string SettingsJson;

        public Paths()
        {
            Plugin = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Cache = System.IO.Path.Combine(Plugin, "cache");
            Assets = System.IO.Path.Combine(Plugin, "assets");
            SettingsJson = System.IO.Path.Combine(Plugin, "settings.json");

            Directory.CreateDirectory(Cache);
        }
    }
}
