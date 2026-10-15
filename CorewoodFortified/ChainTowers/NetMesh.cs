using System.Collections.Generic;
using UnityEngine;

namespace CorewoodFortified;

internal static class NetMesh
{
	private const float NetPitch = 0.26f;

	private const float HeavyPitch = 0.38f;

	public static Mesh Build(Vector3[] rest, int columns, int rows, out int links)
	{
		MeshBuilder meshBuilder = new MeshBuilder
		{
			UvPerMetre = 1f
		};
		List<BoneWeight> list = new List<BoneWeight>();
		links = 0;
		int num = 0;
		for (int i = 0; i < columns; i++)
		{
			for (int j = 0; j + 1 < rows; j++)
			{
				num += Mathf.RoundToInt(Vector3.Distance(rest[i * rows + j], rest[i * rows + j + 1]) / 0.26f);
			}
		}
		bool coarse = num > 1600;
		int num2 = rest.Length;
		for (int k = 0; k < columns; k++)
		{
			for (int l = 0; l + 1 < rows; l++)
			{
				links += Segment(meshBuilder, list, rest, k * rows + l, k * rows + l + 1, 0, heavy: false, coarse);
			}
		}
		for (int m = 0; m < rows; m++)
		{
			for (int n = 0; n + 1 < columns; n++)
			{
				links += Segment(meshBuilder, list, rest, n * rows + m, (n + 1) * rows + m, num2, m == 0 || m == rows - 1, coarse);
			}
		}
		Mesh mesh = meshBuilder.ToMesh("CWChain_net");
		mesh.boneWeights = list.ToArray();
		Matrix4x4[] array = new Matrix4x4[num2 * 2];
		for (int num3 = 0; num3 < num2; num3++)
		{
			array[num3] = (array[num2 + num3] = Matrix4x4.Translate(-rest[num3]));
		}
		mesh.bindposes = array;
		return mesh;
	}

	public static Mesh BuildFar(Vector3[] rest, int columns, int rows, float width)
	{
		MeshBuilder meshBuilder = new MeshBuilder
		{
			UvPerMetre = 1f
		};
		List<BoneWeight> list = new List<BoneWeight>();
		int num = rest.Length;
		for (int i = 0; i < columns; i++)
		{
			for (int j = 0; j + 1 < rows; j++)
			{
				Bar(meshBuilder, list, rest, i * rows + j, i * rows + j + 1, 0, width);
			}
		}
		for (int k = 0; k < rows; k++)
		{
			for (int l = 0; l + 1 < columns; l++)
			{
				Bar(meshBuilder, list, rest, l * rows + k, (l + 1) * rows + k, num, (k == 0 || k == rows - 1) ? (width * 1.6f) : width);
			}
		}
		Mesh mesh = meshBuilder.ToMesh("CWChain_netfar");
		mesh.boneWeights = list.ToArray();
		Matrix4x4[] array = new Matrix4x4[num * 2];
		for (int m = 0; m < num; m++)
		{
			array[m] = (array[num + m] = Matrix4x4.Translate(-rest[m]));
		}
		mesh.bindposes = array;
		return mesh;
	}

	private static void Bar(MeshBuilder builder, List<BoneWeight> weights, Vector3[] rest, int a, int b, int boneSet, float width)
	{
		Vector3 a2 = rest[a];
		Vector3 b2 = rest[b];
		if (!(Vector3.Distance(a2, b2) < 0.05f))
		{
			int count = builder.Count;
			builder.Cylinder(a2, b2, width * 0.5f, 4, caps: false);
			for (int i = count; i < builder.Count; i++)
			{
				bool flag = (i - count) % 2 == 0;
				weights.Add(new BoneWeight
				{
					boneIndex0 = boneSet + (flag ? a : b),
					weight0 = 1f
				});
				builder.Uvs[i] = new Vector2(0.4f + builder.Uvs[i].x * 0.08f, 0.4f + builder.Uvs[i].y * 0.08f);
			}
		}
	}

	private static int Segment(MeshBuilder builder, List<BoneWeight> weights, Vector3[] rest, int a, int b, int boneSet, bool heavy, bool coarse)
	{
		Vector3 vector = rest[a];
		Vector3 vector2 = rest[b];
		float num = Vector3.Distance(vector, vector2);
		if (num < 0.05f)
		{
			return 0;
		}
		Vector3 vector3 = (vector2 - vector) / num;
		Vector3 forward = Vector3.forward;
		Vector3 vector4 = Vector3.Cross(vector3, forward);
		if (vector4.sqrMagnitude < 1E-06f)
		{
			vector4 = Vector3.Cross(vector3, Vector3.up);
		}
		vector4.Normalize();
		float num2 = (heavy ? 0.38f : 0.26f);
		int num3 = Mathf.Max(1, Mathf.RoundToInt(num / num2));
		float num4 = num / (float)num3;
		int pathPoints = (heavy ? 10 : (coarse ? 6 : 8));
		int wireSides = (heavy ? 5 : (coarse ? 3 : 4));
		for (int i = 0; i < num3; i++)
		{
			Vector3 center = vector + vector3 * (((float)i + 0.5f) * num4);
			Vector3 across = ((i % 2 == 0) ? forward : vector4);
			float num5 = num4 * 1.55f;
			float width = num5 * (heavy ? 0.64f : 0.6f);
			float wire = num5 * (heavy ? 0.2f : 0.17f);
			for (int j = builder.Link(center, vector3, across, num5, width, wire, pathPoints, wireSides); j < builder.Count; j++)
			{
				float num6 = Mathf.Clamp01(Vector3.Dot(builder.Vertices[j] - vector, vector3) / num);
				weights.Add((num6 < 0.5f) ? new BoneWeight
				{
					boneIndex0 = boneSet + a,
					weight0 = 1f - num6,
					boneIndex1 = boneSet + b,
					weight1 = num6
				} : new BoneWeight
				{
					boneIndex0 = boneSet + b,
					weight0 = num6,
					boneIndex1 = boneSet + a,
					weight1 = 1f - num6
				});
				builder.Uvs[j] = new Vector2(0.4f + builder.Uvs[j].x * 0.08f, 0.4f + builder.Uvs[j].y * 0.08f);
			}
		}
		return num3;
	}
}
