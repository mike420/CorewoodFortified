using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace CorewoodFortified;

// Applies the Fortified Ballista config to the turret prefab and to every placed one. Placed turrets are updated
// whenever a setting changes, which is also when the server's synced values arrive
internal sealed class TurretSettings : MonoBehaviour
{
	private static readonly List<Turret> s_turrets = new List<Turret>();

	private static GameObject s_prefab;

	// How quickly Fast Turning gets the turret up to speed and slows it down again, and the smallest turn it makes
	private const float FastLookAcceleration = 8f;

	private const float FastLookDeacceleration = 8f;

	private const float FastLookMinDegreesDelta = 0.01f;

	// The prefab's own values, used when Fast Turning is off
	private static float s_lookAcceleration;

	private static float s_lookDeacceleration;

	private static float s_lookMinDegreesDelta;

	private Turret m_turret;

	// Called once the prefab's turret is set up from the vanilla ballista, so these values are not overwritten
	public static void Init(GameObject prefab)
	{
		if (!prefab.GetComponent<Turret>())
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + prefab.name + " has no Turret component, its config settings will not apply.");
			return;
		}
		if (!s_prefab)
		{
			MainConfig.turretRange.ConfigFile.SettingChanged += OnSettingChanged;
		}
		s_prefab = prefab;
		Turret turret = prefab.GetComponent<Turret>();
		s_lookAcceleration = turret.m_lookAcceleration;
		s_lookDeacceleration = turret.m_lookDeacceleration;
		s_lookMinDegreesDelta = turret.m_lookMinDegreesDelta;
		prefab.AddComponent<TurretSettings>();
		Apply(turret);
	}

	private static void OnSettingChanged(object sender, SettingChangedEventArgs e)
	{
		// Read from a bound entry, as BindConfigInOrder numbers the section ("4 - Fortified Ballista")
		if (e.ChangedSetting.Definition.Section != MainConfig.turretRange.Definition.Section)
		{
			return;
		}
		if (s_prefab)
		{
			Apply(s_prefab.GetComponent<Turret>());
		}
		foreach (Turret turret in s_turrets)
		{
			Apply(turret);
		}
	}

	private static void Apply(Turret turret)
	{
		turret.m_horizontalAngle = MainConfig.turretScanArc.Value / 2f;
		turret.m_noTargetScanRate = MainConfig.turretSweepTime.Value;
		turret.m_turnRate = MainConfig.turretTurnRate.Value;
		bool fast = MainConfig.turretFastTurning.Value;
		turret.m_lookAcceleration = fast ? FastLookAcceleration : s_lookAcceleration;
		turret.m_lookDeacceleration = fast ? FastLookDeacceleration : s_lookDeacceleration;
		turret.m_lookMinDegreesDelta = fast ? FastLookMinDegreesDelta : s_lookMinDegreesDelta;
		turret.m_viewDistance = MainConfig.turretRange.Value;
		turret.m_targetPlayers = MainConfig.turretTargetPlayers.Value;
		turret.m_targetTamed = MainConfig.turretTargetTamed.Value;
		turret.m_targetEnemies = MainConfig.turretTargetEnemies.Value;
		turret.m_maxConfigTargets = MainConfig.turretMaxTrophies.Value;
		turret.m_maxAmmo = MainConfig.turretMaxAmmo.Value;
		// The turret sizes its range circle to the range only when it wakes, so a placed one is resized here
		if (turret.m_marker)
		{
			turret.m_marker.m_radius = turret.m_viewDistance;
		}
	}

	private void Awake()
	{
		m_turret = GetComponent<Turret>();
		if (m_turret)
		{
			s_turrets.Add(m_turret);
		}
	}

	private void OnDestroy()
	{
		s_turrets.Remove(m_turret);
	}
}
