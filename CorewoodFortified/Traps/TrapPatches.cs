using System.Collections.Generic;
using HarmonyLib;

namespace CorewoodFortified;

internal static class TrapPatches
{
	// Hides the traps and the trap controller from the build menu while traps are turned off; placed ones stay
	// but are switched off
	[HarmonyPatch(typeof(PieceTable), nameof(PieceTable.UpdateAvailable))]
	private static class PieceTableUpdateAvailablePatch
	{
		private static void Postfix(PieceTable __instance)
		{
			if (MainConfig.enableTraps.Value)
			{
				return;
			}
			__instance.m_availablePieces.RemoveWhere(IsTrapPiece);
			__instance.m_enabledPieces.RemoveWhere(IsTrapPiece);
			foreach (List<Piece> category in __instance.m_availablePiecesByCategory)
			{
				category.RemoveAll(IsTrapPiece);
			}
		}
	}

	private static bool IsTrapPiece(Piece piece)
	{
		return piece && (piece.GetComponent<TrapEnabler>() || piece.GetComponent<TrapController>());
	}
}
