using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace CorewoodFortified;

internal static class ChainTowerPatches
{
	[HarmonyPatch(typeof(Hud), "SetupPieceInfo")]
	private static class Hud_SetupPieceInfo
	{
		private static void Postfix(Hud __instance, Piece piece)
		{
			if (!(piece == null) && ChainTowerPieces.IsTemplate(piece.gameObject) && !string.IsNullOrEmpty(ChainTower.PreviewText))
			{
				string text = (ChainTower.PreviewLinks ? "#81c784" : "#ffb74d");
				TMP_Text pieceDescription = __instance.m_pieceDescription;
				pieceDescription.text = pieceDescription.text + "\n\n<color=" + text + ">" + ChainTower.PreviewText + "</color>";
			}
		}
	}

	[HarmonyPatch(typeof(Projectile), "OnHit")]
	private static class Projectile_OnHit
	{
		private static bool Prefix(Collider collider)
		{
			if (!(collider == null))
			{
				return collider.GetComponentInParent<NetBarrier>() == null;
			}
			return true;
		}
	}

	// Drives the tower ghost's arrow and link preview from the player's ghost update, as on some players' games the
	// ghost's own Update never runs. Runs after the ghost is moved, so the arrow follows it in the same frame
	[HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
	private static class Player_UpdatePlacementGhost
	{
		private static void Postfix(Player __instance)
		{
			if (__instance != Player.m_localPlayer || __instance.m_placementGhost == null || !__instance.m_placementGhost.activeInHierarchy)
			{
				return;
			}
			ChainTower tower = __instance.m_placementGhost.GetComponent<ChainTower>();
			if (tower != null)
			{
				tower.UpdateGhost();
			}
		}
	}

	[HarmonyPatch(typeof(Player), "PieceRayTest")]
	private static class Player_PieceRayTest
	{
		private static void Postfix(Player __instance, bool __result, Vector3 point, Vector3 normal, Piece piece, ref Heightmap heightmap, Collider waterSurface)
		{
			if (__result && !(heightmap != null) && !(piece != null) && !(waterSurface != null) && !(normal.y < 0.5f) && !(((__instance.m_placementGhost != null) ? __instance.m_placementGhost.GetComponent<ChainTower>() : null) == null) && Physics.Raycast(point + normal * 0.1f, -normal, 0.3f, s_rockMask, QueryTriggerInteraction.Ignore))
			{
				heightmap = Heightmap.FindHeightmap(point);
			}
		}
	}

	// Hides the towers from the build menu while they are turned off; placed ones stay but their winches cannot be used
	[HarmonyPatch(typeof(PieceTable), nameof(PieceTable.UpdateAvailable))]
	private static class PieceTable_UpdateAvailable
	{
		private static void Postfix(PieceTable __instance)
		{
			if (MainConfig.enableChainTowers.Value)
			{
				return;
			}
			__instance.m_availablePieces.RemoveWhere(IsTower);
			__instance.m_enabledPieces.RemoveWhere(IsTower);
			foreach (List<Piece> category in __instance.m_availablePiecesByCategory)
			{
				category.RemoveAll(IsTower);
			}
		}
	}

	private static bool IsTower(Piece piece)
	{
		return piece != null && ChainTowerPieces.IsTemplate(piece.gameObject);
	}

	private static readonly int s_rockMask = LayerMask.GetMask("static_solid");
}
