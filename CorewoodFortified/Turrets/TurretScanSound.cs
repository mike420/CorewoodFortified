using HarmonyLib;
using UnityEngine;

namespace CorewoodFortified;

// Plays a looping sound while the ballista swings from side to side looking for a target. It stops once the
// turret has a target, so it is not heard while the turret tracks and shoots
internal sealed class TurretScanSound : MonoBehaviour
{
	// The sound prefab whose clip is looped, set up when the piece is added
	public GameObject m_soundPrefab;

	// The loop's speed against the prefab's own
	public float m_pitch = 1f;

	// The loop's volume against the prefab's own, before the config setting
	public float m_volume = 1f;

	private static readonly AccessTools.FieldRef<Turret, Character> s_target = AccessTools.FieldRefAccess<Turret, Character>("m_target");

	// The turret turns in FixedUpdate, so between two of those a frame can see no turn. The sound keeps playing
	// this long after the last turn so it does not stutter, nor stop as the swing turns back
	private const float TurnHoldTime = 0.25f;

	// The smallest turn in one frame, in degrees, that counts as turning
	private const float MinTurn = 0.01f;

	private const float SoundFadeOut = 0.3f;

	private Turret m_turret;

	private AudioSource m_sound;

	private float m_soundVolume;

	private Quaternion m_lastRotation;

	private float m_lastTurned = float.NegativeInfinity;

	private void Start()
	{
		m_turret = GetComponent<Turret>();
		ZNetView nview = GetComponent<ZNetView>();
		// The build preview and turrets missing parts never turn, so they get no sound
		if (!m_turret || !m_turret.m_turretBody || !nview || !nview.IsValid())
		{
			enabled = false;
			return;
		}
		m_sound = LoopingSound.Create(m_soundPrefab, m_turret.m_turretBody.transform, "turret_scan_sound", out m_soundVolume);
		if (!m_sound)
		{
			enabled = false;
			return;
		}
		m_sound.pitch = m_pitch;
		m_lastRotation = m_turret.m_turretBody.transform.rotation;
	}

	private void Update()
	{
		Quaternion rotation = m_turret.m_turretBody.transform.rotation;
		if (Quaternion.Angle(rotation, m_lastRotation) > MinTurn)
		{
			m_lastTurned = Time.time;
		}
		m_lastRotation = rotation;
		bool scanning = s_target(m_turret) == null && Time.time - m_lastTurned < TurnHoldTime;
		LoopingSound.Update(m_sound, scanning, m_soundVolume * m_volume * MainConfig.turretSoundVolume.Value, SoundFadeOut);
	}
}
