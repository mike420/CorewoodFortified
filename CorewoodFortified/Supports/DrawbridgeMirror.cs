using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CorewoodFortified;

// Placing a large drawbridge also places a second one turned around to face it, so the two halves
// meet at the prefab's bridge_attach_01 / bridge_attach_02 points and make one full drawbridge.
// The two halves open and close together.
internal sealed class DrawbridgeMirror : MonoBehaviour, IPlaced
{
	private const string Attach1 = "bridge_attach_01";

	private const string Attach2 = "bridge_attach_02";

	private static readonly List<DrawbridgeMirror> All = new List<DrawbridgeMirror>();

	// Prefab name -> the middle of its two attach points in the prefab's own space.
	private static readonly Dictionary<string, Vector3?> Joins = new Dictionary<string, Vector3?>();

	// Set while the mirror half is being placed, so it does not place a mirror of its own.
	private static bool s_placingMirror;

	private ZNetView m_nview;

	private Door m_door;

	private DrawbridgeMirror m_partner;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_door = GetComponent<Door>();
		All.Add(this);
	}

	private void OnDestroy()
	{
		All.Remove(this);
	}

	// Where the other half goes for a half at this position and rotation.
	private static bool TryGetMirror(GameObject half, out Vector3 position, out Quaternion rotation)
	{
		position = default;
		rotation = default;
		Vector3? joinLocal = GetJoin(Utils.GetPrefabName(half));
		if (!joinLocal.HasValue)
		{
			return false;
		}
		// Turn the piece half way round its up axis through the join, which swaps the two attach points.
		Transform t = half.transform;
		Vector3 join = t.TransformPoint(joinLocal.Value);
		Quaternion turn = Quaternion.AngleAxis(180f, t.up);
		position = join + turn * (t.position - join);
		rotation = turn * t.rotation;
		return true;
	}

	// Read from the prefab, as built rather than animated, so it is the same whatever state the door is in.
	private static Vector3? GetJoin(string prefabName)
	{
		if (Joins.TryGetValue(prefabName, out Vector3? cached))
		{
			return cached;
		}
		GameObject prefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab(prefabName) : null;
		if (!prefab)
		{
			return null;
		}
		Transform a = FindChild(prefab.transform, Attach1);
		Transform b = FindChild(prefab.transform, Attach2);
		Vector3? join = null;
		if (a && b)
		{
			join = prefab.transform.InverseTransformPoint((a.position + b.position) * 0.5f);
		}
		else
		{
			Jotunn.Logger.LogWarning($"{prefabName}: {Attach1} or {Attach2} not found, cannot place the mirror half.");
		}
		Joins[prefabName] = join;
		return join;
	}

	private static Transform FindChild(Transform parent, string name)
	{
		foreach (Transform child in parent.GetComponentsInChildren<Transform>(includeInactive: true))
		{
			if (child.name == name)
			{
				return child;
			}
		}
		return null;
	}

	private static bool HasTwoSets(Player player, Piece piece)
	{
		Inventory inventory = player.GetInventory();
		foreach (Piece.Requirement requirement in piece.m_resources)
		{
			if (requirement.m_resItem
				&& inventory.CountItems(requirement.m_resItem.m_itemData.m_shared.m_name) < requirement.m_amount * 2)
			{
				return false;
			}
		}
		return true;
	}

	// Checked before the first half is built: the other half must be buildable too, or neither is.
	private static bool CanPlaceBoth(Player player, Piece piece)
	{
		GameObject ghost = player.m_placementGhost;
		if (!ghost || !TryGetMirror(ghost, out Vector3 position, out _))
		{
			return true;
		}
		if (!PrivateArea.CheckAccess(position, 0f, true, false))
		{
			player.Message(MessageHud.MessageType.Center, LocalisedText.Get("text_drawbridge_mirror_warded"));
			return false;
		}
		if (!player.NoCostCheat() && !HasTwoSets(player, piece))
		{
			player.Message(MessageHud.MessageType.Center, LocalisedText.Get("text_drawbridge_mirror_no_materials"));
			return false;
		}
		return true;
	}

	public void OnPlaced()
	{
		if (s_placingMirror)
		{
			return;
		}
		Player player = Player.m_localPlayer;
		GameObject prefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab(Utils.GetPrefabName(gameObject)) : null;
		Piece piece = prefab ? prefab.GetComponent<Piece>() : null;
		if (!player || !piece || !TryGetMirror(gameObject, out Vector3 position, out Quaternion rotation))
		{
			return;
		}
		bool free = player.NoCostCheat();
		s_placingMirror = true;
		try
		{
			player.PlacePiece(piece, position, rotation, doAttack: false, cheated: free);
		}
		finally
		{
			s_placingMirror = false;
		}
		// The first half's materials are taken after this by the game; take the second half's here.
		if (!free)
		{
			player.ConsumeResources(piece.m_resources, 0, -1, 1);
		}
	}

	// The other half is whichever large drawbridge sits where this one's mirror would go.
	private DrawbridgeMirror GetPartner()
	{
		if (m_partner && m_partner.isActiveAndEnabled)
		{
			return m_partner;
		}
		m_partner = null;
		if (!TryGetMirror(gameObject, out Vector3 position, out Quaternion rotation))
		{
			return null;
		}
		foreach (DrawbridgeMirror other in All)
		{
			if (other != this && other.m_door
				&& (other.transform.position - position).sqrMagnitude < 0.04f
				&& Quaternion.Angle(other.transform.rotation, rotation) < 2f)
			{
				m_partner = other;
				break;
			}
		}
		return m_partner;
	}

	private static int GetState(DrawbridgeMirror half)
	{
		ZDO zdo = half.m_nview ? half.m_nview.GetZDO() : null;
		return zdo != null ? zdo.GetInt(ZDOVars.s_state) : 0;
	}

	// Runs on the owner of the half that was used: send the other half to the same state.
	private void SyncPartner()
	{
		DrawbridgeMirror partner = GetPartner();
		if (!partner || !partner.m_nview || !partner.m_nview.IsValid())
		{
			return;
		}
		int state = GetState(this);
		if (GetState(partner) == state)
		{
			return;
		}
		// UseDoor closes an open door whatever the direction, and opens a closed one towards "forward".
		partner.m_nview.InvokeRPC("UseDoor", state == 1);
	}

	[HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
	private static class TryPlacePiecePatch
	{
		private static bool Prefix(Player __instance, Piece piece, ref bool __result)
		{
			if (s_placingMirror || !piece || !piece.GetComponent<DrawbridgeMirror>()
				|| __instance.m_placementStatus != Player.PlacementStatus.Valid)
			{
				return true;
			}
			if (CanPlaceBoth(__instance, piece))
			{
				return true;
			}
			__result = false;
			return false;
		}
	}

	[HarmonyPatch(typeof(Door), nameof(Door.RPC_UseDoor))]
	private static class UseDoorPatch
	{
		private static void Postfix(Door __instance)
		{
			DrawbridgeMirror half = __instance.GetComponent<DrawbridgeMirror>();
			if (half && half.m_nview && half.m_nview.IsOwner())
			{
				half.SyncPartner();
			}
		}
	}
}
