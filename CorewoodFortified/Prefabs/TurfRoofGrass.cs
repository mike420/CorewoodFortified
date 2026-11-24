using System.Collections.Generic;
using UnityEngine;

namespace CorewoodFortified;

// Grass on a turf roof. The game only grows grass on the terrain, so the roof draws its own, the way the game does:
// the grass of the biome the building stands in (Meadows grass where that biome has none), as thick as the player's
// vegetation setting makes it, but evenly all over rather than in patches. The blades are spread over the upward-facing part of the child named turf_roof and
// lean halfway from upright towards the slope
internal sealed class TurfRoofGrass : MonoBehaviour
{
	public const string RoofName = "turf_roof";

	// How far the blades lean from upright towards the roof's slope, 0 upright to 1 square to the roof
	private const float Lean = 0.5f;

	// Faces steeper than this, or facing down, get no grass
	private const float MinUp = 0.2f;

	// The most blades one of the game's grass objects can hold
	private const int MaxPerRenderer = 1023;

	private struct Face
	{
		public Vector3 a;
		public Vector3 b;
		public Vector3 c;
		public Vector3 normal;
		public float area;
	}

	private void Start()
	{
		ZNetView nview = GetComponent<ZNetView>();
		// The placement ghost has none
		if (nview && nview.IsValid())
		{
			Build();
		}
		enabled = false;
	}

