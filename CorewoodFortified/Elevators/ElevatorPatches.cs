using System.Collections.Generic;
using HarmonyLib;

namespace CorewoodFortified;

internal static class ElevatorPatches
{
	private static readonly HashSet<string> s_elevatorPrefabs = new HashSet<string> { "mm_corewood_fortified_wall_elevator", "mm_corewood_fortified_corner_tower" };

	// Hides the elevators from the build menu while they are turned off; placed ones stay but do not move
	[HarmonyPatch(typeof(PieceTable), nameof(PieceTable.UpdateAvailable))]
	private static class PieceTableUpdateAvailablePatch
	{
		private static void Postfix(PieceTable __instance)
		{
			if (MainConfig.enableElevators.Value)
			{
				return;
			}
			__instance.m_availablePieces.RemoveWhere(IsElevator);
			__instance.m_enabledPieces.RemoveWhere(IsElevator);
			foreach (List<Piece> category in __instance.m_availablePiecesByCategory)
			{
				category.RemoveAll(IsElevator);
			}
		}
	}

	private static bool IsElevator(Piece piece)
	{
		return piece != null && s_elevatorPrefabs.Contains(piece.gameObject.name);
	}
}
