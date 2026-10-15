using UnityEngine;
using UnityEngine.Rendering;

namespace CorewoodFortified;

// A rope drawn as a round tube through a list of points, so the rope material's strands run along it and its
// normal map catches the light. The rope texture's strands run up the image, so V follows the rope
internal sealed class RopeTube
{
	private const int Sides = 8;

	// How much of the texture's width wraps once around the rope
	private const float WrapU = 0.5f;

	private readonly GameObject m_object;

	private readonly Mesh m_mesh;

	private Vector3[] m_vertices;

	private Vector3[] m_normals;

	private Vector2[] m_uvs;

	private int m_rings;

	public RopeTube(string name, Transform parent)
	{
		m_object = new GameObject(name);
		m_object.transform.SetParent(parent, worldPositionStays: false);
		m_mesh = new Mesh { name = name };
		m_mesh.MarkDynamic();
		m_object.AddComponent<MeshFilter>().sharedMesh = m_mesh;
		MeshRenderer renderer = m_object.AddComponent<MeshRenderer>();
		renderer.sharedMaterial = ZiplineVisuals.RopeMaterial;
		renderer.shadowCastingMode = ShadowCastingMode.On;
		renderer.receiveShadows = true;
	}

	public bool Visible
	{
		get => m_object.activeSelf;
		set
		{
			if (m_object.activeSelf != value)
			{
				m_object.SetActive(value);
			}
		}
	}

	// points are in world space; thickness is the rope's diameter
	public void Build(Vector3[] points, float thickness)
	{
		int rings = points.Length;
		if (rings < 2)
		{
			Visible = false;
			return;
		}
		if (rings != m_rings)
		{
			Allocate(rings);
		}
		Transform space = m_object.transform;
		float radius = Mathf.Max(0.001f, thickness * 0.5f);
		// Texture pixels stay square: one wrap of WrapU of the texture's width is the rope's circumference
		float vPerMetre = WrapU / (2f * Mathf.PI * radius);
		float along = 0f;
		Vector3 side = Vector3.zero;
		for (int i = 0; i < rings; i++)
		{
			Vector3 tangent = (points[Mathf.Min(i + 1, rings - 1)] - points[Mathf.Max(i - 1, 0)]).normalized;
			if (i > 0)
			{
				along += Vector3.Distance(points[i - 1], points[i]);
			}
			// Each ring's frame follows on from the last so the strands do not twist along the rope
			side = i == 0 ? FirstSide(tangent) : Vector3.ProjectOnPlane(side, tangent).normalized;
			if (side.sqrMagnitude < 0.5f)
			{
				side = FirstSide(tangent);
			}
			Vector3 up = Vector3.Cross(tangent, side);
			Vector3 centre = space.InverseTransformPoint(points[i]);
			for (int j = 0; j <= Sides; j++)
			{
				float angle = (float)j / Sides * 2f * Mathf.PI;
				Vector3 normal = space.InverseTransformDirection(side * Mathf.Cos(angle) + up * Mathf.Sin(angle));
				int index = i * (Sides + 1) + j;
				m_vertices[index] = centre + normal * radius;
				m_normals[index] = normal;
				m_uvs[index] = new Vector2((float)j / Sides * WrapU, along * vPerMetre);
			}
		}
		m_mesh.vertices = m_vertices;
		m_mesh.normals = m_normals;
		m_mesh.uv = m_uvs;
		m_mesh.RecalculateBounds();
		m_mesh.RecalculateTangents();
		Visible = true;
	}

	public void Destroy()
	{
		if (m_object != null)
		{
			Object.Destroy(m_object);
		}
		if (m_mesh != null)
		{
			Object.Destroy(m_mesh);
		}
	}

	private void Allocate(int rings)
	{
		m_rings = rings;
		int count = rings * (Sides + 1);
		m_vertices = new Vector3[count];
		m_normals = new Vector3[count];
		m_uvs = new Vector2[count];
		int[] triangles = new int[(rings - 1) * Sides * 6];
		int t = 0;
		for (int i = 0; i < rings - 1; i++)
		{
			for (int j = 0; j < Sides; j++)
			{
				int a = i * (Sides + 1) + j;
				int b = a + Sides + 1;
				triangles[t++] = a;
				triangles[t++] = a + 1;
				triangles[t++] = b;
				triangles[t++] = b;
				triangles[t++] = a + 1;
				triangles[t++] = b + 1;
			}
		}
		m_mesh.Clear();
		m_mesh.vertices = m_vertices;
		m_mesh.triangles = triangles;
	}

	private static Vector3 FirstSide(Vector3 tangent)
	{
		Vector3 side = Vector3.Cross(Vector3.up, tangent);
		if (side.sqrMagnitude < 0.0001f)
		{
			side = Vector3.Cross(Vector3.right, tangent);
		}
		return side.normalized;
	}
}
