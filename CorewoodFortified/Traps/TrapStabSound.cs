using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace CorewoodFortified;

// Plays a sound once each time the trap strikes and once each time it pulls back. The trap's animation switches
// its damage area on for every strike and off again after, so those are the moments the first damage area under
// _enabled turns on and off. Switching the whole trap off is not a pull back, so it plays nothing
internal sealed class TrapStabSound : MonoBehaviour
{
	// Each trap's volume setting, by prefab name. Unity does not copy a ConfigEntry onto placed copies
	private static readonly Dictionary<string, ConfigEntry<float>> s_volumeConfigs = new Dictionary<string, ConfigEntry<float>>();

	// The sound prefab played on each strike, set up when the piece is added
	public GameObject m_soundPrefab;

	private GameObject m_enabledObject;

	private GameObject m_hitArea;

	private ConfigEntry<float> m_volumeConfig;

	private bool m_wasStriking;

	private void Start()
	{
		ZNetView nview = GetComponent<ZNetView>();
		m_enabledObject = TrapEnabler.FindEnabledObject(transform);
		Aoe aoe = m_enabledObject ? m_enabledObject.GetComponentInChildren<Aoe>(includeInactive: true) : null;
		// The build preview never strikes, so it gets no sound
		if (!aoe || !nview || !nview.IsValid())
		{
			enabled = false;
			return;
		}
		m_hitArea = aoe.gameObject;
		s_volumeConfigs.TryGetValue(Utils.GetPrefabName(gameObject), out m_volumeConfig);
	}

	private void Update()
	{
		bool striking = m_hitArea.activeInHierarchy;
		if (striking != m_wasStriking && (striking || m_enabledObject.activeInHierarchy))
		{
			Play();
		}
		m_wasStriking = striking;
	}

	private void Play()
	{
		float volume = m_volumeConfig != null ? m_volumeConfig.Value : 1f;
		if (volume <= 0f)
		{
			return;
		}
		GameObject sound;
		ZNetView.m_forceDisableInit = true;
		try
		{
			sound = Instantiate(m_soundPrefab, m_hitArea.transform.position, Quaternion.identity);
		}
		finally
		{
			ZNetView.m_forceDisableInit = false;
		}
		foreach (ZSFX sfx in sound.GetComponentsInChildren<ZSFX>())
		{
			sfx.SetVolumeModifier(volume);
		}
		// A sound prefab without its own timer would stay behind after every strike
		if (!sound.GetComponent<TimedDestruction>())
		{
			AudioSource source = sound.GetComponentInChildren<AudioSource>();
			Destroy(sound, source && source.clip ? source.clip.length + 0.5f : 5f);
		}
	}

	public static void AddTo(GameObject piece, string soundPrefabName, ConfigEntry<float> volumeConfig)
	{
		GameObject soundPrefab = Jotunn.Managers.PrefabManager.Instance.GetPrefab(soundPrefabName);
		if (!soundPrefab)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] Sound prefab '" + soundPrefabName + "' not found, " + piece.name + " will be silent.");
			return;
		}
		piece.AddComponent<TrapStabSound>().m_soundPrefab = soundPrefab;
		s_volumeConfigs[piece.name] = volumeConfig;
	}
}
