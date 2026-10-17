using System.Collections.Generic;
using UnityEngine;

namespace CorewoodFortified;

// Draws flickering green lightning along each beam of an electric fence. The beams are found in the meshes named
// laser_beam..., or under such an object, so a remodelled fence still lines up. Each beam gets a few jagged bolts, redrawn many times a second, each
// a soft wide glow with a bright thin core. The bolts run while the fence's damage area is on, or all the time if
// it has none
internal sealed class ElectricFenceArcs : MonoBehaviour
{
	private const string BeamsName = "laser_beam";

	private const int BoltsPerBeam = 2;

	// The bolt is split in half this many times, so it has 2^n + 1 points
	private const int Subdivisions = 5;

	// How far a bolt's middle can stray from the beam, as a share of the beam's length. Each halving strays less
	private const float Jaggedness = 0.05f;
	private const float JaggedFalloff = 0.55f;

	private const float RedrawInterval = 0.05f;

	// Chance a bolt is left out of a redraw, which makes it crackle rather than hum
	private const float FlickerChance = 0.2f;

	private const float GlowWidth = 0.1f;
	private const float CoreWidth = 0.018f;

	private static readonly Color GlowColor = new Color(0.15f, 1f, 0.25f, 0.55f);
	private static readonly Color CoreColor = new Color(0.8f, 1f, 0.8f, 1f);

	// Tried in order; the first the game has is used
	private static readonly string[] ShaderNames =
	{
		"Legacy Shaders/Particles/Additive",
		"Mobile/Particles/Additive",
		"Sprites/Default"
	};

	private static Material s_material;

	private sealed class Bolt
	{
		public LineRenderer m_glow;
		public LineRenderer m_core;
		public Vector3 m_start;
		public Vector3 m_end;
		public Vector3 m_side;
		public Vector3 m_up;
		public float m_length;
	}

	private List<Bolt> m_bolts;
	private Vector3[] m_points;
	private GameObject m_hitArea;
	private float m_nextRedraw;
	private bool m_shown;

	private void Start()
	{
		ZNetView nview = GetComponent<ZNetView>();
		List<MeshFilter> filters = FindBeamMeshes(transform);
		// The build preview stays still
		if (filters.Count == 0 || !nview || !nview.IsValid())
		{
			enabled = false;
			return;
		}
		Aoe aoe = GetComponentInChildren<Aoe>(includeInactive: true);
		m_hitArea = aoe ? aoe.gameObject : null;

		Material material = GetMaterial();
		m_points = new Vector3[(1 << Subdivisions) + 1];
		m_bolts = new List<Bolt>();
		List<(Vector3, Vector3)> beams = new List<(Vector3, Vector3)>();
		foreach (MeshFilter filter in filters)
		{
			beams.AddRange(FindBeams(filter.sharedMesh, transform.worldToLocalMatrix * filter.transform.localToWorldMatrix));
		}
		foreach ((Vector3 start, Vector3 end) in beams)
		{
			Vector3 dir = (end - start).normalized;
			Vector3 side = Vector3.Cross(dir, Mathf.Abs(dir.y) < 0.9f ? Vector3.up : Vector3.forward).normalized;
			for (int i = 0; i < BoltsPerBeam; i++)
			{
				m_bolts.Add(new Bolt
				{
					m_glow = CreateLine("electric_glow", material, GlowWidth, GlowColor),
					m_core = CreateLine("electric_core", material, CoreWidth, CoreColor),
					m_start = start,
					m_end = end,
					m_side = side,
					m_up = Vector3.Cross(side, dir),
					m_length = Vector3.Distance(start, end)
				});
			}
		}
		Show(false);
	}

	private void Update()
	{
		bool show = !m_hitArea || m_hitArea.activeInHierarchy;
		if (show != m_shown)
		{
			Show(show);
		}
		if (!show || Time.time < m_nextRedraw)
		{
			return;
		}
		m_nextRedraw = Time.time + RedrawInterval;
		foreach (Bolt bolt in m_bolts)
		{
			bool visible = Random.value >= FlickerChance;
			bolt.m_glow.enabled = visible;
			bolt.m_core.enabled = visible;
			if (visible)
			{
				Redraw(bolt);
			}
		}
	}

	private void Show(bool show)
	{
		m_shown = show;
		foreach (Bolt bolt in m_bolts)
		{
			bolt.m_glow.enabled = show;
			bolt.m_core.enabled = show;
		}
	}

	// Midpoint displacement: each segment's middle is pushed sideways by a random amount that shrinks with every
	// halving, which gives the big kinks and small crackle of real lightning. The ends stay on the posts
	private void Redraw(Bolt bolt)
	{
		int last = m_points.Length - 1;
		m_points[0] = bolt.m_start;
		m_points[last] = bolt.m_end;
		float spread = bolt.m_length * Jaggedness;
		for (int step = last; step > 1; step /= 2)
		{
			for (int i = 0; i < last; i += step)
			{
				Vector2 push = Random.insideUnitCircle * spread;
				m_points[i + step / 2] = (m_points[i] + m_points[i + step]) * 0.5f + bolt.m_side * push.x + bolt.m_up * push.y;
			}
			spread *= JaggedFalloff;
		}
		bolt.m_glow.SetPositions(m_points);
		bolt.m_core.SetPositions(m_points);
	}