	private void Build()
	{
		ClutterSystem clutter = ClutterSystem.instance;
		if (!clutter || clutter.m_quality == ClutterSystem.Quality.Off)
		{
			return;
		}
		Transform roof = Utils.FindChild(transform, RoofName);
		if (!roof)
		{
			return;
		}
		List<Face> faces = GetFaces(roof);
		if (faces.Count == 0)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + name + ": no grass on " + RoofName + ", as its mesh is not readable and it has no collider. Turn on Read/Write in the mesh's import settings.");
			return;
		}
		float total = 0f;
		Vector3 centre = Vector3.zero;
		foreach (Face face in faces)
		{
			total += face.area;
			centre += (face.a + face.b + face.c) / 3f * face.area;
		}
		centre /= total;

		Vector3 position = transform.position;
		// The same every time and for every player
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(Mathf.RoundToInt(position.x * 13f) ^ Mathf.RoundToInt(position.z * 7919f));
		Heightmap.Biome biome = Heightmap.FindBiomeClutter(position);
		if (biome == Heightmap.Biome.None || Grow(clutter, biome, faces, centre, roof) == 0)
		{
			Grow(clutter, Heightmap.Biome.Meadows, faces, centre, roof);
		}
		UnityEngine.Random.state = state;
	}

	// Covers the roof evenly in the biome's grass, as thickly as the game grows its thickest kind on the ground. The
	// game keeps each kind to its own patches and forest or open ground; the roof does not, so it has no bare spots:
	// each blade is one of the biome's kinds, picked by how common the kind is. The blades are spread out on a
	// jittered grid rather than at random, so they do not clump. Returns how many blades it grew
	private int Grow(ClutterSystem clutter, Heightmap.Biome biome, List<Face> faces, Vector3 centre, Transform roof)
	{
		List<ClutterSystem.Clutter> kinds = new List<ClutterSystem.Clutter>();
		List<float> weights = new List<float>();
		float altitude = centre.y - clutter.m_waterLevel;
		float density = 0f;
		float weightTotal = 0f;
		foreach (ClutterSystem.Clutter kind in clutter.m_clutter)
		{
			if (!kind.m_enabled || !kind.m_instanced || !kind.m_onUncleared || !kind.m_prefab || (kind.m_biome & biome) == 0
				|| altitude < kind.m_minAlt || altitude > kind.m_maxAlt)
			{
				continue;
			}
			int perPatch = clutter.m_quality switch
			{
				ClutterSystem.Quality.Low => kind.m_amount / 4,
				ClutterSystem.Quality.Med => kind.m_amount / 2,
				_ => kind.m_amount,
			};
			float perMetre = perPatch * clutter.m_amountScale / (clutter.m_grassPatchSize * clutter.m_grassPatchSize);
			if (perMetre <= 0f)
			{
				continue;
			}
			kinds.Add(kind);
			weights.Add(perMetre);
			weightTotal += perMetre;
			density = Mathf.Max(density, perMetre);
		}
		if (kinds.Count == 0)
		{
			return 0;
		}
		InstanceRenderer[] renderers = new InstanceRenderer[kinds.Count];
		int[] held = new int[kinds.Count];
		int grown = 0;
		foreach (Face face in faces)
		{
			float expected = density * face.area;
			// The face cut into rows x rows small triangles, a blade in each, some left out to keep the thickness right
			int rows = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(expected)));
			float keep = expected / (rows * rows);
			Quaternion tilt = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(Vector3.up, face.normal), Lean);
			Vector3 stepU = (face.b - face.a) / rows;
			Vector3 stepV = (face.c - face.a) / rows;
			for (int i = 0; i < rows; i++)
			{
				for (int j = 0; j < rows - i; j++)
				{
					// The small triangle pointing one way, then the one pointing back beside it
					for (int flip = 0; flip < (j < rows - i - 1 ? 2 : 1); flip++)
					{
						if (UnityEngine.Random.value >= keep)
						{
							continue;
						}
						float u = UnityEngine.Random.value;
						float v = UnityEngine.Random.value;
						if (u + v > 1f)
						{
							u = 1f - u;
							v = 1f - v;
						}
						Vector3 point = flip == 0
							? face.a + stepU * (i + u) + stepV * (j + v)
							: face.a + stepU * (i + 1 - u) + stepV * (j + 1 - v);
						int pick = Pick(weights, weightTotal);
						ClutterSystem.Clutter kind = kinds[pick];
						float turn = UnityEngine.Random.Range(0f, 360f);
						float scale = UnityEngine.Random.Range(kind.m_scaleMin, kind.m_scaleMax);
						if (!renderers[pick] || held[pick] >= MaxPerRenderer)
						{
							held[pick] = 0;
							renderers[pick] = AddRenderer(clutter, kind, roof, centre);
							if (!renderers[pick])
							{
								continue;
							}
						}
						renderers[pick].AddInstance(point, tilt * Quaternion.Euler(0f, turn, 0f), scale);
						held[pick]++;
						grown++;
					}
				}
			}
		}
		return grown;
	}

	private static int Pick(List<float> weights, float total)
	{
		float roll = UnityEngine.Random.value * total;
		for (int i = 0; i < weights.Count - 1; i++)
		{
			roll -= weights[i];
			if (roll < 0f)
			{
				return i;
			}
		}
		return weights.Count - 1;
	}

	// The upward-facing faces of the roof, in world space. They come from its meshes when those can be read, and
	// otherwise from rays dropped onto its colliders
	private static List<Face> GetFaces(Transform roof)
	{
		List<Face> faces = new List<Face>();
		bool unreadable = false;
		foreach (MeshFilter filter in roof.GetComponentsInChildren<MeshFilter>())
		{
			Mesh mesh = filter.sharedMesh;
			if (!mesh)
			{
				continue;
			}
			if (!mesh.isReadable)
			{
				unreadable = true;
				continue;
			}
			Vector3[] vertices = mesh.vertices;
			int[] triangles = mesh.triangles;
			Matrix4x4 world = filter.transform.localToWorldMatrix;
			for (int i = 0; i + 2 < triangles.Length; i += 3)
			{
				AddFace(faces, world.MultiplyPoint3x4(vertices[triangles[i]]), world.MultiplyPoint3x4(vertices[triangles[i + 1]]), world.MultiplyPoint3x4(vertices[triangles[i + 2]]));
			}
		}
		if (faces.Count == 0 && unreadable)
		{
			AddFacesFromColliders(faces, roof);
		}
		return faces;
	}

	private static void AddFace(List<Face> faces, Vector3 a, Vector3 b, Vector3 c)
	{
		Vector3 cross = Vector3.Cross(b - a, c - a);
		float area = cross.magnitude * 0.5f;
		if (area < 0.0001f)
		{
			return;
		}
		Vector3 normal = cross.normalized;
		if (normal.y < MinUp)
		{
			return;
		}
		faces.Add(new Face { a = a, b = b, c = c, normal = normal, area = area });
	}

	// A grid of rays dropped onto the roof's colliders, every quarter metre, each hit making a small square face
	private static void AddFacesFromColliders(List<Face> faces, Transform roof)
	{
		Collider[] colliders = roof.GetComponentsInChildren<Collider>();
		if (colliders.Length == 0)
		{
			return;
		}
		Bounds bounds = colliders[0].bounds;
		foreach (Collider collider in colliders)
		{
			bounds.Encapsulate(collider.bounds);
		}
		const float step = 0.25f;
		float top = bounds.max.y + 1f;
		float depth = bounds.size.y + 2f;
		for (float x = bounds.min.x + step / 2f; x < bounds.max.x; x += step)
		{
			for (float z = bounds.min.z + step / 2f; z < bounds.max.z; z += step)
			{
				Ray ray = new Ray(new Vector3(x, top, z), Vector3.down);
				float nearest = float.MaxValue;
				RaycastHit best = default;
				foreach (Collider collider in colliders)
				{
					if (collider.enabled && collider.Raycast(ray, out RaycastHit hit, depth) && hit.distance < nearest)
					{
						nearest = hit.distance;
						best = hit;
					}
				}
				if (nearest == float.MaxValue || best.normal.y < MinUp)
				{
					continue;
				}
				// A square on the slope whose shadow from above is one step of the grid
				Vector3 n = best.normal;
				Vector3 alongX = new Vector3(step, -n.x / n.y * step, 0f);
				Vector3 alongZ = new Vector3(0f, -n.z / n.y * step, step);
				Vector3 corner = best.point - alongX / 2f - alongZ / 2f;
				AddFace(faces, corner, corner + alongZ, corner + alongX);
				AddFace(faces, corner + alongX, corner + alongZ, corner + alongX + alongZ);
			}
		}
	}

	// The game's own grass object for this kind, holding the blades as the game's grass patches do. It sits in the
	// middle of the roof, which is where the game measures how far away the grass is
	private InstanceRenderer AddRenderer(ClutterSystem clutter, ClutterSystem.Clutter kind, Transform parent, Vector3 centre)
	{
		GameObject grass = Instantiate(kind.m_prefab, centre, Quaternion.identity, parent);
		grass.name = "cwf_grass_" + kind.m_name;
		InstanceRenderer renderer = grass.GetComponent<InstanceRenderer>();
		if (!renderer)
		{
			Destroy(grass);
			return null;
		}
		renderer.Clear();
		float furthest = clutter.m_distance - clutter.m_grassPatchSize / 2f;
		if (renderer.m_lodMaxDistance > furthest)
		{
			renderer.m_lodMaxDistance = furthest;
		}
		return renderer;
	}
}
