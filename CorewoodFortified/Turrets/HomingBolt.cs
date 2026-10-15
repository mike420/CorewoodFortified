using HarmonyLib;
using UnityEngine;

namespace CorewoodFortified;

// Steers a bolt fired by the fortified ballista towards the turret's target. The bolt keeps its speed and only turns,
// at most TurnRate degrees a second, so a dodging target can still be missed
internal sealed class HomingBolt : MonoBehaviour
{
	// How fast the bolt can turn towards its target, in degrees per second
	private const float TurnRate = 65f;

	// The bolt homes only while its target is nearer than this, so it flies straight for most of a long shot
	private const float HomingRange = 25f;

	// Closer than this the bolt is about to hit, so it is left to fly straight in
	private const float MinDistance = 0.4f;

	private Projectile m_projectile;

	private Character m_target;

	public void Init(Projectile projectile, Character target)
	{
		m_projectile = projectile;
		m_target = target;
	}

	private void FixedUpdate()
	{
		if (!m_projectile || !m_target || m_target.IsDead())
		{
			return;
		}
		// The bolt is moved by whoever owns it, and it is only steered there
		if (m_projectile.m_nview && m_projectile.m_nview.IsValid() && !m_projectile.m_nview.IsOwner())
		{
			return;
		}
		Vector3 velocity = m_projectile.m_vel;
		float speed = velocity.magnitude;
		if (speed < 1f)
		{
			return;
		}
		Vector3 toTarget = m_target.GetCenterPoint() - transform.position;
		float distance = toTarget.magnitude;
		if (distance < MinDistance || distance > HomingRange)
		{
			return;
		}
		// Once the target is behind the bolt it has missed, and turning back would send it circling round
		if (Vector3.Dot(velocity, toTarget) <= 0f)
		{
			enabled = false;
			return;
		}
		m_projectile.m_vel = Vector3.RotateTowards(velocity, toTarget / distance * speed, TurnRate * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f);
	}

	[HarmonyPatch(typeof(Turret), nameof(Turret.ShootProjectile))]
	private static class Turret_ShootProjectile
	{
		private static void Postfix(Turret __instance)
		{
			if (!MainConfig.turretHomingBolts.Value || !__instance.GetComponent<TurretSettings>() || !__instance.m_lastProjectile || !__instance.m_target)
			{
				return;
			}
			Projectile projectile = __instance.m_lastProjectile.GetComponent<Projectile>();
			if (projectile)
			{
				__instance.m_lastProjectile.AddComponent<HomingBolt>().Init(projectile, __instance.m_target);
			}
		}
	}
}
