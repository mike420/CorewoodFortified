using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CorewoodFortified;

// The ground trap: a spiked pit hidden under a cover of ground. A placed trap digs the terrain under it down to its
// dig_level, inside its frame, and the ground comes back when the trap is removed. While the trap
// runs the cover is taken away, so whoever stands on it drops onto the spikes; it comes back when the trap stops
internal sealed class GroundTrap : MonoBehaviour, ITrapTargets
{
	public const string PrefabName = "mm_corewood_ground_trap";

	private const string CoverName = "ground_trap_ground";

	private const string DigLevelName = "dig_level";

	private const string FrameName = "ground_trap_top";

	// The terrain has a height every metre and slopes between them, so the pit's sides take up to this much room
	// beyond the last point dug
	private const float TerrainStep = 1f;

	// The narrowest the dug square goes, so a small frame still gets a pit
	private const float SmallestDig = 0.5f;

	// How far below the dig level and above the cover a character can be and still be over the pit, in metres
	private const float SpringBelow = 0.5f;

	private const float SpringAbove = 2.5f;

	private const string HitAreaName = "HIT AREA";

	private const string EnabledName = "_enabled";

	// Set up on the prefab and copied to each placed trap
	public GameObject m_cover;

	// Half the cover's width, and how far below the trap's base the pit is dug, in metres
	public float m_halfSize = 1.6f;

	public float m_digDepth = 0.9f;

	// Half the frame's width, in metres; 0 when it has none
	public float m_frameHalfSize;

	// The frame, and how high the tops of the cover and frame are above the trap's base, for the grass on them
	public Transform m_frame;

	public float m_coverTop;

	public float m_frameTop;

	// The placed traps digging now, for the heightmaps to dig again after the ground's own changes
	private static readonly List<GroundTrap> s_digging = new List<GroundTrap>();

	private ZNetView m_nview;

	private TrapEnabler m_trap;

	private TerrainModifier m_modifier;

