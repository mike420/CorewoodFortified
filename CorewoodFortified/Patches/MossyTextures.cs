using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;

namespace CorewoodFortified;

// Mossy Textures: swaps the wood, log and stone materials on the fortified pieces for "<name>_mossy" versions
// in the bundle (made in Unity by Corewood Fortified > Build Mossy Materials: the original texture with one of
// mm_bark_1..3 blended in faintly). Nothing is built at runtime. Only materials used by this mod's pieces change.
public static class MossyTextures
{
	[HarmonyPatch(typeof(ZNetScene), "Awake")]
	private static class ZNetSceneAwakePatch
	{
		[HarmonyPriority(Priority.VeryLow)]
		private static void Postfix()
		{
			// Prefabs keep their materials between worlds, so this has to be able to undo as well as apply.
			Apply();
		}
	}

	// Keep in step with Materials in MossyMaterialBuilder (Unity project)
	private static readonly HashSet<string> MossyNames = new HashSet<string>
	{
		"corewood_log_flat", "corewood_log_flat_worn", "corewood_logwall", "corewood_logwall2", "corewood_logwall_worn",
		"corelogs", "corelogs_worn", "corewood_wood_bark", "log_bark", "log_bark_worn",
		"plank_wood", "plank_wood_worn", "seamless_wood", "texture_wood", "wood_parts",
		"wood_planks", "wood_planks_worn", "stone_mat"
	};

	// Original material name -> its mossy material
	private static readonly Dictionary<string, Material> MossyByName = new Dictionary<string, Material>();

	// Mossy material -> the material it replaced
	private static readonly Dictionary<Material, Material> OriginalByMossy = new Dictionary<Material, Material>();

	private static HashSet<string> fortifiedPrefabs;

	private static bool applied;

	private static int swapCount;

	public static void OnSettingChanged()
	{
		if (ZNetScene.instance)
		{
			Apply();
		}
	}

	public static void Apply()
	{
		bool wanted = MainConfig.mossyTextures.Value;
		if (wanted == applied || !ZNetScene.instance)
		{
			return;
		}
		if (wanted && !CorewoodFortifiedMod.cwfortified)
		{
			Jotunn.Logger.LogWarning("Mossy Textures: asset bundle " + CorewoodFortifiedMod.newBundle + " is not loaded.");
			return;
		}
		Process(wanted);
		applied = wanted;
	}

	// Disable HD swaps materials by identity, so it asks for the moss to come off first and goes back on after.
	internal static void Suspend()
	{
		if (applied)
		{
			Process(false);
		}
	}

	internal static void Resume()
	{
		if (applied)
		{
			Process(true);
		}
	}

	private static void Process(bool moss)
	{
		swapCount = 0;
		HashSet<string> prefabNames = GetFortifiedPrefabs();
		foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
		{
			if (prefab && prefabNames.Contains(prefab.name.ToLowerInvariant()))
			{
				SwapRenderers(prefab, moss);
			}
		}
		foreach (ZNetView nview in Object.FindObjectsByType<ZNetView>(FindObjectsSortMode.None))
		{
			if (prefabNames.Contains(Utils.GetPrefabName(nview.gameObject).ToLowerInvariant()))
			{
				SwapRenderers(nview.gameObject, moss);
			}
		}
		if (ZiplineVisuals.TrolleyPrefab)
		{
			SwapRenderers(ZiplineVisuals.TrolleyPrefab, moss);
		}
		Jotunn.Logger.LogInfo("[CorewoodFortified] Mossy Textures: " + (moss ? "swapped in " : "swapped out ") + swapCount + " material slots.");
	}

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
			fortifiedPrefabs.Add(ZiplineSetup.PostPrefab.ToLowerInvariant());
		}
		return fortifiedPrefabs;
	}

	private static void SwapRenderers(GameObject root, bool moss)
	{
		foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(includeInactive: true))
		{
			Material[] materials = renderer.sharedMaterials;
			bool changed = false;
			for (int i = 0; i < materials.Length; i++)
			{
				Material replacement = moss ? GetMossy(materials[i]) : GetOriginal(materials[i]);
				if (replacement)
				{
					materials[i] = replacement;
					changed = true;
					swapCount++;
				}
			}
			if (changed)
			{
				renderer.sharedMaterials = materials;
			}
		}
	}

	private static Material GetOriginal(Material material)
	{
		return material && OriginalByMossy.TryGetValue(material, out Material original) ? original : null;
	}

	private static Material GetMossy(Material material)
	{
		if (!material || OriginalByMossy.ContainsKey(material))
		{
			return null;
		}
		string name = material.name.Replace(" (Instance)", "").Trim();
		if (!MossyNames.Contains(name))
		{
			return null;
		}
		if (!MossyByName.TryGetValue(name, out Material mossy))
		{
			mossy = CorewoodFortifiedMod.cwfortified.LoadAsset<Material>(name + "_mossy");
			if (mossy)
			{
				FixShader(mossy, material);
			}
			else
			{
				Jotunn.Logger.LogWarning("Mossy Textures: material '" + name + "_mossy' not found in " + CorewoodFortifiedMod.newBundle + ".");
			}
			MossyByName[name] = mossy;
			if (mossy)
			{
				OriginalByMossy[mossy] = material;
			}
		}
		return mossy;
	}

	private static Dictionary<string, Shader> loadedShaders;

	// Straight from the bundle a material still has a placeholder ("JVLmock_...") shader. If it uses the same one as the
	// original, take the original's (already the real one). If it was given a different one in Unity, keep that choice
	// and look up the real shader of the same name.
	private static void FixShader(Material mossy, Material original)
	{
		string own = mossy.shader ? mossy.shader.name : "";
		if (original.shader && (own == original.shader.name || own == "" || own == "Hidden/InternalErrorShader"))
		{
			mossy.shader = original.shader;
			mossy.renderQueue = original.renderQueue;
			mossy.shaderKeywords = original.shaderKeywords;
			return;
		}
		const string Mock = "JVLmock_";
		if (!own.StartsWith(Mock))
		{
			return;
		}
		if (loadedShaders == null)
		{
			loadedShaders = new Dictionary<string, Shader>();
			foreach (Shader shader in Resources.FindObjectsOfTypeAll<Shader>())
			{
				loadedShaders[shader.name] = shader;
			}
		}
		if (loadedShaders.TryGetValue(own.Substring(Mock.Length), out Shader real))
		{
			mossy.shader = real;
		}
		else
		{
			Jotunn.Logger.LogWarning("Mossy Textures: shader '" + own.Substring(Mock.Length) + "' for '" + mossy.name + "' is not loaded in the game.");
		}
	}
}
