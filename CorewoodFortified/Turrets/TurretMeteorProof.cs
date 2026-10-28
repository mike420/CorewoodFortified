using HarmonyLib;
using UnityEngine;

namespace CorewoodFortified;

// Meteor showers, such as Deep North's, rain on a base for many minutes with no raid to warn of them, and a turret on
// the top of a tower takes hit after hit until it breaks. The fortified ballista and organ cannon take no damage from
// a meteor, whether it lands on them, bursts near them or leaves a blast behind
internal static class TurretMeteorProof
{
	// How many meteor hits are being worked out now. A piece is damaged during its hit, so one counted here is a
	// meteor's
	private static int s_meteorHits;

	private static bool IsMeteor(GameObject gameObject)
	{
		return gameObject.name.IndexOf("meteor", System.StringComparison.OrdinalIgnoreCase) >= 0;
	}

	// A meteor's direct hit and its burst around where it lands
	[HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
	private static class Projectile_OnHit
	{
		private static void Prefix(Projectile __instance, out bool __state)
		{
			__state = IsMeteor(__instance.gameObject);
			if (__state)
			{
				s_meteorHits++;
			}
		}

		private static void Finalizer(bool __state)
		{
			if (__state)
			{
				s_meteorHits--;
			}
		}
	}

	// A blast a meteor leaves where it lands is made during its hit, and is marked as a meteor's
	[HarmonyPatch(typeof(Aoe), "Awake")]
	private static class Aoe_Awake
	{
		private static void Postfix(Aoe __instance)
		{
			if (s_meteorHits > 0 || IsMeteor(__instance.gameObject))
			{
				__instance.gameObject.AddComponent<MeteorBlast>();
			}
		}
	}

	[HarmonyPatch(typeof(Aoe), nameof(Aoe.OnHit))]
	private static class Aoe_OnHit
	{
		private static void Prefix(Aoe __instance, out bool __state)
		{
			__state = __instance.GetComponent<MeteorBlast>();
			if (__state)
			{
				s_meteorHits++;
			}
		}

		private static void Finalizer(bool __state)
		{
			if (__state)
			{
				s_meteorHits--;
			}
		}
	}

	[HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Damage))]
	private static class WearNTear_Damage
	{
		private static bool Prefix(WearNTear __instance)
		{
			return s_meteorHits <= 0 || !__instance.GetComponent<MovableTurret>();
		}
	}

	private sealed class MeteorBlast : MonoBehaviour
	{
	}
}
