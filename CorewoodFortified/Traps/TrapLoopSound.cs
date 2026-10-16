using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace CorewoodFortified;

// Plays a looping sound while the trap's _enabled child is on, and fades it out when it turns off. The sound lives
// on the trap itself rather than under _enabled, so it can fade out after the trap has switched off
internal sealed class TrapLoopSound : MonoBehaviour
{
	private const float SoundFadeOut = 0.3f;

	// Each trap's volume setting, by prefab name. Unity does not copy a ConfigEntry onto placed copies
	private static readonly Dictionary<string, ConfigEntry<float>> s_volumeConfigs = new Dictionary<string, ConfigEntry<float>>();

	// The sound prefab whose clip is looped, set up when the piece is added
	public GameObject m_soundPrefab;

	private GameObject m_enabledObject;

	private AudioSource m_sound;

	private float m_soundVolume;

	private ConfigEntry<float> m_volumeConfig;

	private void Start()
	{
		ZNetView nview = GetComponent<ZNetView>();
		m_enabledObject = TrapEnabler.FindEnabledObject(transform);
		// The build preview never switches on, so it gets no sound
		if (!m_enabledObject || !nview || !nview.IsValid())
		{
			enabled = false;
			return;
		}
		s_volumeConfigs.TryGetValue(Utils.GetPrefabName(gameObject), out m_volumeConfig);
		m_sound = LoopingSound.Create(m_soundPrefab, transform, "trap_loop_sound", out m_soundVolume);
		if (!m_sound)
		{
			enabled = false;
		}
	}

	private void Update()
	{
		LoopingSound.Update(m_sound, m_enabledObject.activeInHierarchy, m_soundVolume * (m_volumeConfig != null ? m_volumeConfig.Value : 1f), SoundFadeOut);
	}

	public static void AddTo(GameObject piece, string soundPrefabName, ConfigEntry<float> volumeConfig)
	{
		GameObject soundPrefab = Jotunn.Managers.PrefabManager.Instance.GetPrefab(soundPrefabName);
		if (!soundPrefab)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] Sound prefab '" + soundPrefabName + "' not found, " + piece.name + " will be silent.");
			return;
		}
		piece.AddComponent<TrapLoopSound>().m_soundPrefab = soundPrefab;
		s_volumeConfigs[piece.name] = volumeConfig;
	}
}
