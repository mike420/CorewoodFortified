using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace CorewoodFortified;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency("com.jotunn.jotunn", BepInDependency.DependencyFlags.HardDependency)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]
public class CorewoodFortifiedMod : BaseUnityPlugin
{
    public const string PluginName = "CorewoodFortified";
    public const string PluginVersion = "1.0.5";
    public const string PluginAuthor = "MagicMike";
    internal const string PluginGUID = "MagicMike.CorewoodFortified";
    internal const string PluginCopy = "Copyright © MagicMike 2026";
    public const string PluginLink = "https://discord.gg/mnr36G6J4x";

    private static char pds = Path.DirectorySeparatorChar;
    public static readonly string _basePath = BepInEx.Paths.ConfigPath + pds + "CorewoodFortified";
    public static readonly string TranslationFilePath = _basePath + pds + "Translations";

    public static CorewoodFortifiedMod plugininstance;

    public static AssetBundle cwfortified;
    public static string newBundle = "corewood_fortified";

    private Harmony harmony;

    private volatile bool translationsChanged;
    private float translationReloadAt = -1f;

    public void Awake()
    {
        plugininstance = this;

        MainConfig.InitConfigs(base.Config);
        MagicMike.ModSettings.ModSettings.Init(this);


        if (MainConfig.modEnabled.Value)
        {
            CheckForFolders();
            // A broken translation file must not stop the pieces loading; the built-in English names cover it
            try
            {
                LoadLocalisationData(TranslationFilePath);
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogWarning("[CorewoodFortified] Could not load the translation files: " + ex.Message);
            }
            int missing = LocalisedText.AddMissingLocalisations();
            if (missing > 0)
            {
                Jotunn.Logger.LogWarning("[CorewoodFortified] " + missing + " English translation(s) missing from " + TranslationFilePath + ", using built-in names for them.");
            }
            FileWatcher.WatchTranslationChanges(TranslationFilePath, delegate
            {
                translationsChanged = true;
            });

            if (!LoadAssetBundles())
            {
                Jotunn.Logger.LogError("CorewoodFortified pieces will not be loaded because the asset bundle is missing.");
                return;
            }
            AddFXAssets();

            PrefabManager.OnVanillaPrefabsAvailable += LoadCoreWoodFortified;

            harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginGUID);

            Game.isModded = true;
        }
    }

    public void Update()
    {
        ReloadTranslationsIfChanged();
    }

    private void ReloadTranslationsIfChanged()
    {
        if (translationsChanged)
        {
            translationsChanged = false;
            translationReloadAt = Time.time + 0.5f;
        }
        if (translationReloadAt >= 0f && Time.time >= translationReloadAt)
        {
            translationReloadAt = -1f;
            try
            {
                LocalisedText.Reload(() => LoadLocalisationData(TranslationFilePath));
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogWarning("[CorewoodFortified] Could not apply the reloaded translations: " + ex.Message);
            }
        }
    }

    public static void CheckForFolders()
    {
        if (!Directory.Exists(_basePath))
        {
            Jotunn.Logger.LogInfo("Creating CorewoodFortified directory");
            Directory.CreateDirectory(_basePath);
        }
        if (!Directory.Exists(TranslationFilePath))
        {
            Jotunn.Logger.LogInfo("Creating Translations folder");
            Directory.CreateDirectory(TranslationFilePath);
        }
    }

    public void LoadLocalisationData(string directory)
    {
        string[] fileSystemEntries = Directory.GetFileSystemEntries(directory, "*.json", SearchOption.AllDirectories);
        string root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        foreach (string path in fileSystemEntries)
        {
            // The language comes from the folder name, so a file sitting directly in Translations has none
            string folder = Path.GetDirectoryName(Path.GetFullPath(path));
            if (string.Equals(folder, root, StringComparison.OrdinalIgnoreCase))
            {
                Jotunn.Logger.LogWarning("[CorewoodFortified] Skipping " + path + ": translation files must be in a language folder, e.g. Translations" + pds + "English.");
                continue;
            }
            string language = new DirectoryInfo(folder).Name;
            LocalisedText.Localization.AddJsonFile(language, File.ReadAllText(path));
        }
    }


    public static bool LoadAssetBundles()
    {
        cwfortified = AssetUtils.LoadAssetBundleFromResources(newBundle, Assembly.GetExecutingAssembly());
        if (cwfortified == null)
        {
            Jotunn.Logger.LogError("Failed to load asset bundle with name: " + newBundle);
            return false;
        }
        return true;
    }

    internal static readonly string[] SfxPrefabs =
    {
        "sfx_open_fortified_cwf",
        "sfx_close_fortified_cwf",
        "sfx_oc_drawbridge_cwf",
        "sfx_tower_chains_cwf",
        "sfx_elevator_cwf",
        "sfx_turret_scan_cwf",
        "sfx_flames_cwf",
        "sfx_saw_cwf",
        "sfx_spikes_cwf"
    };

    // Each sound is loaded on its own so one missing from the bundle does not stop the rest
    public static void AddFXAssets()
    {
        foreach (string prefabName in SfxPrefabs)
        {
            try
            {
                GameObject prefab = cwfortified.LoadAsset<GameObject>(prefabName);
                if (!prefab)
                {
                    Jotunn.Logger.LogWarning("[CorewoodFortified] Sound prefab '" + prefabName + "' not found in the asset bundle, skipping.");
                    continue;
                }
                PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, fixReference: false));
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogError("Exception caught while loading custom sound '" + prefabName + "': " + ex.Message);
            }
        }
    }

    private void LoadCoreWoodFortified()
    {
        try
        {
            AddFortified.LoadPieces();
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Exception caught while loading CoreWoodFortified Pieces: " + ex);
        }
        finally
        {
            PrefabManager.OnVanillaPrefabsAvailable -= LoadCoreWoodFortified;
        }
    }

    internal static void TryAddPiece(Action addPiece)
    {
        try
        {
            addPiece();
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Exception caught while adding " + addPiece.Method.Name + ": " + ex);
        }
    }
}
