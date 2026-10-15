using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;

namespace CorewoodFortified;

internal static class PieceAssets
{
    public static EffectList buildWood;
    public static EffectList breakWood;
    public static EffectList hitWood;
    public static EffectList buildMetal;
    public static EffectList breakMetal;
    public static EffectList hitMetal;
    public static EffectList doorOpen;
    public static EffectList doorClose;
    public static EffectList chestOpen;
    public static EffectList chestClose;
    public static EffectList fortifiedOpen;
    public static EffectList fortifiedClose;
    public static EffectList drawBridgeOC;
    public static EffectList addFuel;

    public static ItemDrop fuelCoal;
    public static ItemDrop fuelResin;

    private static bool s_built;

    public static void Init()
    {
        if (s_built)
        {
            return;
        }

        buildWood = Effects("sfx_build_hammer_wood");
        breakWood = Effects("sfx_wood_break");
        hitWood = Effects("sfx_wood_hit");

        buildMetal = Effects("sfx_build_hammer_metal");
        breakMetal = Effects("sfx_metal_blocked");
        hitMetal = Effects("sfx_metal_blocked");

        doorOpen = Effects("sfx_door_open");
        doorClose = Effects("sfx_door_close");

        chestOpen = Effects("sfx_chest_open");
        chestClose = Effects("sfx_chest_close");

        fortifiedOpen = Effects("sfx_open_fortified_cwf");
        fortifiedClose = Effects("sfx_close_fortified_cwf");

        drawBridgeOC = Effects("sfx_oc_drawbridge_cwf");

        addFuel = Effects("vfx_walltorch_addFuel", "sfx_FireAddFuel");

        fuelCoal = PrefabManager.Cache.GetPrefab<GameObject>("Coal").GetComponent<ItemDrop>();
        fuelResin = PrefabManager.Cache.GetPrefab<GameObject>("Resin").GetComponent<ItemDrop>();

        s_built = true;
    }

    public static EffectList Effects(params string[] prefabNames)
    {
        List<EffectList.EffectData> effects = new List<EffectList.EffectData>(prefabNames.Length);
        foreach (string prefabName in prefabNames)
        {
            GameObject prefab = PrefabManager.Instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Jotunn.Logger.LogWarning("[CorewoodFortified] Effect prefab '" + prefabName + "' not found, skipping.");
                continue;
            }
            effects.Add(new EffectList.EffectData { m_prefab = prefab });
        }
        return new EffectList { m_effectPrefabs = effects.ToArray() };
    }
}
