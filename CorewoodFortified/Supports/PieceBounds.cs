using UnityEngine;

namespace CorewoodFortified;

internal static class PieceBounds
{
	// The box that holds the given bounds once moved by the matrix, such as into a piece's own space
	public static Bounds Transform(Bounds bounds, Matrix4x4 matrix)
	{
		Vector3 min = bounds.min;
		Vector3 max = bounds.max;
		Bounds result = new Bounds(matrix.MultiplyPoint3x4(min), Vector3.zero);
		for (int i = 1; i < 8; i++)
		{
			result.Encapsulate(matrix.MultiplyPoint3x4(new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z)));
		}
		return result;
	}
}
