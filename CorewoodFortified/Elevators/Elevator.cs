using System;
using UnityEngine;

namespace CorewoodFortified;

internal sealed class Elevator : MonoBehaviour
{
	public bool m_canMove = true;

	public Transform[] m_points;

	// The looping sound played by the platform while it moves, set up by ElevatorSetup
	public GameObject m_soundPrefab;

	public static readonly int m_elevatorPosition = "elevatorPosition".GetStableHashCode();

	private static readonly int s_target = "elevatorTarget".GetStableHashCode();

	private static readonly int s_startTime = "elevatorStart".GetStableHashCode();

	private static readonly int s_speed = "elevatorSpeed".GetStableHashCode();

	private static readonly int s_reverse = "elevatorReverse".GetStableHashCode();

	private static readonly int s_handleStop = "elevatorHandleStop".GetStableHashCode();

	private static readonly int s_handleTime = "elevatorHandleTime".GetStableHashCode();

	// How long the sound takes to fade out once the platform stops
	private const float SoundFadeOut = 0.3f;

	// How long after stopping before stepping onto the platform can start another trip
	private const float AutoTriggerDelay = 1f;

	private float m_stoppedTime = float.NegativeInfinity;

	private ZNetView m_nview;

	private AudioSource m_sound;

	private float m_soundVolume;

	private bool IsNetworked => m_nview != null && m_nview.IsValid();

	public bool IsOwner()
	{
		return IsNetworked && m_nview.IsOwner();
	}

	public void Start()
	{
		m_nview = base.gameObject.GetComponentInParent<ZNetView>();
		if (m_points == null || m_points.Length == 0)
		{
			return;
		}
		// The placement ghost has no ZDO, so it shows the platform at the first point
		int rest = 0;
		if (IsNetworked)
		{
			ZDO zdo = m_nview.GetZDO();
			rest = Mathf.Clamp(zdo.GetInt(m_elevatorPosition, 0), 0, m_points.Length - 1);
			CreateSound();
		}
		base.transform.position = m_points[rest].position;
	}

	public void Update()
	{
		if (m_points == null || m_points.Length == 0 || !IsNetworked || ZNet.instance == null)
		{
			return;
		}
		ZDO zdo = m_nview.GetZDO();
		ReadState(zdo, out int rest, out int target, out float traveled, out float distance);
		// Every client plays the sound from the shared trip state, so it lasts exactly as long as the trip
		UpdateSound(target >= 0 && traveled < distance);

		if (target < 0)
		{
			base.transform.position = m_points[rest].position;
			SetStopped();
			return;
		}
		if (traveled >= distance)
		{
			base.transform.position = m_points[target].position;
			SetStopped();
			if (m_nview.IsOwner())
			{
				zdo.Set(m_elevatorPosition, target);
				zdo.Set(s_target, -1);
			}
			return;
		}
		base.transform.position = Vector3.MoveTowards(m_points[rest].position, m_points[target].position, traveled);
		m_canMove = true;
	}

	private void SetStopped()
	{
		if (m_canMove)
		{
			m_stoppedTime = Time.time;
		}
		m_canMove = false;
	}

	// A rider lifts off the platform on the way down and lands on it again as it stops, which would otherwise
	// count as stepping on and send it straight back. Switches are not affected, only stepping onto the platform.
	public bool CanAutoTrigger()
	{
		return !m_canMove && Time.time - m_stoppedTime >= AutoTriggerDelay;
	}

	// The stop nearest the given height, used by a switch to call the platform to its own floor
	public int GetNearestStop(float y)
	{
		int nearest = -1;
		float best = float.MaxValue;
		for (int i = 0; m_points != null && i < m_points.Length; i++)
		{
			float distance = Mathf.Abs(m_points[i].position.y - y);
			if (distance < best)
			{
				best = distance;
				nearest = i;
			}
		}
		return nearest;
	}

	// Sends the platform to the given stop, or on to the next stop when none is given or it is already there
	public bool Activate(int stop = -1)
	{
		if (!MainConfig.enableElevators.Value || m_points == null || m_points.Length < 2 || !IsNetworked || ZNet.instance == null)
		{
			return false;
		}
		ZDO zdo = m_nview.GetZDO();
		ReadState(zdo, out int rest, out int target, out float traveled, out float distance);
		if (target >= 0)
		{
			if (traveled < distance)
			{
				return false;
			}
			rest = target;
		}

		bool reverse = zdo.GetBool(s_reverse, false);
		if (rest >= m_points.Length - 1)
		{
			reverse = true;
		}
		else if (rest <= 0)
		{
			reverse = false;
		}
		int next = reverse ? rest - 1 : rest + 1;
		if (stop >= 0 && stop < m_points.Length && stop != rest)
		{
			next = stop;
			reverse = stop < rest;
		}
		long start = ZNet.instance.GetTime().Ticks;

		m_nview.ClaimOwnership();
		zdo.Set(m_elevatorPosition, rest);
		zdo.Set(s_reverse, reverse);
		// Saved with the trip so every client moves the platform at the speed it was started with
		zdo.Set(s_speed, MainConfig.elevatorSpeed.Value);
		zdo.Set(s_target, next);
		zdo.Set(s_startTime, start);
		m_canMove = true;
		return true;
	}

	// Saves which floor's handle was pulled and when, so every client plays its pull from the same moment.
	// Called straight after Activate, which has already claimed ownership
	public void SetHandlePulled(int stop)
	{
		if (!IsOwner())
		{
			return;
		}
		ZDO zdo = m_nview.GetZDO();
		zdo.Set(s_handleStop, stop);
		zdo.Set(s_handleTime, ZNet.instance.GetTime().Ticks);
	}

	// Seconds since the handle for the given stop was last pulled, or -1 if it was not the last one pulled
	public float GetHandlePullAge(int stop)
	{
		if (!IsNetworked || ZNet.instance == null)
		{
			return -1f;
		}
		ZDO zdo = m_nview.GetZDO();
		if (zdo.GetInt(s_handleStop, -1) != stop)
		{
			return -1f;
		}
		return (float)((ZNet.instance.GetTime().Ticks - zdo.GetLong(s_handleTime, 0L)) / (double)TimeSpan.TicksPerSecond);
	}

	private void ReadState(ZDO zdo, out int rest, out int target, out float traveled, out float distance)
	{
		rest = Mathf.Clamp(zdo.GetInt(m_elevatorPosition, 0), 0, m_points.Length - 1);
		target = zdo.GetInt(s_target, -1);
		traveled = 0f;
		distance = 0f;
		if (target < 0 || target >= m_points.Length)
		{
			target = -1;
			return;
		}
		double elapsed = (ZNet.instance.GetTime().Ticks - zdo.GetLong(s_startTime, 0L)) / (double)TimeSpan.TicksPerSecond;
		traveled = (float)Math.Max(0.0, elapsed) * zdo.GetFloat(s_speed, MainConfig.elevatorSpeed.Value);
		distance = Vector3.Distance(m_points[rest].position, m_points[target].position);
	}

	// The sound is played from the platform itself rather than spawned as an effect, so it follows the platform
	// and is stopped when the trip ends instead of after the sound prefab's fixed lifetime
	private void CreateSound()
	{
		m_sound = LoopingSound.Create(m_soundPrefab, base.transform, "elevator_sound", out m_soundVolume);
	}

	private void UpdateSound(bool playing)
	{
		LoopingSound.Update(m_sound, playing, m_soundVolume * MainConfig.elevatorSoundVolume.Value, SoundFadeOut);
	}
}
