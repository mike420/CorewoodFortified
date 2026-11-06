using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;

namespace CorewoodFortified;

// Disable HD: swaps the HD materials on the fortified pieces for the low quality versions in the bundle.
public static class LowQualityMaterials
{
	[HarmonyPatch(typeof(ZNetScene), "Awake")]
	private static class ZNetSceneAwakePatch
	{
		[HarmonyPriority(Priority.Low)]
		private static void Postfix()
		{
			if (MainConfig.disableHDTextures.Value)
			{
				Apply();
			}
		}
	}

	// Original material name -> low quality material name in the corewood_fortified bundle.
	// A low quality material may serve several originals; each original gets its own copy so it can be swapped back.
	private static readonly Dictionary<string, string> LowQualityNames = new Dictionary<string, string>
	{
		{ "corewood_log_flat", "corewood_log_flat_lq" },
		{ "corewood_log_flat_worn", "corewood_log_flat_worn_lq" },
		{ "corewood_logwall", "corewood_logwall_lq" },
        { "corewood_logwall2", "corewood_logwall_lq" },
        { "corewood_logwall_worn", "corewood_logwall_worn_lq" },
		{ "corelogs", "corelogs_lq" },
		{ "corelogs_worn", "corelogs_worn_lq" },
		{ "corewood_wood_bark", "log_bark_lq" },
		{ "plank_wood", "plank_wood_lq" },
		{ "plank_wood_worn", "plank_wood_worn_lq" },
		{ "seamless_wood", "seamless_wood_lq" },
		{ "texture_wood", "texture_wood_worn_lq" },
		{ "wood_parts", "wood_parts_lq" },
		{ "wood_planks", "wood_planks_lq" },
		{ "wood_planks_worn", "wood_planks_worn_lq" },
        { "power_generator", "power_generator_lq" },
        { "stone_mat", "stone_mat_lq" },
        { "straw_roof", "straw_roof_lq" },
        { "straw_roof_alpha", "straw_roof_alpha_lq" },
        { "straw_roof_worn", "straw_roof_worn_lq" },
        { "straw_roof_worn_alpha", "straw_roof_worn_alpha_lq" }
    };

	private static Dictionary<string, Material> lowQualityMaterials;

	// Low quality material -> the HD material it replaced, filled in as materials are swapped.
	private static readonly Dictionary<Material, Material> OriginalMaterials = new Dictionary<Material, Material>();

	// Original material name -> the low quality material used in its place, one per original.
	private static readonly Dictionary<string, Material> LowQualityByOriginal = new Dictionary<string, Material>();

	private static HashSet<string> fortifiedPrefabs;

	private static bool swapped;

	public static void OnSettingChanged()
	{
		if (ZNetScene.instance)
		{
			Apply();
		}
	}

	public static void Apply()
	{
		ZNetScene zNetScene = ZNetScene.instance;
		if (!zNetScene)
		{
			return;
		}
		bool lowQuality = MainConfig.disableHDTextures.Value;
		if (lowQuality == swapped)
		{
			return;
		}
		if (lowQuality && !LoadLowQualityMaterials())
		{
			return;
		}
		HashSet<string> prefabNames = GetFortifiedPrefabs();
		foreach (GameObject prefab in zNetScene.m_prefabs)
		{
			if (prefab && prefabNames.Contains(prefab.name.ToLowerInvariant()))
			{
				SwapRenderers(prefab, lowQuality);
			}
		}
		foreach (ZNetView nview in Object.FindObjectsByType<ZNetView>(FindObjectsSortMode.None))
		{
			if (prefabNames.Contains(Utils.GetPrefabName(nview.gameObject).ToLowerInvariant()))
			{
				SwapRenderers(nview.gameObject, lowQuality);
			}
		}
		swapped = lowQuality;
	}

	// Only touch prefabs from this mod's bundle, so pieces from other Corewood mods are left alone.
	private static HashSet<string> GetFortifiedPrefabs()
	{
		if (fortifiedPrefabs != null)
		{
			return fortifiedPrefabs;
		}
		fortifiedPrefabs = new HashSet<string>();
		if (CorewoodFortifiedMod.cwfortified)
		{
			foreach (string assetPath in CorewoodFortifiedMod.cwfortified.GetAllAssetNames())
			{
				if (assetPath.EndsWith(".prefab"))
				{
					fortifiedPrefabs.Add(Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant());
				}
			}
		}
		return fortifiedPrefabs;
	}

	private static bool LoadLowQualityMaterials()
	{
		if (lowQualityMaterials != null)
		{
			return lowQualityMaterials.Count > 0;
		}
		lowQualityMaterials = new Dictionary<string, Material>();
		if (!CorewoodFortifiedMod.cwfortified)
		{
			Jotunn.Logger.LogWarning("Disable HD: asset bundle " + CorewoodFortifiedMod.newBundle + " is not loaded, cannot swap materials.");
			return false;
		}
		foreach (string lqName in new HashSet<string>(LowQualityNames.Values))
		{
			Material material = CorewoodFortifiedMod.cwfortified.LoadAsset<Material>(lqName);
			if (material)
			{
				lowQualityMaterials[lqName] = material;
			}
			else
			{
				Jotunn.Logger.LogWarning("Disable HD: low quality material '" + lqName + "' not found in " + CorewoodFortifiedMod.newBundle + ".");
			}
		}
		return lowQualityMaterials.Count > 0;
	}

	private static void SwapRenderers(GameObject root, bool lowQuality)
	{
		foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(includeInactive: true))
		{
			Material[] materials = renderer.sharedMaterials;
			bool changed = false;
			for (int i = 0; i < materials.Length; i++)
			{
				Material replacement = lowQuality ? GetLowQuality(materials[i]) : GetOriginal(materials[i]);
				if (replacement)
				{
					materials[i] = replacement;
					changed = true;
				}
			}
			if (changed)
			{
				renderer.sharedMaterials = materials;
			}
		}
	}

	private static Material GetLowQuality(Material material)
	{
		if (!material)
		{
			return null;
		}
		string originalName = CleanName(material.name);
		if (LowQualityByOriginal.TryGetValue(originalName, out Material lqMaterial) && lqMaterial)
		{
			return lqMaterial;
		}
		if (!LowQualityNames.TryGetValue(originalName, out string lqName)
			|| !lowQualityMaterials.TryGetValue(lqName, out Material source))
		{
			return null;
		}
		// The bundle material is already standing in for another original: use a copy, so each
		// low quality material maps back to exactly one original.
		lqMaterial = OriginalMaterials.ContainsKey(source) ? new Material(source) { name = source.name } : source;
		LowQualityByOriginal[originalName] = lqMaterial;
		OriginalMaterials[lqMaterial] = material;
		FixShader(lqMaterial, material);
		return lqMaterial;
	}

	private static Material GetOriginal(Material material)
	{
		return material && OriginalMaterials.TryGetValue(material, out Material original) ? original : null;
	}

	// Materials loaded straight from the bundle still use the placeholder shader, unlike the
	// piece prefabs, which Jotunn fixes. Give them the real shader the original material uses.
	private static void FixShader(Material lqMaterial, Material original)
	{
		lqMaterial.shader = original.shader;
		lqMaterial.renderQueue = original.renderQueue;
		lqMaterial.shaderKeywords = original.shaderKeywords;
	}

	private static string CleanName(string name)
	{
		return name.Replace(" (Instance)", "").Trim();
	}
}
