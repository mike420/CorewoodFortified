using System.Collections.Generic;
using UnityEngine;

namespace CorewoodFortified;

// The fortified entrance trap's two spiked rollers. When the trap springs they spin up on their upright axis, then
// roll in towards each other across the doorway while still spinning. When it stops they spin down and roll back to
// the sides. Each spike carries its own _enabled hit area, so it hurts wherever it is
internal sealed class EntranceTrap : MonoBehaviour
{
	public const string PrefabName = "mm_corewood_fortified_entrance_trap";

	private static readonly string[] SpikeNames = { "fortified_trap_doorway_spikes_01", "fortified_trap_doorway_spikes_02" };

	private const string PivotName = "_spikes_pivot";

	// Full speed, in degrees a second
	private const float SpinSpeed = 540f;

	private const float SpinUpTime = 0.6f;

	private const float SpinDownTime = 1f;

	private const float CloseTime = 1.2f;

	private const float OpenTime = 1.5f;

	// Space left between the spike tips when the rollers meet, in metres
	private const float Gap = 0.05f;

	// One per roller, set up on the prefab and copied to each placed trap
	public List<Transform> m_pivots = new List<Transform>();

	public List<Vector3> m_openPositions = new List<Vector3>();

	public List<Vector3> m_closedPositions = new List<Vector3>();

	private ZNetView m_nview;

	private TrapEnabler m_trap;

	// How fast the rollers spin, from 0 (still) to 1 (full speed)
	private float m_spin;

	// How far the rollers have come in, from 0 (at the sides) to 1 (met in the middle)
	private float m_closed;

	private float m_angle;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_trap = GetComponent<TrapEnabler>();
	}

	private void Update()
	{
		if (!m_trap || !m_nview || !m_nview.IsValid())
		{
			return;
		}
		float dt = Time.deltaTime;
		if (m_trap.IsRunning)
		{
			m_spin = Mathf.MoveTowards(m_spin, 1f, dt / SpinUpTime);
			// They only roll in once they are up to speed
			if (m_spin >= 1f)
			{
				m_closed = Mathf.MoveTowards(m_closed, 1f, dt / CloseTime);
			}
		}
		else if (m_spin > 0f || m_closed > 0f)
		{
			m_spin = Mathf.MoveTowards(m_spin, 0f, dt / SpinDownTime);
			m_closed = Mathf.MoveTowards(m_closed, 0f, dt / OpenTime);
		}
		else
		{
			return;
		}
		m_angle = Mathf.Repeat(m_angle + SpinSpeed * m_spin * dt, 360f);
		float travel = Mathf.SmoothStep(0f, 1f, m_closed);
		for (int i = 0; i < m_pivots.Count; i++)
		{
			if (!m_pivots[i])
			{
				continue;
			}
			// The two turn opposite ways, like a pair of rollers
			float angle = i % 2 == 0 ? m_angle : -m_angle;
			m_pivots[i].localPosition = Vector3.Lerp(m_openPositions[i], m_closedPositions[i], travel);
			m_pivots[i].localRotation = Quaternion.AngleAxis(angle, Vector3.up);
		}
	}

	// Wraps each spike in a pivot at its middle, so it spins in place, and works out how far each has to roll for
	// the two to meet. The prefab must be a copy rather than the bundle's own, as it is given new children
	public static void Setup(GameObject piece)
	{
		List<Transform> spikes = new List<Transform>();
		foreach (string name in SpikeNames)
		{
			Transform spike = FindChild(piece.transform, name);
			if (!spike || !spike.GetComponent<MeshFilter>())
			{
				Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " has no '" + name + "' mesh, its spikes will not move.");
				return;
			}
			spikes.Add(spike);
		}

		EntranceTrap trap = piece.AddComponent<EntranceTrap>();
		List<Bounds> bounds = new List<Bounds>();
		foreach (Transform spike in spikes)
		{
			Transform parent = spike.parent;
			Bounds inParent = OrganCannon.MeshBounds(parent, spike);
			Transform pivot = new GameObject(PivotName).transform;
			pivot.SetParent(parent, worldPositionStays: false);
			pivot.localPosition = inParent.center;
			pivot.localRotation = Quaternion.identity;
			pivot.localScale = Vector3.one;
			spike.SetParent(pivot, worldPositionStays: true);
			bounds.Add(inParent);
			trap.m_pivots.Add(pivot);
			trap.m_openPositions.Add(pivot.localPosition);
		}

		// Both rollers share a parent, so their positions can be compared directly. They roll level, along the line
		// between them, until the tips of their spikes nearly touch
		for (int i = 0; i < spikes.Count; i++)
		{
			Vector3 toOther = bounds[1 - i].center - bounds[i].center;
			toOther.y = 0f;
			float between = toOther.magnitude;
			float reach = Radius(bounds[0]) + Radius(bounds[1]);
			float travel = Mathf.Max(0f, (between - reach - Gap) / 2f);
			Vector3 direction = between > 0f ? toOther / between : Vector3.zero;
			trap.m_closedPositions.Add(trap.m_openPositions[i] + direction * travel);
		}
	}

	// How far a roller's spikes reach out from its upright axis
	private static float Radius(Bounds bounds)
	{
		return Mathf.Max(bounds.extents.x, bounds.extents.z);
	}

	private static Transform FindChild(Transform root, string name)
	{
		foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
		{
			if (child.name == name)
			{
				return child;
			}
		}
		return null;
	}
}