	// Half the square of terrain points dug, in the trap's own space
	private float m_digHalf;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_trap = GetComponent<TrapEnabler>();
		// The placement ghost does not dig
		if (m_nview && m_nview.IsValid())
		{
			Dig();
		}
	}

	// Only a character over the cover or down in the pit springs the trap or keeps it open, not one beside it, and
	// never a player while Ground Trap Ignores Players is on
	public bool Springs(Character character)
	{
		if (MainConfig.groundTrapIgnoresPlayers.Value && character is Player)
		{
			return false;
		}
		Vector3 local = transform.InverseTransformPoint(character.transform.position);
		return Mathf.Abs(local.x) <= m_halfSize && Mathf.Abs(local.z) <= m_halfSize && local.y >= -m_digDepth - SpringBelow && local.y <= SpringAbove;
	}

	private void OnDestroy()
	{
		s_digging.Remove(this);
	}

	private void Update()
	{
		if (!m_cover || !m_trap || !m_nview || !m_nview.IsValid())
		{
			return;
		}
		bool covered = !m_trap.IsRunning;
		if (m_cover.activeSelf != covered)
		{
			m_cover.SetActive(covered);
		}
	}

	// Only the terrain points a metre inside the frame's edge are dug, turned with the trap, so the slope up to the
	// untouched ground stays under the frame. The game's own terrain modifier is kept for what it does well: it
	// clears the grass, and redraws the ground when the trap is placed or removed. Its own levelling would dig a
	// square rounded out to whole metres, far wider than the trap, so it is left off and the heightmap patch digs
	private void Dig()
	{
		float edge = m_frameHalfSize > 0f ? m_frameHalfSize : m_halfSize;
		m_digHalf = Mathf.Max(SmallestDig, edge - TerrainStep);
		s_digging.Add(this);
		TerrainModifier modifier = m_modifier = gameObject.AddComponent<TerrainModifier>();
		modifier.m_useTerrainCompiler = false;
		modifier.m_playerModifiction = false;
		modifier.m_level = false;
		modifier.m_smooth = false;
		// No grass growing up through the cover or in the pit. Its radius also sets how far round the trap the
		// ground is redrawn
		modifier.m_paintCleared = true;
		modifier.m_paintType = TerrainModifier.PaintType.Dirt;
		modifier.m_paintRadius = m_halfSize;
		modifier.m_paintStrength = 1f;
		modifier.PokeHeightmaps(false);
	}

	// Lowers the terrain points inside the dug square to the dig level, never raising any
	private void DigInto(Heightmap heightmap)
	{
		Transform hm = heightmap.transform;
		float scale = heightmap.m_scale;
		int side = heightmap.m_width + 1;
		int half = heightmap.m_width / 2;
		float level = transform.position.y - m_digDepth - hm.position.y;
		heightmap.WorldToVertex(transform.position, out int cx, out int cy);
		int reach = Mathf.CeilToInt(m_digHalf * 1.415f / scale) + 1;
		for (int y = Mathf.Max(0, cy - reach); y <= Mathf.Min(side - 1, cy + reach); y++)
		{
			for (int x = Mathf.Max(0, cx - reach); x <= Mathf.Min(side - 1, cx + reach); x++)
			{
				Vector3 point = hm.position + new Vector3((x - half) * scale, 0f, (y - half) * scale);
				Vector3 local = transform.InverseTransformPoint(point);
				if (Mathf.Abs(local.x) > m_digHalf || Mathf.Abs(local.z) > m_digHalf)
				{
					continue;
				}
				int i = y * side + x;
				if (heightmap.m_heights[i] > level)
				{
					heightmap.m_heights[i] = level;
				}
			}
		}
	}

	// Measures the cover and the dig level, hides the dig level marker, and puts the spikes' hit area under an
	// _enabled so it only hurts while the trap runs. The prefab must be a copy rather than the bundle's own, as it
	// is given a new child
	public static void Setup(GameObject piece)
	{
		Transform root = piece.transform;
		Transform cover = Find(root, CoverName);
		if (!cover)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " has no '" + CoverName + "', it will not open or dig.");
			return;
		}
		GroundTrap trap = piece.AddComponent<GroundTrap>();
		trap.m_cover = cover.gameObject;
		Bounds bounds = OrganCannon.MeshBounds(root, cover);
		trap.m_halfSize = Mathf.Max(bounds.extents.x, bounds.extents.z);
		trap.m_coverTop = bounds.max.y;

		Transform frame = Find(root, FrameName);
		if (frame && frame.GetComponent<MeshFilter>())
		{
			Bounds frameBounds = OrganCannon.MeshBounds(root, frame);
			trap.m_frame = frame;
			trap.m_frameHalfSize = Mathf.Max(frameBounds.extents.x, frameBounds.extents.z);
			trap.m_frameTop = frameBounds.max.y;
		}
		piece.AddComponent<GroundTrapGrass>();

		Transform digLevel = Find(root, DigLevelName);
		if (digLevel)
		{
			trap.m_digDepth = -root.InverseTransformPoint(digLevel.position).y;
			digLevel.gameObject.SetActive(false);
		}
		else
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " has no '" + DigLevelName + "', it digs " + trap.m_digDepth + "m down.");
		}

		Transform hitArea = Find(root, HitAreaName);
		if (hitArea && !TrapEnabler.FindEnabledObject(root))
		{
			GameObject enabled = new GameObject(EnabledName);
			enabled.transform.SetParent(hitArea.parent, worldPositionStays: false);
			enabled.SetActive(false);
			hitArea.SetParent(enabled.transform, worldPositionStays: true);
		}
	}

	private static Transform Find(Transform root, string name)
	{
		foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
		{
			if (child.name == name)
			{
				return child;
			}
		}
		return null;
	}

	// The pit is dug after the game has built the ground, including any changes players have made there with the hoe
	// or pickaxe, so it always ends at the dig level
	[HarmonyPatch(typeof(Heightmap), nameof(Heightmap.ApplyModifiers))]
	private static class Heightmap_ApplyModifiers
	{
		private static void Postfix(Heightmap __instance)
		{
			foreach (GroundTrap trap in s_digging)
			{
				if (trap && trap.m_modifier && trap.m_modifier.enabled && __instance.IsPointInside(trap.transform.position, trap.m_digHalf * 1.415f + TerrainStep))
				{
					trap.DigInto(__instance);
				}
			}
		}
	}
}
