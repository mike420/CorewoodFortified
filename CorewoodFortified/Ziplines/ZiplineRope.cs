using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorewoodFortified;

// A rope tied between two zipline posts. Its own networked object holds both ends and the sag it was tied with
public class ZiplineRope : MonoBehaviour
{
	internal static readonly List<ZiplineRope> All = new List<ZiplineRope>();

	internal const string KeyA = "mm_cf_zl_a";

	internal const string KeyB = "mm_cf_zl_b";

	internal const string KeySag = "mm_cf_zl_sag";

	internal const float EndTolerance = 0.4f;

	private const int LinePoints = 40;

	private ZNetView m_nview;

	private RopeTube m_tube;

	private readonly Vector3[] m_points = new Vector3[LinePoints];

	private uint m_revision = uint.MaxValue;

	private bool m_registered;

	private float m_nextCheck;

	private int m_missingStrikes;

	private float m_width;

	internal RopePath Path { get; private set; }

	internal float SagFactor
	{
		get
		{
			ZDO zdo = m_nview != null && m_nview.IsValid() ? m_nview.GetZDO() : null;
			return zdo != null ? zdo.GetFloat(KeySag, 0.02f) : MainConfig.ziplineRopeSag.Value;
		}
	}

	private void Start()
	{
		m_nview = GetComponent<ZNetView>();
		if (m_nview == null || m_nview.GetZDO() == null)
		{
			return;
		}
		All.Add(this);
		m_registered = true;
		m_tube = new RopeTube("rope", transform);
		m_tube.Visible = false;
		m_width = MainConfig.ziplineRopeWidth.Value;
		m_nextCheck = Time.time + 8f;
	}

	private void OnDestroy()
	{
		if (m_registered)
		{
			All.Remove(this);
		}
		m_tube?.Destroy();
	}

	private void Update()
	{
		try
		{
			if (m_nview == null || !m_nview.IsValid())
			{
				return;
			}
			ZDO zdo = m_nview.GetZDO();
			if (zdo.DataRevision != m_revision)
			{
				m_revision = zdo.DataRevision;
				Reload(zdo);
			}
			if (m_width != MainConfig.ziplineRopeWidth.Value)
			{
				m_width = MainConfig.ziplineRopeWidth.Value;
				Redraw();
			}
			if (Time.time >= m_nextCheck)
			{
				m_nextCheck = Time.time + 2.5f;
				if (m_nview.IsOwner())
				{
					CheckStillTied();
				}
			}
		}
		catch (Exception ex)
		{
			ZiplineMessages.LogDebug("Rope update failed: " + ex.Message);
		}
	}

	private void Reload(ZDO zdo)
	{
		Vector3 a = zdo.GetVec3(KeyA, Vector3.zero);
		Vector3 b = zdo.GetVec3(KeyB, Vector3.zero);
		if (a == Vector3.zero || b == Vector3.zero || (a - b).sqrMagnitude < 1f)
		{
			Path = null;
		}
		else
		{
			Path = new RopePath(a, b, zdo.GetFloat(KeySag, 0.02f));
		}
		Redraw();
	}

	private void Redraw()
	{
		if (m_tube == null)
		{
			return;
		}
		if (Path == null)
		{
			m_tube.Visible = false;
			return;
		}
		Path.Fill(m_points);
		m_tube.Build(m_points, m_width);
	}

	// A rope whose post is gone comes down, after a few checks so a post still loading is not mistaken for one
	private void CheckStillTied()
	{
		if (Path == null)
		{
			m_missingStrikes = 0;
		}
		else if (IsEndMissing(Path.A) || IsEndMissing(Path.B))
		{
			m_missingStrikes++;
			if (m_missingStrikes >= 3)
			{
				ZiplineMessages.LogInfo("Taking down a zipline rope whose post is gone.");
				DestroyRope();
			}
		}
		else
		{
			m_missingStrikes = 0;
		}
	}

	private static bool IsEndMissing(Vector3 point)
	{
		ZNetScene scene = ZNetScene.instance;
		if (scene == null || !scene.IsAreaReady(point))
		{
			return false;
		}
		return ZiplineAnchor.FindAt(point, EndTolerance) == null;
	}

	internal void MoveEnd(Vector3 from, Vector3 to)
	{
		if (m_nview == null || !m_nview.IsValid())
		{
			return;
		}
		if (!m_nview.IsOwner())
		{
			m_nview.ClaimOwnership();
		}
		ZDO zdo = m_nview.GetZDO();
		Vector3 a = zdo.GetVec3(KeyA, Vector3.zero);
		Vector3 b = zdo.GetVec3(KeyB, Vector3.zero);
		float tolerance = EndTolerance * EndTolerance;
		if ((a - from).sqrMagnitude <= tolerance)
		{
			a = to;
		}
		else if ((b - from).sqrMagnitude <= tolerance)
		{
			b = to;
		}
		else
		{
			return;
		}
		zdo.Set(KeyA, a);
		zdo.Set(KeyB, b);
		Vector3 middle = (a + b) * 0.5f;
		transform.position = middle;
		zdo.SetPosition(middle);
		m_missingStrikes = 0;
	}

	internal void DestroyRope()
	{
		if (m_nview == null || !m_nview.IsValid() || ZNetScene.instance == null)
		{
			return;
		}
		if (!m_nview.IsOwner())
		{
			m_nview.ClaimOwnership();
		}
		ZNetScene.instance.Destroy(gameObject);
	}

	internal static ZiplineRope Spawn(Vector3 a, Vector3 b)
	{
		if (ZNetScene.instance == null)
		{
			return null;
		}
		GameObject prefab = ZNetScene.instance.GetPrefab(ZiplineSetup.RopePrefab);
		if (prefab == null)
		{
			ZiplineMessages.LogWarning("The zipline rope prefab is not registered.");
			return null;
		}
		GameObject rope = Instantiate(prefab, (a + b) * 0.5f, Quaternion.identity);
		ZNetView nview = rope.GetComponent<ZNetView>();
		if (nview == null || nview.GetZDO() == null)
		{
			return null;
		}
		ZDO zdo = nview.GetZDO();
		zdo.Set(KeyA, a);
		zdo.Set(KeyB, b);
		zdo.Set(KeySag, MainConfig.ziplineRopeSag.Value);
		return rope.GetComponent<ZiplineRope>();
	}

	internal static bool Connects(Vector3 p, Vector3 q)
	{
		foreach (ZiplineRope rope in All)
		{
			RopePath path = rope.Path;
			if (path != null && path.HasEnd(p, EndTolerance) && path.HasEnd(q, EndTolerance))
			{
				return true;
			}
		}
		return false;
	}
}
