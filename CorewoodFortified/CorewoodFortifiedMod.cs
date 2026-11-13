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
[BepInDependency(EasyRelocateCompat.Guid, BepInDependency.DependencyFlags.SoftDependency)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Major)]
public class CorewoodFortifiedMod : BaseUnityPlugin
{
    public const string PluginName = "CorewoodFortified";
    public const string PluginVersion = "1.2.2";
    public const string PluginAuthor = "MagicMike";
    internal const string PluginGUID = "MagicMike.CorewoodFortified";
    internal const string PluginCopy = "Copyright © MagicMike 2026";
    public const string PluginLink = "https://discord.gg/mnr36G6J4x";

    public static CorewoodFortifiedMod plugininstance;

    public static AssetBundle cwfortified;
    public static string newBundle = "corewood_fortified";

    private Harmony harmony;

    public void Awake()
    {
        plugininstance = this;

        MainConfig.InitConfigs(base.Config);
        MagicMike.ModSettings.ModSettings.Init(this);


        if (MainConfig.modEnabled.Value)
        {
            Translations.Load(this);

            if (!LoadAssetBundles())
            {
                Jotunn.Logger.LogError("CorewoodFortified pieces will not be loaded because the asset bundle is missing.");
                return;
            }
            AddFXAssets();
            ClimbingSkill.Register();

            PrefabManager.OnVanillaPrefabsAvailable += LoadCoreWoodFortified;

            harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginGUID);
            EasyRelocateCompat.Apply(harmony);

            Game.isModded = true;
        }
    }

    public void Update()
    {
        Translations.Tick();
        TurretMover.Tick();
        LadderClimber.Tick();
    }

    public void FixedUpdate()
    {
        LadderClimber.FixedTick();
    }

    public void OnDestroy()
    {
        Translations.Unload();
    }


    public static bool LoadAssetBundles()
    {
        string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), newBundle);
        cwfortified = File.Exists(path) ? AssetBundle.LoadFromFile(path) : null;
        if (cwfortified == null)
        {
            Jotunn.Logger.LogError("Failed to load asset bundle with name: " + newBundle + " (expected at " + path + ")");
            return false;
        }
        return true;
    }

    internal static readonly string[] SfxPrefabs =
    {
        "sfx_open_fortified_cwf",
        "sfx_close_fortified_cwf",
        "sfx_oc_drawbridge_cwf",
        "sfx_open_drawbridge_cwf",
        "sfx_close_drawbridge_cwf",
        "sfx_tower_chains_cwf",
        "sfx_elevator_cwf",
        "sfx_turret_scan_cwf",
        "sfx_flames_cwf",
        "sfx_saw_cwf",
        "sfx_spikes_cwf",
        "sfx_electric_fence_cwf",
        "sfx_fence_controller_cwf",
        "sfx_cannon_shot_cwf"
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
