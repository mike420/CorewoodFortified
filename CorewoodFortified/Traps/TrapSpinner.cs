using UnityEngine;

namespace CorewoodFortified;

// Spins one part of a trap on its upright axis while the trap runs, such as the floor blades, and lets it run down
// and stop when the trap stops. The part turns about its own pivot
internal sealed class TrapSpinner : MonoBehaviour
{
	// Set up on the prefab and copied to each placed trap
	public Transform m_part;

	// Full speed, in degrees a second
	public float m_speed = 1080f;

	public float m_spinUpTime = 0.3f;

	public float m_spinDownTime = 0.3f;

	private ZNetView m_nview;

	private TrapEnabler m_trap;

	private Quaternion m_rest;

	// How fast it spins, from 0 (still) to 1 (full speed)
	private float m_spin;

	private float m_angle;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_trap = GetComponent<TrapEnabler>();
		if (m_part)
		{
			m_rest = m_part.localRotation;
		}
	}

	private void Update()
	{
		if (!m_part || !m_trap || !m_nview || !m_nview.IsValid())
		{
			return;
		}
		bool running = m_trap.IsRunning;
		if (!running && m_spin <= 0f)
		{
			return;
		}
		m_spin = Mathf.MoveTowards(m_spin, running ? 1f : 0f, Time.deltaTime / (running ? m_spinUpTime : m_spinDownTime));
		m_angle = Mathf.Repeat(m_angle + m_speed * m_spin * Time.deltaTime, 360f);
		// Turned about the parent's up, so it spins flat whichever way the part's own axes point
		m_part.localRotation = Quaternion.AngleAxis(m_angle, Vector3.up) * m_rest;
	}

	public static void AddTo(GameObject piece, string partName, float speed)
	{
		Transform part = null;
		foreach (Transform child in piece.GetComponentsInChildren<Transform>(includeInactive: true))
		{
			if (child.name == partName)
			{
				part = child;
				break;
			}
		}
		if (!part)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " has no '" + partName + "' child, it will not spin.");
			return;
		}
		TrapSpinner spinner = piece.AddComponent<TrapSpinner>();
		spinner.m_part = part;
		spinner.m_speed = speed;
	}
}
