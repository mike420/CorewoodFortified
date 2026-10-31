using System.Collections.Generic;
using UnityEngine;

namespace CorewoodFortified;

// A placed dock capstan that ships can dock at (see ShipDocking). Its attach points are children named attach_point_NN
// on the dock's front edge, and a ship docks in front of them, lying along the dock
internal sealed class DockCapstan : MonoBehaviour
{
	private static readonly List<DockCapstan> s_all = new List<DockCapstan>();

	private readonly List<Transform> m_attachPoints = new List<Transform>();

	private ZNetView m_nview;

	public static List<DockCapstan> All => s_all;

	public List<Transform> AttachPoints => m_attachPoints;

	// Halfway between the attach points, on the front edge of the dock
	public Vector3 DockPoint
	{
		get
		{
			if (m_attachPoints.Count == 0)
			{
				return transform.position;
			}
			Vector3 sum = Vector3.zero;
			foreach (Transform point in m_attachPoints)
			{
				sum += point.position;
			}
			return sum / m_attachPoints.Count;
		}
	}

	// Out from the dock over the water, flat
	public Vector3 Front => Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

	// Along the dock, flat
	public Vector3 Along => Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		if (!m_nview || m_nview.GetZDO() == null)
		{
			enabled = false;
			return;
		}
		foreach (Transform child in GetComponentsInChildren<Transform>(includeInactive: true))
		{
			if (child.name.StartsWith("attach_point_"))
			{
				m_attachPoints.Add(child);
			}
		}
		s_all.Add(this);
	}

	private void OnDestroy()
	{
		s_all.Remove(this);
	}

	// The capstan standing at a place, within a little of it
	public static DockCapstan FindAt(Vector3 position)
	{
		DockCapstan best = null;
		float bestDistance = 0.5f;
		foreach (DockCapstan capstan in s_all)
		{
			if (!capstan)
			{
				continue;
			}
			float distance = Vector3.Distance(capstan.transform.position, position);
			if (distance < bestDistance)
			{
				bestDistance = distance;
				best = capstan;
			}
		}
		return best;
	}
}
