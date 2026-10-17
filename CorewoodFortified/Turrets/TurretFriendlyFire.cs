using HarmonyLib;
using UnityEngine;

namespace CorewoodFortified;

// Vanilla ballista bolts have no owner, so every building piece is a valid target for them and a bolt that misses
// damages whatever it lands in: walls, towers, braziers and other ballistas. Bolts from the fortified ballista are
// marked when fired, and while Spare Buildings is on a marked bolt still stops on a piece but does it no damage
internal sealed class FortifiedBolt : MonoBehaviour
{
	[HarmonyPatch(typeof(Turret), nameof(Turret.ShootProjectile))]
	private static class Turret_ShootProjectile
	{
		private static void Postfix(Turret __instance)
		{
			if (__instance.m_lastProjectile && __instance.GetComponent<TurretSettings>())
			{
				__instance.m_lastProjectile.AddComponent<FortifiedBolt>();
			}
		}
	}

	[HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
	private static class Projectile_OnHit
	{
		private static void Prefix(Projectile __instance, Collider collider, out HitData.DamageTypes? __state)
		{
			__state = null;
			if (!collider || !SparesBuildings(__instance))
			{
				return;
			}
			GameObject hitObject = Projectile.FindHitObject(collider);
			if (hitObject && hitObject.GetComponent<WearNTear>())
			{
				__state = __instance.m_damage;
				__instance.m_damage = default;
			}
		}

		// Organ cannon balls follow the cannon's own setting
		private static bool SparesBuildings(Projectile projectile)
		{
			if (projectile.GetComponent<FortifiedBolt>())
			{
				return MainConfig.turretSpareBuildings.Value;
			}
			return projectile.GetComponent<CannonBallShot>() && MainConfig.cannonSpareBuildings.Value;
		}

		private static void Postfix(Projectile __instance, HitData.DamageTypes? __state)
		{
			if (__state.HasValue)
			{
				__instance.m_damage = __state.Value;
			}
		}
	}
}
