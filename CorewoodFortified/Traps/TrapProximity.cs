using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace CorewoodFortified;

// Sits on a trap's Trigger collider and tells the trap whenever a character is inside it. Trigger
// messages go to the collider's own object, so this cannot live on the trap's root
internal sealed class TrapProximity : MonoBehaviour
{
	// Each trap's trigger radius setting, by prefab name. Unity does not copy a ConfigEntry onto placed copies
	private static readonly Dictionary<string, ConfigEntry<float>> s_radiusConfigs = new Dictionary<string, ConfigEntry<float>>();

	private TrapEnabler m_trap;

	private SphereCollider m_sphere;

	private ConfigEntry<float> m_radiusConfig;

	private void Awake()
	{
		m_trap = GetComponentInParent<TrapEnabler>();
		m_sphere = GetComponent<SphereCollider>();
		if (m_trap && m_sphere)
		{
			s_radiusConfigs.TryGetValue(Utils.GetPrefabName(m_trap.gameObject), out m_radiusConfig);
		}
		if (m_radiusConfig != null)
		{
			ApplyRadius();
			m_radiusConfig.SettingChanged += OnRadiusChanged;
		}
	}

	private void OnDestroy()
	{
		if (m_radiusConfig != null)
		{
			m_radiusConfig.SettingChanged -= OnRadiusChanged;
		}
	}

	private void OnRadiusChanged(object sender, EventArgs e)
	{
		ApplyRadius();
	}

	// The setting is in metres, so a scaled Trigger still ends up that size in the world
	private void ApplyRadius()
	{
		Vector3 scale = transform.lossyScale;
		float largest = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
		m_sphere.radius = m_radiusConfig.Value / (largest > 0f ? largest : 1f);
	}

	private void OnTriggerEnter(Collider other)
	{
		Check(other);
	}

	private void OnTriggerStay(Collider other)
	{
		Check(other);
	}

	private void Check(Collider other)
	{
		if (!m_trap)
		{
			return;
		}
		Rigidbody body = other.attachedRigidbody;
		if (body ? body.GetComponent<Character>() : other.GetComponentInParent<Character>())
		{
			m_trap.OnCharacterNear();
		}
	}

	public static void AddTo(GameObject trigger, string prefabName, ConfigEntry<float> radiusConfig)
	{
		trigger.AddComponent<TrapProximity>();
		if (radiusConfig == null)
		{
			return;
		}
		if (!trigger.GetComponent<SphereCollider>())
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + prefabName + "'s trigger is not a sphere, its radius setting does nothing.");
		}
		s_radiusConfigs[prefabName] = radiusConfig;
	}
}
