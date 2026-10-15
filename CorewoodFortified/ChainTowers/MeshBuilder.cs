using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CorewoodFortified;

internal sealed class MeshBuilder
{
	public readonly List<Vector3> Vertices = new List<Vector3>();

	public readonly List<Vector3> Normals = new List<Vector3>();

	public readonly List<Vector2> Uvs = new List<Vector2>();

	public readonly List<int> Triangles = new List<int>();

	public float UvPerMetre = 0.5f;

	public int Count => Vertices.Count;

	public int Vertex(Vector3 position, Vector3 normal, Vector2 uv)
	{
		Vertices.Add(position);
		Normals.Add(normal);
		Uvs.Add(uv);
		return Vertices.Count - 1;
	}

	public void Triangle(int a, int b, int c)
	{
		Triangles.Add(a);
		Triangles.Add(b);
		Triangles.Add(c);
	}

	public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uvOffset = default(Vector2))
	{
		Vector3 normalized = Vector3.Cross(b - a, d - a).normalized;
		float x = Vector3.Distance(a, b) * UvPerMetre;
		float y = Vector3.Distance(a, d) * UvPerMetre;
		int num = Vertex(a, normalized, uvOffset);
		Vertex(b, normalized, uvOffset + new Vector2(x, 0f));
		Vertex(c, normalized, uvOffset + new Vector2(x, y));
		Vertex(d, normalized, uvOffset + new Vector2(0f, y));
		Triangle(num, num + 1, num + 2);
		Triangle(num, num + 2, num + 3);
	}

	public void Box(Vector3 center, Vector3 size, Quaternion rotation, Vector2 uvOffset = default(Vector2))
	{
		Vector3 h = size * 0.5f;
		Quad(P(-1f, -1f, 1f), P(1f, -1f, 1f), P(1f, 1f, 1f), P(-1f, 1f, 1f), uvOffset);
		Quad(P(1f, -1f, -1f), P(-1f, -1f, -1f), P(-1f, 1f, -1f), P(1f, 1f, -1f), uvOffset);
		Quad(P(1f, -1f, 1f), P(1f, -1f, -1f), P(1f, 1f, -1f), P(1f, 1f, 1f), uvOffset);
		Quad(P(-1f, -1f, -1f), P(-1f, -1f, 1f), P(-1f, 1f, 1f), P(-1f, 1f, -1f), uvOffset);
		Quad(P(-1f, 1f, 1f), P(1f, 1f, 1f), P(1f, 1f, -1f), P(-1f, 1f, -1f), uvOffset);
		Quad(P(-1f, -1f, -1f), P(1f, -1f, -1f), P(1f, -1f, 1f), P(-1f, -1f, 1f), uvOffset);
		Vector3 P(float x, float y, float z)
		{
			return center + rotation * new Vector3(x * h.x, y * h.y, z * h.z);
		}
	}

	public void Box(Vector3 center, Vector3 size)
	{
		Box(center, size, Quaternion.identity);
	}

	public void Cylinder(Vector3 a, Vector3 b, float radius, int sides, bool caps = true)
	{
		Vector3 normalized = (b - a).normalized;
		Vector3 normalized2 = Vector3.Cross(normalized, (Mathf.Abs(normalized.y) < 0.9f) ? Vector3.up : Vector3.right).normalized;
		Vector3 vector = Vector3.Cross(normalized, normalized2);
		float y = Vector3.Distance(a, b) * UvPerMetre;
		float num = Mathf.PI * 2f * radius * UvPerMetre;
		int count = Count;
		for (int i = 0; i <= sides; i++)
		{
			float f = (float)i * 2f * Mathf.PI / (float)sides;
			Vector3 vector2 = normalized2 * Mathf.Cos(f) + vector * Mathf.Sin(f);
			Vertex(a + vector2 * radius, vector2, new Vector2(num * (float)i / (float)sides, 0f));
			Vertex(b + vector2 * radius, vector2, new Vector2(num * (float)i / (float)sides, y));
		}
		for (int j = 0; j < sides; j++)
		{
			int num2 = count + j * 2;
			Triangle(num2, num2 + 3, num2 + 1);
			Triangle(num2, num2 + 2, num2 + 3);
		}
		if (!caps)
		{
			return;
		}
		(Vector3, Vector3)[] array = new(Vector3, Vector3)[2]
		{
			(a, -normalized),
			(b, normalized)
		};
		for (int k = 0; k < array.Length; k++)
		{
			(Vector3, Vector3) tuple = array[k];
			Vector3 item = tuple.Item1;
			Vector3 item2 = tuple.Item2;
			int a2 = Vertex(item, item2, new Vector2(0.5f, 0.5f));
			int count2 = Count;
			for (int l = 0; l < sides; l++)
			{
				float f2 = (float)l * 2f * Mathf.PI / (float)sides;
				Vector3 vector3 = normalized2 * Mathf.Cos(f2) + vector * Mathf.Sin(f2);
				Vertex(item + vector3 * radius, item2, new Vector2(0.5f + 0.5f * Mathf.Cos(f2), 0.5f + 0.5f * Mathf.Sin(f2)));
			}
			for (int m = 0; m < sides; m++)
			{
				int num3 = count2 + m;
				int num4 = count2 + (m + 1) % sides;
				if (item2 == normalized)
				{
					Triangle(a2, num3, num4);
				}
				else
				{
					Triangle(a2, num4, num3);
				}
			}
		}
	}

	public int Link(Vector3 center, Vector3 along, Vector3 across, float length, float width, float wire, int pathPoints, int wireSides)
	{
		Vector3 normalized = Vector3.Cross(along, across).normalized;
		float num = (width - wire) * 0.5f;
		float num2 = Mathf.Max(0f, (length - wire) * 0.5f - num);
		int count = Count;
		for (int i = 0; i < pathPoints; i++)
		{
			float f = ((float)i + 0.5f) * 2f * Mathf.PI / (float)pathPoints;
			float num3 = Mathf.Cos(f);
			float num4 = Mathf.Sin(f);
			Vector3 vector = along * (num3 * num + Mathf.Sign(num3) * num2) + across * (num4 * num);
			Vector3 normalized2 = (along * num3 + across * num4).normalized;
			for (int j = 0; j < wireSides; j++)
			{
				float f2 = (float)j * 2f * Mathf.PI / (float)wireSides;
				Vector3 vector2 = normalized2 * Mathf.Cos(f2) + normalized * Mathf.Sin(f2);
				Vertex(center + vector + vector2 * (wire * 0.5f), vector2, new Vector2((float)i / (float)pathPoints, (float)j / (float)wireSides));
			}
		}
		for (int k = 0; k < pathPoints; k++)
		{
			int num5 = (k + 1) % pathPoints;
			for (int l = 0; l < wireSides; l++)
			{
				int num6 = (l + 1) % wireSides;
				int a = count + k * wireSides + l;
				int b = count + num5 * wireSides + l;
				int num7 = count + num5 * wireSides + num6;
				int c = count + k * wireSides + num6;
				Triangle(a, b, num7);
				Triangle(a, num7, c);
			}
		}
		return count;
	}

	public Mesh ToMesh(string name)
	{
		Mesh mesh = new Mesh
		{
			name = name
		};
		if (Vertices.Count > 65000)
		{
			mesh.indexFormat = IndexFormat.UInt32;
		}
		mesh.SetVertices(Vertices);
		mesh.SetNormals(Normals);
		mesh.SetUVs(0, Uvs);
		mesh.SetTriangles(Triangles, 0);
		mesh.RecalculateBounds();
		mesh.RecalculateTangents();
		return mesh;
	}
}
