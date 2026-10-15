using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Rendering;

namespace CorewoodFortified;

internal static class ChainMaterials
{
	private static readonly Dictionary<string, Material> s_found = new Dictionary<string, Material>();

	private static Material s_chain;

	private static Material s_preview;

	private static Material s_overlay;

	public static Material Iron => OfPiece("woodiron_pole", "Ironbeam_mat") ?? First("Ironbeam_mat", "metalwall", "iron");

	public static Material Chain
	{
		get
		{
			if (s_chain != null)
			{
				return s_chain;
			}
			Material iron = Iron;
			if (iron == null)
			{
				return null;
			}
			s_chain = new Material(iron)
			{
				name = "CWChain_chain"
			};
			s_chain.EnableKeyword("_MOVEABLEOBJECT_ON");
			if (s_chain.HasProperty("_TriplanarLocalPos"))
			{
				s_chain.SetFloat("_TriplanarLocalPos", 1f);
			}
			if (s_chain.HasProperty("_ValueNoise"))
			{
				s_chain.SetFloat("_ValueNoise", 0f);
			}
			return s_chain;
		}
	}

	public static Material Preview
	{
		get
		{
			if (s_preview != null)
			{
				return s_preview;
			}
			Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended");
			if (shader == null)
			{
				Material material = Find("item_particle");
				shader = ((material != null) ? material.shader : null);
			}
			if (shader == null)
			{
				return null;
			}
			s_preview = new Material(shader)
			{
				name = "CWChain_preview"
			};
			return s_preview;
		}
	}

	// Drawn after and on top of everything, water included, for the placement preview line and direction arrow.
	public static Material Overlay
	{
		get
		{
			if (s_overlay != null)
			{
				return s_overlay;
			}
			Shader shader = Shader.Find("UI/Default");
			if (shader == null)
			{
				return Preview;
			}
			s_overlay = new Material(shader)
			{
				name = "CWChain_overlay",
				renderQueue = 4000
			};
			s_overlay.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
			return s_overlay;
		}
	}

	public static Material Find(string name)
	{
		if (s_found.TryGetValue(name, out var value) && value != null)
		{
			return value;
		}
		Material[] array = Resources.FindObjectsOfTypeAll<Material>();
		foreach (Material material in array)
		{
			if (!(material == null) && !(material.name != name))
			{
				s_found[name] = material;
				return material;
			}
		}
		return null;
	}

	public static Material First(params string[] names)
	{
		for (int i = 0; i < names.Length; i++)
		{
			Material material = Find(names[i]);
			if (material != null)
			{
				return material;
			}
		}
		return null;
	}

	public static Material OfPiece(string prefab, string material)
	{
		string key = prefab + "/" + material;
		if (s_found.TryGetValue(key, out var value) && value != null)
		{
			return value;
		}
		GameObject gameObject = PrefabManager.Cache.GetPrefab<GameObject>(prefab);
		if (gameObject == null)
		{
			return null;
		}
		Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Material[] sharedMaterials = componentsInChildren[i].sharedMaterials;
			foreach (Material material2 in sharedMaterials)
			{
				if (material2 != null && material2.name == material)
				{
					s_found[key] = material2;
					return material2;
				}
			}
		}
		return null;
	}

	public static void Unload()
	{
		s_chain = null;
		s_preview = null;
		s_overlay = null;
		s_found.Clear();
	}
}
