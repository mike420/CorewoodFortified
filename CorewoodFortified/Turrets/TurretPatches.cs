using System.Collections.Generic;
using HarmonyLib;

namespace CorewoodFortified;

internal static class TurretPatches
{
	private const string TurretPrefab = "mm_corewood_fortified_turret";

	// Hides the fortified ballista from the build menu while it is turned off; placed ones stay but do not turn, fire or take bolts
	[HarmonyPatch(typeof(PieceTable), nameof(PieceTable.UpdateAvailable))]
	private static class PieceTableUpdateAvailablePatch
	{
		private static void Postfix(PieceTable __instance)
		{
			if (MainConfig.enableTurrets.Value)
			{
				return;
			}
			__instance.m_availablePieces.RemoveWhere(IsTurret);
			__instance.m_enabledPieces.RemoveWhere(IsTurret);
			foreach (List<Piece> category in __instance.m_availablePiecesByCategory)
			{
				category.RemoveAll(IsTurret);
			}
		}
	}

	// Skips targeting, turning and firing for placed fortified ballistas while they are turned off
	[HarmonyPatch(typeof(Turret), "FixedUpdate")]
	private static class TurretFixedUpdatePatch
	{
		private static bool Prefix(Turret __instance)
		{
			return !IsDisabled(__instance);
		}
	}

	// Stops bolts and trophies being loaded by hand while turned off. Checked before the item leaves the inventory
	[HarmonyPatch(typeof(Turret), nameof(Turret.Interact))]
	private static class TurretInteractPatch
	{
		private static bool Prefix(Turret __instance, Humanoid character, bool hold, ref bool __result)
		{
			return AllowLoading(__instance, hold ? null : character, ref __result);
		}
	}

	[HarmonyPatch(typeof(Turret), nameof(Turret.UseItem))]
	private static class TurretUseItemPatch
	{
		private static bool Prefix(Turret __instance, Humanoid user, ref bool __result)
		{
			return AllowLoading(__instance, user, ref __result);
		}
	}

	[HarmonyPatch(typeof(Turret), nameof(Turret.GetHoverText))]
	private static class TurretGetHoverTextPatch
	{
		private static bool Prefix(Turret __instance, ref string __result)
		{
			if (IsDisabled(__instance))
			{
				__result = Localization.instance.Localize(__instance.m_name + "\n$text_turrets_disabled");
				return false;
			}
			return true;
		}
	}

	private static bool AllowLoading(Turret turret, Humanoid user, ref bool result)
	{
		if (!IsDisabled(turret))
		{
			return true;
		}
		if (user)
		{
			user.Message(MessageHud.MessageType.Center, "$text_turrets_disabled");
		}
		result = false;
		return false;
	}

	private static bool IsDisabled(Turret turret)
	{
		return !MainConfig.enableTurrets.Value && turret.GetComponent<TurretSettings>();
	}

	private static bool IsTurret(Piece piece)
	{
		return piece != null && piece.gameObject.name == TurretPrefab;
	}
}