	private LineRenderer CreateLine(string name, Material material, float width, Color color)
	{
		GameObject go = new GameObject(name);
		go.transform.SetParent(transform, worldPositionStays: false);
		LineRenderer line = go.AddComponent<LineRenderer>();
		line.useWorldSpace = false;
		line.sharedMaterial = material;
		line.positionCount = m_points.Length;
		line.widthMultiplier = width;
		line.startColor = color;
		line.endColor = color;
		line.textureMode = LineTextureMode.Stretch;
		line.alignment = LineAlignment.View;
		line.numCapVertices = 2;
		line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		line.receiveShadows = false;
		return line;
	}

	// Every mesh named laser_beam..., or sitting under an object named so
	private static List<MeshFilter> FindBeamMeshes(Transform root)
	{
		List<MeshFilter> filters = new List<MeshFilter>();
		foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(includeInactive: true))
		{
			if (!filter.sharedMesh)
			{
				continue;
			}
			for (Transform t = filter.transform; t && t != root; t = t.parent)
			{
				if (t.name.StartsWith(BeamsName))
				{
					filters.Add(filter);
					break;
				}
			}
		}
		return filters;
	}

	// A mesh can hold one beam or several side by side, each a separate tube. The points are grouped by where they sit across the fence's long
	// side, and each group runs from its lowest to its highest point along it. A mesh the game cannot read is treated
	// as one beam through its middle
	private static List<(Vector3, Vector3)> FindBeams(Mesh mesh, Matrix4x4 toLocal)
	{
		List<(Vector3, Vector3)> beams = new List<(Vector3, Vector3)>();
		Vector3[] vertices = mesh.isReadable ? mesh.vertices : null;
		if (vertices == null || vertices.Length == 0)
		{
			Bounds bounds = mesh.bounds;
			Vector3 extents = bounds.extents;
			int longest = extents.x >= extents.y && extents.x >= extents.z ? 0 : extents.y >= extents.z ? 1 : 2;
			Vector3 offset = Vector3.zero;
			offset[longest] = extents[longest];
			beams.Add((toLocal.MultiplyPoint3x4(bounds.center - offset), toLocal.MultiplyPoint3x4(bounds.center + offset)));
			return beams;
		}

		Vector3 min = Vector3.positiveInfinity;
		Vector3 max = Vector3.negativeInfinity;
		for (int i = 0; i < vertices.Length; i++)
		{
			vertices[i] = toLocal.MultiplyPoint3x4(vertices[i]);
			min = Vector3.Min(min, vertices[i]);
			max = Vector3.Max(max, vertices[i]);
		}
		Vector3 size = max - min;
		int axis = size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;
		float groupDistance = size[axis] * 0.1f;

		List<(Vector3 across, int count, float from, float to)> groups = new List<(Vector3, int, float, float)>();
		foreach (Vector3 vertex in vertices)
		{
			Vector3 across = vertex;
			across[axis] = 0f;
			int found = -1;
			for (int g = 0; g < groups.Count; g++)
			{
				if (Vector3.Distance(groups[g].across / groups[g].count, across) < groupDistance)
				{
					found = g;
					break;
				}
			}
			if (found < 0)
			{
				groups.Add((across, 1, vertex[axis], vertex[axis]));
			}
			else
			{
				var group = groups[found];
				groups[found] = (group.across + across, group.count + 1, Mathf.Min(group.from, vertex[axis]), Mathf.Max(group.to, vertex[axis]));
			}
		}
		foreach (var group in groups)
		{
			Vector3 start = group.across / group.count;
			Vector3 end = start;
			start[axis] = group.from;
			end[axis] = group.to;
			beams.Add((start, end));
		}
		return beams;
	}

	// Shared by every fence: an additive material with a soft-edged strip texture, so the wide line glows and fades
	// out at its sides
	private static Material GetMaterial()
	{
		if (s_material)
		{
			return s_material;
		}
		Shader shader = null;
		foreach (string shaderName in ShaderNames)
		{
			shader = Shader.Find(shaderName);
			if (shader)
			{
				break;
			}
		}
		Texture2D texture = new Texture2D(4, 32, TextureFormat.RGBA32, mipChain: false)
		{
			wrapMode = TextureWrapMode.Clamp,
			name = "cwf_electric_strip"
		};
		for (int y = 0; y < texture.height; y++)
		{
			float across = (y + 0.5f) / texture.height * 2f - 1f;
			float brightness = Mathf.Exp(-across * across * 4f);
			Color color = new Color(brightness, brightness, brightness, brightness);
			for (int x = 0; x < texture.width; x++)
			{
				texture.SetPixel(x, y, color);
			}
		}
		texture.Apply();
		s_material = new Material(shader) { name = "cwf_electric", mainTexture = texture };
		if (s_material.HasProperty("_TintColor"))
		{
			s_material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
		}
		return s_material;
	}

	public static void AddTo(GameObject piece)
	{
		if (FindBeamMeshes(piece.transform).Count == 0)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " has no '" + BeamsName + "...' meshes, it will have no lightning.");
			return;
		}
		piece.AddComponent<ElectricFenceArcs>();
	}
}
