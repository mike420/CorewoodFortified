using System.Collections.Generic;
using UnityEngine;

namespace CorewoodFortified;

// Rolls the organ cannon's wheels as it turns on the spot. Turning about the middle between them, the wheel on one
// side rolls forwards and the other backwards, each as far as its side of the carriage moves
internal sealed class CannonWheels : MonoBehaviour
{
	public Transform m_neck;

	// A pivot at each wheel's middle, which is turned about the axle
	public List<Transform> m_pivots = new List<Transform>();

	// The axle's direction, in each pivot's space
	public List<Vector3> m_axles = new List<Vector3>();

	// Degrees each wheel rolls for every degree the cannon turns
	public List<float> m_ratios = new List<float>();

	private float[] m_angles;

	private float m_lastYaw;

	private void Awake()
	{
		m_angles = new float[m_pivots.Count];
		if (m_neck)
		{
			m_lastYaw = m_neck.localEulerAngles.y;
		}
	}

	private void LateUpdate()
	{
		if (!m_neck)
		{
			return;
		}
		float yaw = m_neck.localEulerAngles.y;
		float turned = Mathf.DeltaAngle(m_lastYaw, yaw);
		m_lastYaw = yaw;
		if (Mathf.Abs(turned) < 0.0001f)
		{
			return;
		}
		for (int i = 0; i < m_pivots.Count; i++)
		{
			if (!m_pivots[i])
			{
				continue;
			}
			m_angles[i] = Mathf.Repeat(m_angles[i] + turned * m_ratios[i], 360f);
			m_pivots[i].localRotation = Quaternion.AngleAxis(m_angles[i], m_axles[i]);
		}
	}

	// Each wheel goes under a pivot at its middle, so it turns about its axle and not the model's origin. The axle
	// runs across the cannon, and a wheel's radius is half its height
	public static void Setup(GameObject piece, Transform neck, List<Transform> wheels)
	{
		if (wheels.Count == 0)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " has no wheel meshes, its wheels will not roll.");
			return;
		}
		CannonWheels component = piece.AddComponent<CannonWheels>();
		component.m_neck = neck;
		foreach (Transform wheel in wheels)
		{
			Bounds inNeck = OrganCannon.MeshBounds(neck, wheel);
			float radius = Mathf.Max(inNeck.extents.y, inNeck.extents.z);
			if (radius < 0.01f)
			{
				continue;
			}
			Transform pivot = new GameObject("_wheel_pivot").transform;
			pivot.SetParent(wheel.parent, worldPositionStays: false);
			pivot.localPosition = OrganCannon.MeshBounds(wheel.parent, wheel).center;
			pivot.localRotation = Quaternion.identity;
			pivot.localScale = Vector3.one;
			wheel.SetParent(pivot, worldPositionStays: true);
			component.m_pivots.Add(pivot);
			component.m_axles.Add(pivot.InverseTransformDirection(neck.right).normalized);
			// Turning right moves a wheel on the left forwards. Rolling forwards turns a wheel about the right axis
			component.m_ratios.Add(-inNeck.center.x / radius);
		}
	}
}
