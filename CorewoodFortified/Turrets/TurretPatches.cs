using System.Collections.Generic;
using HarmonyLib;

namespace CorewoodFortified;

internal static class TurretPatches
{
	private const string TurretPrefab = "mm_corewood_fortified_turret";

	// Hides the fortified ballista and organ cannon from the build menu while they are turned off; placed ones stay but do not turn, fire or take ammo
	[HarmonyPatch(typeof(PieceTable), nameof(PieceTable.UpdateAvailable))]
	private static class PieceTableUpdateAvailablePatch
	{
		private static void Postfix(PieceTable __instance)
		{
			if (MainConfig.enableTurrets.Value && MainConfig.enableOrganCannon.Value)
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

	// Skips targeting, turning and firing for placed ones while they are turned off
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
				__result = Localization.instance.Localize(__instance.m_name + "\n" + DisabledText(__instance));
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
			user.Message(MessageHud.MessageType.Center, DisabledText(turret));
		}
		result = false;
		return false;
	}

	private static bool IsDisabled(Turret turret)
	{
		if (turret.GetComponent<OrganCannon>())
		{
			return !MainConfig.enableOrganCannon.Value;
		}
		return !MainConfig.enableTurrets.Value && turret.GetComponent<TurretSettings>();
	}

	private static string DisabledText(Turret turret)
	{
		return turret.GetComponent<OrganCannon>() ? "$text_organ_cannons_disabled" : "$text_turrets_disabled";
	}

	private static bool IsTurret(Piece piece)
	{
		if (piece == null)
		{
			return false;
		}
		string name = piece.gameObject.name;
		return name == TurretPrefab && !MainConfig.enableTurrets.Value || name == OrganCannon.PrefabName && !MainConfig.enableOrganCannon.Value;
	}
}
