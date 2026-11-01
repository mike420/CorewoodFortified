using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CorewoodFortified;

// Stretches each of a piece's posts down to the ground under it, so a dock or bridge built out over water or a gap
// stands on the ground below. Every post is a child mesh with "_support_" in its name (such as docks_straight_support_01),
// in both the new and the worn look, with a box collider round it. The post keeps its top where it is and only its
// bottom moves, and its collider grows with it, so the piece counts as standing on the ground and gets full support.
//
// Every game works the length out from the terrain itself, so nothing is saved. It is worked out again when the
// terrain under the piece is dug or raised. The placement ghost keeps its posts short, as long ones get in the way of
// placing it.
internal sealed class ExtendingSupports : MonoBehaviour
{
	private const string PostMarker = "_support_";

	// How far a post goes into the ground, so it does not show a gap where the ground slopes
	private const float SinkDepth = 0.3f;

	// A post is not stretched when the ground is further down than this, as it would be a pole hanging in deep water
	private const float MaxExtension = 100f;

	private const float RetryInterval = 0.5f;

	private static readonly AccessTools.FieldRef<Heightmap, Action> s_clearCache = AccessTools.FieldRefAccess<Heightmap, Action>("m_clearConnectedWearNTearCache");

	private sealed class Post
	{
		// The post's centre and bottom, in the piece's space
		public Vector3 Bottom;

		// Its top, in the piece's space, which the pivots scale about
		public float Top;

		// A pivot at the top of the post for each look it has, which the post's mesh hangs from
		public readonly List<Transform> Pivots = new List<Transform>();

		public BoxCollider Collider;

		public Vector3 ColliderCenter;

		public Vector3 ColliderSize;

		public float Extension;
	}

	private readonly List<Post> m_posts = new List<Post>();

	private WearNTear m_wear;

	private Heightmap m_heightmap;

	private Action m_onTerrainChanged;

	private bool m_pending = true;

	private float m_retryTime;

	private void Awake()
	{
		ZNetView nview = GetComponent<ZNetView>();
		if (!nview || nview.GetZDO() == null)
		{
			enabled = false;
			return;
		}
		m_wear = GetComponent<WearNTear>();
		FindPosts();
		m_onTerrainChanged = OnTerrainChanged;
	}

	private void Start()
	{
		m_heightmap = Heightmap.FindHeightmap(transform.position);
		if (m_heightmap)
		{
			s_clearCache(m_heightmap) = (Action)Delegate.Combine(s_clearCache(m_heightmap), m_onTerrainChanged);
		}
		m_pending = !Apply();
	}

	private void OnDestroy()
	{
		if (m_heightmap)
		{
			s_clearCache(m_heightmap) = (Action)Delegate.Remove(s_clearCache(m_heightmap), m_onTerrainChanged);
		}
	}

	// Until the terrain under a placed piece has loaded
	private void LateUpdate()
	{
		if (m_pending && Time.time >= m_retryTime)
		{
			m_retryTime = Time.time + RetryInterval;
			m_pending = !Apply();
		}
	}

	private void OnTerrainChanged()
	{
		m_pending = !Apply();
	}

	private void FindPosts()
	{
		Dictionary<string, Post> byName = new Dictionary<string, Post>();
		Matrix4x4 toPiece = transform.worldToLocalMatrix;
		foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>(includeInactive: true))
		{
			string name = filter.name;
			if (!name.Contains(PostMarker) || !filter.sharedMesh)
			{
				continue;
			}
			Transform mesh = filter.transform;
			Bounds bounds = PieceBounds.Transform(filter.sharedMesh.bounds, toPiece * mesh.localToWorldMatrix);
			if (!byName.TryGetValue(name, out Post post))
			{
				post = new Post
				{
					Bottom = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z),
					Top = bounds.max.y
				};
				byName.Add(name, post);
				m_posts.Add(post);
			}
			post.Pivots.Add(AddPivot(mesh, post.Top));
		}
		foreach (Post post in m_posts)
		{
			post.Collider = FindCollider(post, toPiece);
			if (post.Collider)
			{
				post.ColliderCenter = post.Collider.center;
				post.ColliderSize = post.Collider.size;
			}
		}
	}

	// Hangs the post's mesh from a new object at the post's top, so scaling that object stretches the post downwards
	private Transform AddPivot(Transform mesh, float top)
	{
		Transform parent = mesh.parent;
		Bounds bounds = PieceBounds.Transform(mesh.GetComponent<MeshFilter>().sharedMesh.bounds, parent.worldToLocalMatrix * mesh.localToWorldMatrix);
		Vector3 topInParent = parent.InverseTransformPoint(transform.TransformPoint(new Vector3(0f, top, 0f)));
		Transform pivot = new GameObject(mesh.name + "_pivot").transform;
		pivot.SetParent(parent, worldPositionStays: false);
		pivot.localPosition = new Vector3(bounds.center.x, topInParent.y, bounds.center.z);
		mesh.SetParent(pivot, worldPositionStays: true);
		return pivot;
	}

	// The box collider round the post: the one standing nearest it whose bottom is level with the post's
	private BoxCollider FindCollider(Post post, Matrix4x4 toPiece)
	{
		BoxCollider best = null;
		float bestDistance = 0.6f;
		foreach (BoxCollider box in GetComponentsInChildren<BoxCollider>(includeInactive: true))
		{
			Bounds bounds = PieceBounds.Transform(new Bounds(box.center, box.size), toPiece * box.transform.localToWorldMatrix);
			if (Mathf.Abs(bounds.min.y - post.Bottom.y) > 0.25f)
			{
				continue;
			}
			float distance = Vector2.Distance(new Vector2(bounds.center.x, bounds.center.z), new Vector2(post.Bottom.x, post.Bottom.z));
			if (distance < bestDistance)
			{
				bestDistance = distance;
				best = box;
			}
		}
		return best;
	}

	// Stretches every post to the ground. False when the ground under a post has not loaded yet
	private bool Apply()
	{
		if (ZoneSystem.instance == null || m_posts.Count == 0)
		{
			return m_posts.Count == 0;
		}
		bool changed = false;
		foreach (Post post in m_posts)
		{
			Vector3 bottom = transform.TransformPoint(post.Bottom);
			if (!ZoneSystem.instance.GetGroundHeight(bottom, out float ground))
			{
				return false;
			}
			float extension = bottom.y - ground + SinkDepth;
			if (extension <= SinkDepth || extension > MaxExtension)
			{
				extension = 0f;
			}
			if (Mathf.Abs(extension - post.Extension) > 0.01f)
			{
				SetExtension(post, extension);
				changed = true;
			}
		}
		if (changed && m_wear)
		{
			// The piece works out its support from its colliders as they were, so it is made to look at them again
			m_wear.m_colliders = null;
			m_wear.ClearCachedSupport();
		}
		return true;
	}

	private static void SetExtension(Post post, float extension)
	{
		post.Extension = extension;
		float length = post.Top - post.Bottom.y;
		float scale = (length + extension) / length;
		foreach (Transform pivot in post.Pivots)
		{
			pivot.localScale = new Vector3(1f, scale, 1f);
		}
		if (post.Collider)
		{
			post.Collider.size = post.ColliderSize + new Vector3(0f, extension, 0f);
			post.Collider.center = post.ColliderCenter - new Vector3(0f, extension * 0.5f, 0f);
		}
	}
}
