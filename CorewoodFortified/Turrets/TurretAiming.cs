using HarmonyLib;
using UnityEngine;

namespace CorewoodFortified;

// Predictive aiming for the fortified ballista. Vanilla leads a moving target by its speed times a guessed flight
// time, but works that time out from the wrong distance (it drops the depth axis) and doubles the lead. This aims
// where the target will be when the bolt gets there, and higher by as much as the bolt drops on the way.
// Only the aiming changes: sweeping, turning and the no-ammo droop are the same as vanilla
internal static class TurretAiming
{
	// Rounds of working out the flight time. Each one brings the aim closer to where the target will be
	private const int InterceptPasses = 3;

	// The longest flight time aimed for, so a fast target far away does not pull the aim far off
	private const float MaxLeadTime = 2f;

	[HarmonyPatch(typeof(Turret), "UpdateTurretRotation")]
	private static class Turret_UpdateTurretRotation
	{
		private static bool Prefix(Turret __instance)
		{
			if (!MainConfig.turretPredictiveAim.Value || !__instance.GetComponent<TurretSettings>())
			{
				return true;
			}
			UpdateRotation(__instance);
			return false;
		}
	}

	// Turret.UpdateTurretRotation, with only the aim at a target changed
	private static void UpdateRotation(Turret turret)
	{
		if (turret.IsCoolingDown())
		{
			return;
		}
		float dt = Time.fixedDeltaTime;
		bool haveTarget = turret.m_target && turret.HasAmmo();
		Vector3 forward;
		if (haveTarget)
		{
			if (turret.m_lastAmmo == null)
			{
				turret.m_lastAmmo = turret.GetAmmoItem();
			}
			if (turret.m_lastAmmo == null)
			{
				ZLog.LogWarning("Turret had invalid ammo, resetting ammo");
				turret.m_nview.GetZDO().Set(ZDOVars.s_ammo, 0);
				return;
			}
			Transform body = turret.m_turretBody.transform;
			Transform eye = turret.m_eye.transform;
			// The bolt flies along the eye's line, which sits above the body's pivot. Aiming from the pivot lifted by
			// that offset lines the bolt up with the target rather than the pivot
			Vector3 origin = body.position + Vector3.ProjectOnPlane(eye.position - body.position, eye.forward);
			forward = LeadTarget(turret.m_target, turret.m_lastAmmo, eye.position) - origin;
		}
		else if (!turret.HasAmmo())
		{
			forward = turret.transform.forward + new Vector3(0f, -0.3f, 0f);
		}
		else
		{
			turret.m_scan += dt;
			if (turret.m_scan > turret.m_noTargetScanRate * 2f)
			{
				turret.m_scan = 0f;
			}
			forward = Quaternion.Euler(0f, turret.transform.rotation.eulerAngles.y + (turret.m_scan - turret.m_noTargetScanRate > 0f ? 1f : -1f) * turret.m_horizontalAngle, 0f) * Vector3.forward;
		}
		forward.Normalize();
		Quaternion look = Quaternion.LookRotation(forward, Vector3.up);
		Vector3 euler = look.eulerAngles;
		float baseYaw = turret.transform.rotation.eulerAngles.y;
		euler.y -= baseYaw;
		if (turret.m_horizontalAngle >= 0f)
		{
			float yaw = euler.y;
			if (yaw > 180f)
			{
				yaw -= 360f;
			}
			else if (yaw < -180f)
			{
				yaw += 360f;
			}
			if (yaw > turret.m_horizontalAngle)
			{
				euler = new Vector3(euler.x, turret.m_horizontalAngle + baseYaw, euler.z);
				look.eulerAngles = euler;
			}
			else if (yaw < -turret.m_horizontalAngle)
			{
				euler = new Vector3(euler.x, -turret.m_horizontalAngle + baseYaw, euler.z);
				look.eulerAngles = euler;
			}
		}
		Quaternion rotation = Utils.RotateTorwardsSmooth(turret.m_turretBody.transform.rotation, look, turret.m_lastRotation, turret.m_turnRate * dt, turret.m_lookAcceleration, turret.m_lookDeacceleration, turret.m_lookMinDegreesDelta);
		turret.m_lastRotation = turret.m_turretBody.transform.rotation;
		turret.m_turretBody.transform.rotation = turret.m_baseBodyRotation * rotation;
		Quaternion bodyRotation = turret.m_turretBody.transform.rotation;
		turret.m_turretNeck.transform.rotation = turret.m_baseNeckRotation * Quaternion.Euler(0f, bodyRotation.eulerAngles.y, bodyRotation.eulerAngles.z);
		turret.m_aimDiffToTarget = haveTarget ? Quaternion.Dot(rotation, look) : -1f;
	}

	// Where to aim so a bolt fired from the eye meets the target
	private static Vector3 LeadTarget(Character target, ItemDrop.ItemData ammo, Vector3 eye)
	{
		Vector3 point = target.GetCenterPoint();
		float speed = ammo.m_shared.m_attack.m_projectileVel;
		if (speed <= 0f)
		{
			return point;
		}
		Vector3 velocity = target.GetVelocity();
		// Walking over bumps and steps makes a target bob up and down, which would only shake the aim
		if (target.IsOnGround())
		{
			velocity.y = 0f;
		}
		GameObject projectilePrefab = ammo.m_shared.m_attack.m_attackProjectile;
		Projectile projectile = projectilePrefab ? projectilePrefab.GetComponent<Projectile>() : null;
		float gravity = projectile ? projectile.m_gravity : 0f;
		Vector3 aim = point;
		float time = 0f;
		for (int i = 0; i < InterceptPasses; i++)
		{
			time = Mathf.Min(Vector3.Distance(eye, aim) / speed, MaxLeadTime);
			aim = point + velocity * time;
		}
		// The bolt falls as it flies, so aim as much higher as it will have fallen
		aim.y += 0.5f * gravity * time * time;
		return aim;
	}
}
