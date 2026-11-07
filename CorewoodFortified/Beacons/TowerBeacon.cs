using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CorewoodFortified;

// Reveals the map around a tower beacon while a lit small fortified brazier stands on its top, out to the Beacon Reveal Radius setting.
// Each player's own map is revealed once the beacon is loaded near them, and is saved with their character like any explored land
public class TowerBeacon : MonoBehaviour
{
	private const string BrazierPrefab = "mm_corewood_fortified_brazier_small";

	private const float CheckInterval = 2f;

	// How far a brazier's pivot may be from the beacon's top snap points and still count as standing on it
	private const float TopTolerance = 0.5f;

	// The brazier snaps by the snap points on its corners, a metre out from its pivot each way, so its pivot can sit
	// well over a metre from the beacon's snap points sideways and front to back
	private const float SideTolerance = 1.5f;

	private static readonly Collider[] s_hits = new Collider[64];

	private static int s_pieceMask;

	// Braziers kept dry by Beacon Unlimited Fuel: wind and rain do not put them out
	private static readonly HashSet<Fireplace> s_sheltered = new HashSet<Fireplace>();

	private static readonly AccessTools.FieldRef<Fireplace, bool> s_wet = AccessTools.FieldRefAccess<Fireplace, bool>("m_wet");

	// The brazier this beacon is keeping dry, if any
	private Fireplace m_sheltered;

	private ZNetView m_nview;

	private Piece m_piece;

	// In the beacon's local space: where a brazier's pivot has to be, from the beacon's snap points
	private Bounds m_top;

	// The radius already revealed by this beacon since it loaded, so a raised setting reveals the rest
	private float m_revealedRadius;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_piece = GetComponent<Piece>();
		if (s_pieceMask == 0)
		{
			s_pieceMask = LayerMask.GetMask("piece", "piece_nonsolid");
		}
		m_top = TopArea();
	}

	private void Start()
	{
		if (m_nview == null || !m_nview.IsValid() || (ZNet.instance != null && ZNet.instance.IsDedicated()))
		{
			return;
		}
		InvokeRepeating(nameof(Check), Random.Range(0.5f, CheckInterval), CheckInterval);
	}

	private void Check()
	{
		if (!m_nview.IsValid() || Player.m_localPlayer == null || Minimap.instance == null)
		{
			return;
		}
		Piece brazier = FindBrazier();
		bool unlimited = MainConfig.beaconUnlimitedFuel.Value;
		Shelter(unlimited && brazier != null ? brazier.GetComponent<Fireplace>() : null);
		if (brazier == null)
		{
			return;
		}
		if (unlimited)
		{
			Refuel(brazier);
		}
		float radius = MainConfig.beaconRevealRadius.Value;
		if (radius <= m_revealedRadius || !IsLit(brazier))
		{
			return;
		}
		Minimap.instance.Explore(transform.position, radius);
		m_revealedRadius = radius;
	}

	private void OnDestroy()
	{
		Shelter(null);
	}

	private void Shelter(Fireplace fireplace)
	{
		if (m_sheltered == fireplace)
		{
			return;
		}
		if (m_sheltered != null)
		{
			s_sheltered.Remove(m_sheltered);
		}
		s_sheltered.RemoveWhere(f => f == null);
		m_sheltered = fireplace;
		if (fireplace != null)
		{
			s_sheltered.Add(fireplace);
			s_wet(fireplace) = false;
		}
	}

	// Wind and rain make a fire wet, and the game then switches a wet brazier off; a sheltered one stays dry
	[HarmonyPatch(typeof(Fireplace), "CheckWet")]
	private static class Fireplace_CheckWet
	{
		private static void Postfix(Fireplace __instance)
		{
			if (s_sheltered.Contains(__instance) && MainConfig.beaconUnlimitedFuel.Value)
			{
				s_wet(__instance) = false;
			}
		}
	}

	// The small fortified brazier standing on the beacon's top, if any; a lit one first
	private Piece FindBrazier()
	{
		Vector3 center = transform.TransformPoint(m_top.center);
		int count = Physics.OverlapSphereNonAlloc(center, m_top.extents.magnitude + 1f, s_hits, s_pieceMask);
		Piece found = null;
		for (int i = 0; i < count; i++)
		{
			Piece piece = s_hits[i].GetComponentInParent<Piece>();
			if (piece != null && piece != m_piece && Utils.GetPrefabName(piece.gameObject) == BrazierPrefab && m_top.Contains(transform.InverseTransformPoint(piece.transform.position)))
			{
				if (IsLit(piece))
				{
					return piece;
				}
				found ??= piece;
			}
		}
		return found;
	}

	// Beacon Unlimited Fuel: the brazier's owner keeps it full, so it lights even when empty and never burns out.
	// Switching it off or covering it over still puts it out; wind and rain do not
	private static void Refuel(Piece brazier)
	{
		Fireplace fireplace = brazier.GetComponent<Fireplace>();
		ZNetView nview = brazier.GetComponent<ZNetView>();
		if (fireplace == null || nview == null || !nview.IsValid() || !nview.IsOwner())
		{
			return;
		}
		ZDO zdo = nview.GetZDO();
		if (zdo.GetFloat(ZDOVars.s_fuel) < fireplace.m_maxFuel)
		{
			zdo.Set(ZDOVars.s_fuel, fireplace.m_maxFuel);
		}
	}

	// Burning as the game sees it: fuelled, switched on, not covered over and not under water
	private static bool IsLit(Piece brazier)
	{
		Fireplace fireplace = brazier.GetComponent<Fireplace>();
		ZNetView nview = brazier.GetComponent<ZNetView>();
		return fireplace != null && nview != null && nview.IsValid() && fireplace.IsBurning();
	}

	// The top snap points, the brazier's and the ladder's, spread a little sideways and up and down
	private Bounds TopArea()
	{
		float topY = float.MinValue;
		float minX = 0f, maxX = 0f, minZ = 0f, maxZ = 0f;
		for (int i = 0; i < transform.childCount; i++)
		{
			Transform child = transform.GetChild(i);
			if (!child.CompareTag("snappoint"))
			{
				continue;
			}
			Vector3 p = child.localPosition;
			topY = Mathf.Max(topY, p.y);
			minX = Mathf.Min(minX, p.x);
			maxX = Mathf.Max(maxX, p.x);
			minZ = Mathf.Min(minZ, p.z);
			maxZ = Mathf.Max(maxZ, p.z);
		}
		if (topY == float.MinValue)
		{
			topY = 6f;
		}
		Bounds bounds = new Bounds();
		bounds.SetMinMax(new Vector3(minX - SideTolerance, topY - TopTolerance, minZ - SideTolerance), new Vector3(maxX + SideTolerance, topY + TopTolerance, maxZ + SideTolerance));
		return bounds;
	}
}
