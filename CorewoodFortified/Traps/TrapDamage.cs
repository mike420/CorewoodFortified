using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace CorewoodFortified;

// Sets how hard a trap's hit areas hit from its damage setting. Each hit area keeps the kinds of damage the prefab
// gives it, such as fire or slash, scaled so they add up to the setting. Placed traps change with the setting too
internal sealed class TrapDamage : MonoBehaviour
{
	// Each trap's damage setting, by prefab name. Unity does not copy a ConfigEntry onto placed copies
	private static readonly Dictionary<string, ConfigEntry<float>> s_damageConfigs = new Dictionary<string, ConfigEntry<float>>();

	private ConfigEntry<float> m_damageConfig;

	private Aoe[] m_aoes;

	private void Awake()
	{
		if (!s_damageConfigs.TryGetValue(Utils.GetPrefabName(gameObject), out m_damageConfig))
		{
			return;
		}
		m_aoes = GetComponentsInChildren<Aoe>(includeInactive: true);
		ApplyDamage();
		m_damageConfig.SettingChanged += OnDamageChanged;
	}

	private void OnDestroy()
	{
		if (m_damageConfig != null)
		{
			m_damageConfig.SettingChanged -= OnDamageChanged;
		}
	}

	private void OnDamageChanged(object sender, EventArgs e)
	{
		ApplyDamage();
	}

	private void ApplyDamage()
	{
		foreach (Aoe aoe in m_aoes)
		{
			if (!aoe)
			{
				continue;
			}
			float total = aoe.m_damage.GetTotalDamage();
			if (total > 0f)
			{
				aoe.m_damage.Modify(m_damageConfig.Value / total);
			}
		}
	}

	public static void AddTo(GameObject piece, ConfigEntry<float> damageConfig)
	{
		Aoe[] aoes = piece.GetComponentsInChildren<Aoe>(includeInactive: true);
		if (aoes.Length == 0)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " has no hit area, its damage setting does nothing.");
			return;
		}
		foreach (Aoe aoe in aoes)
		{
			if (aoe.m_damage.GetTotalDamage() <= 0f)
			{
				Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + "'s hit area '" + aoe.name + "' does no damage, its damage setting cannot scale it.");
			}
		}
		piece.AddComponent<TrapDamage>();
		s_damageConfigs[piece.name] = damageConfig;
	}
}
