using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorewoodFortified;

// Grass on the ground trap's cover and frame, so it hides among the grass round it. The game only grows grass on
// the terrain, so the trap draws its own, the way the game does: the grass of the biome it is in, as thick as the
// player's vegetation setting makes it, with the same patchiness as the ground round it. The cover's grass goes
// with the cover when the trap opens. Each player's own Ground Trap Grass setting turns it on and off
internal sealed class GroundTrapGrass : MonoBehaviour
{
	private GroundTrap m_trap;

	private ZNetView m_nview;

	private readonly List<GameObject> m_grass = new List<GameObject>();

	private bool m_built;

	private void Start()
	{
		m_trap = GetComponent<GroundTrap>();
		m_nview = GetComponent<ZNetView>();
		// The placement ghost has none
		if (!m_trap || !m_nview || !m_nview.IsValid())
		{
			enabled = false;
			return;
		}
		MainConfig.groundTrapGrass.SettingChanged += OnSettingChanged;
		Apply();
	}

	private void OnDestroy()
	{
		MainConfig.groundTrapGrass.SettingChanged -= OnSettingChanged;
	}

	private void OnSettingChanged(object sender, EventArgs e)
	{
		Apply();
	}

	private void Apply()
	{
		bool show = MainConfig.groundTrapGrass.Value;
		if (show && !m_built)
		{
			m_built = Build();
		}
		foreach (GameObject grass in m_grass)
		{
			if (grass)
			{
				grass.SetActive(show);
			}
		}
	}

	// False when there is no grass to be had yet, such as with vegetation turned off, so it is tried again the next
	// time the setting is turned on
	private bool Build()
	{
		ClutterSystem clutter = ClutterSystem.instance;
		if (!clutter || clutter.m_quality == ClutterSystem.Quality.Off)
		{
			return false;
		}
		Vector3 position = transform.position;
		Heightmap.Biome biome = Heightmap.FindBiomeClutter(position);
		Heightmap heightmap = Heightmap.FindHeightmap(position);
		if (biome == Heightmap.Biome.None || !heightmap)
		{
			return false;
		}
		float outer = m_trap.m_frame ? Mathf.Max(m_trap.m_frameHalfSize, m_trap.m_halfSize) : m_trap.m_halfSize;
		float area = outer * 2f * outer * 2f;
		// The same every time and for every player
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(Mathf.RoundToInt(position.x * 13f) ^ Mathf.RoundToInt(position.z * 7919f));
		for (int i = 0; i < clutter.m_clutter.Count; i++)
		{
			ClutterSystem.Clutter kind = clutter.m_clutter[i];
			if (!kind.m_enabled || !kind.m_instanced || !kind.m_onUncleared || !kind.m_prefab || (kind.m_biome & biome) == 0)
			{
				continue;
			}
			int perPatch = clutter.m_quality switch
			{
				ClutterSystem.Quality.Low => kind.m_amount / 4,
				ClutterSystem.Quality.Med => kind.m_amount / 2,
				_ => kind.m_amount,
			};
			float perMetre = perPatch * clutter.m_amountScale / (clutter.m_grassPatchSize * clutter.m_grassPatchSize);
			int count = Mathf.RoundToInt(perMetre * area);
			InstanceRenderer onCover = null;
			InstanceRenderer onFrame = null;
			for (int j = 0; j < count; j++)
			{
				Vector3 local = new Vector3(UnityEngine.Random.Range(-outer, outer), 0f, UnityEngine.Random.Range(-outer, outer));
				float turn = UnityEngine.Random.Range(0f, 360f);
				float scale = UnityEngine.Random.Range(kind.m_scaleMin, kind.m_scaleMax);
				bool cover = Mathf.Abs(local.x) <= m_trap.m_halfSize && Mathf.Abs(local.z) <= m_trap.m_halfSize;
				if (!cover && !m_trap.m_frame)
				{
					continue;
				}
				local.y = cover ? m_trap.m_coverTop : m_trap.m_frameTop;
				Vector3 point = transform.TransformPoint(local);
				if (!Grows(clutter, kind, point, heightmap))
				{
					continue;
				}
				ref InstanceRenderer renderer = ref cover ? ref onCover : ref onFrame;
				if (!renderer)
				{
					renderer = AddRenderer(clutter, kind, cover ? m_trap.m_cover.transform : m_trap.m_frame);
					if (!renderer)
					{
						break;
					}
				}
				renderer.AddInstance(point, Quaternion.Euler(0f, turn, 0f), scale);
			}
		}
		UnityEngine.Random.state = state;
		return true;
	}

	// Whether this kind of grass would grow here on open ground: high enough above the sea, and in or out of forest
	// and in the patches it keeps to, as the game decides for the ground round the trap
	private static bool Grows(ClutterSystem clutter, ClutterSystem.Clutter kind, Vector3 point, Heightmap heightmap)
	{
		float altitude = point.y - clutter.m_waterLevel;
		if (altitude < kind.m_minAlt || altitude > kind.m_maxAlt)
		{
			return false;
		}
		if (kind.m_inForest)
		{
			float forest = WorldGenerator.GetForestFactor(point);
			if (forest < kind.m_forestTresholdMin || forest > kind.m_forestTresholdMax)
			{
				return false;
			}
		}
		if (kind.m_fractalScale > 0f)
		{
			float fractal = Utils.Fbm(point * 0.01f * kind.m_fractalScale + new Vector3(kind.m_fractalOffset, 0f, 0f), 3, 1.6f, 0.7f);
			if (fractal < kind.m_fractalTresholdMin || fractal > kind.m_fractalTresholdMax)
			{
				return false;
			}
		}
		if (kind.m_minVegetation != kind.m_maxVegetation)
		{
			float vegetation = heightmap.GetVegetationMask(point);
			if (vegetation > kind.m_maxVegetation || vegetation < kind.m_minVegetation)
			{
				return false;
			}
		}
		return true;
	}

	// The game's own grass object for this kind, holding the blades as the game's grass patches do. It sits under the
	// cover or frame so it hides with it
	private InstanceRenderer AddRenderer(ClutterSystem clutter, ClutterSystem.Clutter kind, Transform parent)
	{
		GameObject grass = Instantiate(kind.m_prefab, transform.position, Quaternion.identity, parent);
		grass.name = "cwf_grass_" + kind.m_name;
		InstanceRenderer renderer = grass.GetComponent<InstanceRenderer>();
		if (!renderer)
		{
			Destroy(grass);
			return null;
		}
		renderer.Clear();
		float furthest = clutter.m_distance - clutter.m_grassPatchSize / 2f;
		if (renderer.m_lodMaxDistance > furthest)
		{
			renderer.m_lodMaxDistance = furthest;
		}
		m_grass.Add(grass);
		return renderer;
	}
}
