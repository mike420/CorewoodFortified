using UnityEngine;

namespace CorewoodFortified;

// A rope between two points, sagging as a parabola, measured along its length
internal sealed class RopePath
{
	private const int Samples = 64;

	public readonly Vector3 A;

	public readonly Vector3 B;

	public readonly float SagDepth;

	public readonly float Length;

	private readonly float[] m_distance = new float[Samples + 1];

	public RopePath(Vector3 a, Vector3 b, float sagFactor)
	{
		A = a;
		B = b;
		SagDepth = Mathf.Max(0f, sagFactor) * Vector3.Distance(a, b);
		Vector3 previous = A;
		m_distance[0] = 0f;
		for (int i = 1; i <= Samples; i++)
		{
			Vector3 point = PointT((float)i / Samples);
			m_distance[i] = m_distance[i - 1] + Vector3.Distance(previous, point);
			previous = point;
		}
		Length = Mathf.Max(0.01f, m_distance[Samples]);
	}

	public Vector3 PointT(float t)
	{
		t = Mathf.Clamp01(t);
		return Vector3.LerpUnclamped(A, B, t) + Vector3.down * (4f * SagDepth * t * (1f - t));
	}

	// The curve parameter for a distance along the rope from A
	public float TAt(float s)
	{
		if (s <= 0f)
		{
			return 0f;
		}
		if (s >= Length)
		{
			return 1f;
		}
		int low = 0;
		int high = Samples;
		while (high - low > 1)
		{
			int mid = (low + high) >> 1;
			if (m_distance[mid] <= s)
			{
				low = mid;
			}
			else
			{
				high = mid;
			}
		}
		float span = m_distance[high] - m_distance[low];
		float fraction = span > 1E-05f ? (s - m_distance[low]) / span : 0f;
		return (low + fraction) / Samples;
	}

	public Vector3 PointAt(float s)
	{
		return PointT(TAt(s));
	}

	public Vector3 TangentAt(float s)
	{
		float t = TAt(s);
		Vector3 tangent = B - A + Vector3.up * (4f * SagDepth * (2f * t - 1f));
		return tangent.sqrMagnitude > 1E-06f ? tangent.normalized : Vector3.forward;
	}

	public bool HasEnd(Vector3 p, float tolerance)
	{
		float squared = tolerance * tolerance;
		return (A - p).sqrMagnitude <= squared || (B - p).sqrMagnitude <= squared;
	}

	public Vector3 OtherEnd(Vector3 p)
	{
		return (A - p).sqrMagnitude <= (B - p).sqrMagnitude ? B : A;
	}

	public void Fill(Vector3[] points)
	{
		int last = points.Length - 1;
		for (int i = 0; i <= last; i++)
		{
			points[i] = PointT((float)i / last);
		}
	}

	// The curve parameter of the rope point nearest p
	public float ClosestT(Vector3 p)
	{
		Vector3 chord = B - A;
		float t = Mathf.Clamp01(Vector3.Dot(p - A, chord) / Mathf.Max(0.0001f, chord.sqrMagnitude));
		for (int i = 0; i < 4; i++)
		{
			Vector3 tangent = chord + Vector3.down * (4f * SagDepth * (1f - 2f * t));
			float step = Vector3.Dot(p - PointT(t), tangent) / Mathf.Max(0.0001f, tangent.sqrMagnitude);
			t = Mathf.Clamp01(t + step);
		}
		return t;
	}

	// The distance along the rope from A to the curve parameter t
	public float DistanceAtT(float t)
	{
		float low = 0f;
		float high = Length;
		for (int i = 0; i < 24; i++)
		{
			float mid = (low + high) * 0.5f;
			if (TAt(mid) < t)
			{
				low = mid;
			}
			else
			{
				high = mid;
			}
		}
		return (low + high) * 0.5f;
	}
}
