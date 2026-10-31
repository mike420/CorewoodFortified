using System.Collections.Generic;
using UnityEngine;

namespace CorewoodFortified;

// Ties a docked ship to its capstan: a rope from each of the capstan's attach points, meeting at one point on the front
// of the ship's hull. They are drawn like the zipline ropes, from the same rope material and to the same thickness.
//
// Every game draws them from what the ship keeps in its ZDO (see ShipDocking), so everyone sees a docked ship tied up,
// and they follow the ship as it rides the waves
internal sealed class MooringRopes : MonoBehaviour
{
	private const int Rings = 20;

	// How much a rope sags in the middle, as a share of its length
	private const float Sag = 0.04f;

	// How far in from the very front of the hull the ropes are tied, so they meet on it rather than at its edge
	private const float TieInset = 0.3f;

	private ZNetView m_nview;

	private Rigidbody m_body;

	private readonly List<RopeTube> m_ropes = new List<RopeTube>();

	private readonly Vector3[] m_points = new Vector3[Rings];

	private DockCapstan m_capstan;

	// Where the ropes meet, in the ship's space
	private Vector3 m_tie;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_body = GetComponent<Rigidbody>();
	}

	private void LateUpdate()
	{
		DockCapstan capstan = MooredTo();
		if (!capstan)
		{
			Hide();
			return;
		}
		if (capstan != m_capstan)
		{
			m_capstan = capstan;
			m_tie = transform.InverseTransformPoint(FindTiePoint());
		}
		Vector3 tie = transform.TransformPoint(m_tie);
		List<Transform> attachPoints = capstan.AttachPoints;
		while (m_ropes.Count < attachPoints.Count)
		{
			m_ropes.Add(new RopeTube("mm_mooring_rope", transform));
		}
		for (int i = 0; i < m_ropes.Count; i++)
		{
			if (i >= attachPoints.Count || !attachPoints[i])
			{
				m_ropes[i].Visible = false;
				continue;
			}
			RopePath path = new RopePath(attachPoints[i].position, tie, Sag);
			for (int j = 0; j < Rings; j++)
			{
				m_points[j] = path.PointT((float)j / (Rings - 1));
			}
			m_ropes[i].Build(m_points, MainConfig.ziplineRopeWidth.Value);
		}
	}

	private void OnDestroy()
	{
		foreach (RopeTube rope in m_ropes)
		{
			rope.Destroy();
		}
		m_ropes.Clear();
	}

	// The capstan the ship lies tied to, once it is in its place
	private DockCapstan MooredTo()
	{
		if (!m_nview || !m_nview.IsValid() || !MainConfig.enableAutoDocking.Value)
		{
			return null;
		}
		ZDO zdo = m_nview.GetZDO();
		if (!zdo.GetBool(ShipDocking.s_mooredKey))
		{
			return null;
		}
		return ShipDocking.SavedCapstan(zdo);
	}

	private void Hide()
	{
		m_capstan = null;
		foreach (RopeTube rope in m_ropes)
		{
			rope.Visible = false;
		}
	}

	// The top of the hull at the very front of the ship
	private Vector3 FindTiePoint()
	{
		Collider[] colliders = GetComponentsInChildren<Collider>();
		Vector3 middle = m_body ? m_body.worldCenterOfMass : transform.position;
		Vector3 far = middle + Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized * 1000f;
		Vector3 front = middle;
		float nearest = float.MaxValue;
		foreach (Collider collider in colliders)
		{
			if (!collider || collider.isTrigger || !collider.enabled)
			{
				continue;
			}
			Vector3 point = collider is MeshCollider mesh && !mesh.convex ? collider.bounds.ClosestPoint(far) : collider.ClosestPoint(far);
			float distance = Vector3.ProjectOnPlane(far - point, Vector3.up).sqrMagnitude;
			if (distance < nearest)
			{
				nearest = distance;
				front = point;
			}
		}
		// Back a little from the very front, then down onto the top of the hull there
		Vector3 inset = front + (Vector3.ProjectOnPlane(middle - front, Vector3.up).normalized * TieInset);
		Vector3 above = new Vector3(inset.x, middle.y + 20f, inset.z);
		float top = float.MinValue;
		foreach (RaycastHit hit in Physics.RaycastAll(above, Vector3.down, 40f, ~0, QueryTriggerInteraction.Ignore))
		{
			if (hit.collider.transform.IsChildOf(transform) && hit.point.y > top)
			{
				top = hit.point.y;
			}
		}
		inset.y = top > float.MinValue ? top : middle.y + 1f;
		return inset;
	}
}
