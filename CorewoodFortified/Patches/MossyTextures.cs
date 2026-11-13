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

	// The low quality versions, used while Disable HD is on. The copies Disable HD makes are named
	// "<low quality name>_<original name>", so they are matched by their start.
	private static readonly string[] MossyLowQualityNames =
	{
		"corewood_log_flat_lq", "corewood_log_flat_worn_lq", "corewood_logwall_lq", "corewood_logwall_worn_lq", "corelogs_lq", "corelogs_worn_lq", "log_bark_lq", "log_bark_worn_lq", "plank_wood_lq", "plank_wood_worn_lq", "seamless_wood_lq", "wood_parts_lq", "wood_planks_lq", "wood_planks_worn_lq", "stone_mat_lq"
	};

	// Material -> the mossy material standing in for it
	private static readonly Dictionary<Material, Material> MossyByMaterial = new Dictionary<Material, Material>();

	// Mossy material -> the material it replaced
	private static readonly Dictionary<Material, Material> OriginalByMossy = new Dictionary<Material, Material>();

	// Mossy asset name -> the material loaded from the bundle (a second material needing it gets a copy)
	private static readonly Dictionary<string, Material> AssetsByName = new Dictionary<string, Material>();

	private static HashSet<string> fortifiedPrefabs;

	private static bool applied;

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

	// Every slot that was swapped, so it can be put back exactly
	private sealed class Swap
	{
		public Renderer Renderer;
		public int Index;
		public Material Original;
		public Material Mossy;
	}

	private static readonly List<Swap> Swaps = new List<Swap>();

	private static void Process(bool moss)
	{
		if (!moss)
		{
			Revert();
			return;
		}
		int before = Swaps.Count;
		HashSet<string> prefabNames = GetFortifiedPrefabs();
		foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
		{
			if (prefab && prefabNames.Contains(prefab.name.ToLowerInvariant()))
			{
				SwapRenderers(prefab);
			}
		}
		int prefabSlots = Swaps.Count - before;
		foreach (ZNetView nview in Object.FindObjectsByType<ZNetView>(FindObjectsSortMode.None))
		{
			if (prefabNames.Contains(Utils.GetPrefabName(nview.gameObject).ToLowerInvariant()))
			{
				SwapRenderers(nview.gameObject);
			}
		}
		int instanceSlots = Swaps.Count - before - prefabSlots;
		if (ZiplineVisuals.TrolleyPrefab)
		{
			SwapRenderers(ZiplineVisuals.TrolleyPrefab);
		}
		Jotunn.Logger.LogInfo("[CorewoodFortified] Mossy Textures: swapped in " + (Swaps.Count - before) + " material slots (" + prefabSlots + " on prefabs, " + instanceSlots + " on placed pieces).");
	}

	// The game can replace a slot's material with its own per-renderer copy, named "<name> (Instance)"
	private static bool IsOurs(Material held, Material mossy)
	{
		return held && (held == mossy || held.name == mossy.name + " (Instance)");
	}

	// Pieces already placed hold the game's own copies of the mossy materials ("<name> (Instance)"), which were not
	// recorded as swaps: find them by name and put the original back
	private static int RevertPlacedPieces()
	{
		if (!ZNetScene.instance)
		{
			return 0;
		}
		Dictionary<string, Material> originals = new Dictionary<string, Material>();
		foreach (KeyValuePair<Material, Material> pair in OriginalByMossy)
		{
			if (pair.Key)
			{
				originals[pair.Key.name] = pair.Value;
			}
		}
		HashSet<string> prefabNames = GetFortifiedPrefabs();
		int count = 0;
		foreach (ZNetView nview in Object.FindObjectsByType<ZNetView>(FindObjectsSortMode.None))
		{
			if (!prefabNames.Contains(Utils.GetPrefabName(nview.gameObject).ToLowerInvariant()))
			{
				continue;
			}
			foreach (Renderer renderer in nview.GetComponentsInChildren<Renderer>(includeInactive: true))
			{
				Material[] materials = renderer.sharedMaterials;
				bool changed = false;
				for (int i = 0; i < materials.Length; i++)
				{
					if (materials[i] && originals.TryGetValue(materials[i].name.Replace(" (Instance)", "").Trim(), out Material original))
					{
						materials[i] = original;
						changed = true;
						count++;
					}
				}
				if (changed)
				{
					renderer.sharedMaterials = materials;
				}
			}
		}
		return count;
	}

	private static void Revert()
	{
		int reverted = 0, gone = 0, outOfRange = 0, changed = 0;
		string firstChange = null;
		foreach (Swap swap in Swaps)
		{
			if (!swap.Renderer)
			{
				gone++;
				continue;
			}
			Material[] materials = swap.Renderer.sharedMaterials;
			if (swap.Index >= materials.Length)
			{
				outOfRange++;
			}
			else if (IsOurs(materials[swap.Index], swap.Mossy))
			{
				materials[swap.Index] = swap.Original;
				swap.Renderer.sharedMaterials = materials;
				reverted++;
			}
			else
			{
				changed++;
				if (firstChange == null)
				{
					firstChange = swap.Renderer.name + "[" + swap.Index + "] holds '" + (materials[swap.Index] ? materials[swap.Index].name : "null") + "', expected '" + swap.Mossy.name + "'";
				}
			}
		}
		int placed = RevertPlacedPieces();
		Jotunn.Logger.LogInfo("[CorewoodFortified] Mossy Textures: swapped out " + reverted + " of " + Swaps.Count + " material slots (" + gone + " renderers gone, " + outOfRange + " slots missing, " + changed + " slots changed by something else), plus " + placed + " slots on placed pieces." + (firstChange != null ? " First: " + firstChange : ""));
		Swaps.Clear();
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

	private static void SwapRenderers(GameObject root)
	{
		foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(includeInactive: true))
		{
			Material[] materials = renderer.sharedMaterials;
			bool changed = false;
			for (int i = 0; i < materials.Length; i++)
			{
				Material original = materials[i];
				Material mossy = GetMossy(original);
				if (mossy)
				{
					materials[i] = mossy;
					Swaps.Add(new Swap { Renderer = renderer, Index = i, Original = original, Mossy = mossy });
					changed = true;
				}
			}
			if (changed)
			{
				renderer.sharedMaterials = materials;
			}
		}
	}

	private static Material GetMossy(Material material)
	{
		if (!material || OriginalByMossy.ContainsKey(material))
		{
			return null;
		}
		if (MossyByMaterial.TryGetValue(material, out Material cached))
		{
			return cached;
		}
		Material mossy = null;
		string assetName = MossyAssetName(material.name.Replace(" (Instance)", "").Trim());
		if (assetName != null)
		{
			EnsureRealShader(material);
			if (!AssetsByName.TryGetValue(assetName, out Material asset))
			{
				asset = CorewoodFortifiedMod.cwfortified.LoadAsset<Material>(assetName);
				if (asset)
				{
					FixShader(asset, material);
				}
				else
				{
					Jotunn.Logger.LogWarning("Mossy Textures: material '" + assetName + "' not found in " + CorewoodFortifiedMod.newBundle + ".");
				}
				AssetsByName[assetName] = asset;
				mossy = asset;
			}
			else if (asset)
			{
				// Already standing in for another material: this one gets its own copy, so each maps back to exactly one
				mossy = new Material(asset) { name = asset.name + "_" + material.name };
			}
		}
		MossyByMaterial[material] = mossy;
		if (mossy)
		{
			OriginalByMossy[mossy] = material;
		}
		return mossy;
	}

	private static string MossyAssetName(string name)
	{
		if (MossyNames.Contains(name))
		{
			return name + "_mossy";
		}
		string best = null;
		foreach (string lq in MossyLowQualityNames)
		{
			if ((name == lq || name.StartsWith(lq + "_")) && (best == null || lq.Length > best.Length))
			{
				best = lq;
			}
		}
		return best == null ? null : best + "_mossy";
	}

	private static Dictionary<string, Shader> loadedShaders;

	private const string Mock = "JVLmock_";

	private static string RealName(string name)
	{
		return name.StartsWith(Mock) ? name.Substring(Mock.Length) : name;
	}

	private static Shader FindShader(string name)
	{
		if (loadedShaders != null && loadedShaders.TryGetValue(name, out Shader found) && found)
		{
			return found;
		}
		loadedShaders = new Dictionary<string, Shader>();
		foreach (Shader shader in Resources.FindObjectsOfTypeAll<Shader>())
		{
			loadedShaders[shader.name] = shader;
		}
		return loadedShaders.TryGetValue(name, out found) ? found : null;
	}

	// Straight from the bundle a material has a placeholder ("JVLmock_...") shader, which draws wrongly. Give it the real
	// shader of the same name, whichever one it was given in Unity. When it is the same kind of shader as the material it
	// stands in for, take that material's queue and keywords too.
	private static void FixShader(Material mossy, Material original)
	{
		string own = mossy.shader ? mossy.shader.name : "";
		string originalName = original.shader ? RealName(original.shader.name) : "";
		if (own == "" || own == "Hidden/InternalErrorShader")
		{
			own = originalName;
			mossy.shader = original.shader;
		}
		string wanted = RealName(own);
		if (wanted == originalName && original.shader && !original.shader.name.StartsWith(Mock))
		{
			// The material it stands in for already renders with the real shader: use exactly that one
			mossy.shader = original.shader;
		}
		else if (own.StartsWith(Mock))
		{
			Shader real = FindShader(wanted);
			if (real)
			{
				mossy.shader = real;
			}
			else
			{
				Jotunn.Logger.LogWarning("Mossy Textures: shader '" + wanted + "' for '" + mossy.name + "' is not loaded in the game.");
			}
		}
		if (wanted == originalName)
		{
			mossy.renderQueue = original.renderQueue;
			mossy.shaderKeywords = original.shaderKeywords;
		}
	}

	// A material still holding its placeholder ("JVLmock_...") shader only gets the real one when Jotunn goes over the
	// materials on the pieces, and a material swapped out for a mossy one is missed: it would come back drawing wrongly
	// (see-through). So give it the real shader before it is swapped out.
	internal static void EnsureRealShader(Material material)
	{
		if (!material.shader || !material.shader.name.StartsWith(Mock))
		{
			return;
		}
		Shader real = FindShader(RealName(material.shader.name));
		if (!real)
		{
			Jotunn.Logger.LogWarning("Mossy Textures: shader '" + RealName(material.shader.name) + "' for '" + material.name + "' is not loaded in the game.");
			return;
		}
		int queue = material.renderQueue;
		string[] keywords = material.shaderKeywords;
		material.shader = real;
		material.renderQueue = queue;
		material.shaderKeywords = keywords;
	}
}
