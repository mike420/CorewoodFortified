using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CorewoodFortified;

internal sealed class ParentPlatform : MonoBehaviour
{
	private class Rider
	{
		public int Touching;

		public Transform OriginalParent;
	}

	private Piece m_piece;

	// Riders are parented to this rather than the platform itself, so a platform scaled unevenly in the prefab
	// does not stretch them
	private Transform m_anchor;

	[System.NonSerialized]
	private readonly Dictionary<Transform, Rider> m_riders = new Dictionary<Transform, Rider>();

	private readonly List<Transform> m_gone = new List<Transform>();

	private static readonly HashSet<ParentPlatform> s_platforms = new HashSet<ParentPlatform>();

	private void Start()
	{
		m_piece = base.gameObject.GetComponentInParent<Piece>();
	}

	private void OnEnable()
	{
		s_platforms.Add(this);
	}

	private void OnDisable()
	{
		s_platforms.Remove(this);
	}

	private void OnCollisionEnter(Collision other)
	{
		Transform rider = other.transform;
		if (!m_riders.TryGetValue(rider, out Rider state))
		{
			state = new Rider { OriginalParent = rider.parent };
			m_riders[rider] = state;
			rider.SetParent(GetAnchor());
			if (other.rigidbody != null)
			{
				other.rigidbody.interpolation = RigidbodyInterpolation.None;
			}
		}
		state.Touching++;
		UpdateRemovable();
	}

	private void OnCollisionExit(Collision other)
	{
		Transform rider = other.transform;
		if (!m_riders.TryGetValue(rider, out Rider state))
		{
			return;
		}
		state.Touching--;
		if (state.Touching <= 0)
		{
			m_riders.Remove(rider);
			Release(rider, state);
		}
		UpdateRemovable();
	}

	private void FixedUpdate()
	{
		if (m_riders.Count == 0)
		{
			return;
		}
		m_gone.Clear();
		foreach (Transform rider in m_riders.Keys)
		{
			if (rider == null || !rider.gameObject.activeInHierarchy)
			{
				m_gone.Add(rider);
			}
		}
		if (m_gone.Count == 0)
		{
			return;
		}
		foreach (Transform rider in m_gone)
		{
			if (m_riders.TryGetValue(rider, out Rider state))
			{
				m_riders.Remove(rider);
				Release(rider, state);
			}
		}
		UpdateRemovable();
	}

	public void DetachAll()
	{
		foreach (KeyValuePair<Transform, Rider> rider in m_riders)
		{
			Release(rider.Key, rider.Value);
		}
		m_riders.Clear();
		UpdateRemovable();
	}

	private void Release(Transform rider, Rider state)
	{
		if (rider == null || rider.parent != m_anchor)
		{
			return;
		}
		rider.SetParent(state.OriginalParent != null ? state.OriginalParent : null);
		Rigidbody body = rider.GetComponent<Rigidbody>();
		if (body != null)
		{
			body.interpolation = RigidbodyInterpolation.Interpolate;
		}
	}

	// Undoes the platform's world scale, leaving the anchor with only its position and rotation
	private Transform GetAnchor()
	{
		if (!m_anchor)
		{
			m_anchor = new GameObject("riders").transform;
			m_anchor.SetParent(base.transform, worldPositionStays: false);
		}
		Vector3 scale = base.transform.lossyScale;
		m_anchor.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
		return m_anchor;
	}

	private void UpdateRemovable()
	{
		if (m_piece != null)
		{
			m_piece.m_canBeRemoved = m_riders.Count == 0;
		}
	}

	[HarmonyPatch(typeof(ZNetView), nameof(ZNetView.ResetZDO))]
	private static class ZNetView_ResetZDO_Patch
	{
		private static void Prefix(ZNetView __instance)
		{
			if (s_platforms.Count == 0 || !__instance)
			{
				return;
			}
			Transform root = __instance.transform;
			foreach (ParentPlatform platform in s_platforms)
			{
				if (platform && platform.m_riders.Count > 0 && platform.transform.IsChildOf(root))
				{
					platform.DetachAll();
				}
			}
		}
	}
}
