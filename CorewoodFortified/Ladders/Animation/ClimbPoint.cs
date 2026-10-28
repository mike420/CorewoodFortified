using System;
using System.Globalization;

namespace CorewoodFortified.LadderAnimation;

internal readonly struct ClimbPoint(float x, float y, float z) : IEquatable<ClimbPoint>
{
	public float X { get; } = x;

	public float Y { get; } = y;

	public float Z { get; } = z;

	public bool IsFinite
	{
		get
		{
			if (IsFiniteValue(X) && IsFiniteValue(Y))
			{
				return IsFiniteValue(Z);
			}
			return false;
		}
	}

	public float HorizontalDistanceTo(ClimbPoint other)
	{
		float num = X - other.X;
		float num2 = Z - other.Z;
		return (float)Math.Sqrt(num * num + num2 * num2);
	}

	public float VerticalDistanceTo(ClimbPoint other)
	{
		return Math.Abs(Y - other.Y);
	}

	public ClimbPoint WithHeight(float y)
	{
		return new ClimbPoint(X, y, Z);
	}

	public ClimbPoint Offset(ClimbHeading heading, float metres)
	{
		return new ClimbPoint(X + heading.X * metres, Y, Z + heading.Z * metres);
	}

	public ClimbPoint Raise(float metres)
	{
		return new ClimbPoint(X, Y + metres, Z);
	}

	public bool Equals(ClimbPoint other)
	{
		if (X.Equals(other.X) && Y.Equals(other.Y))
		{
			return Z.Equals(other.Z);
		}
		return false;
	}

	public override bool Equals(object? obj)
	{
		if (obj is ClimbPoint other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((17 * 31 + X.GetHashCode()) * 31 + Y.GetHashCode()) * 31 + Z.GetHashCode();
	}

	public override string ToString()
	{
		return string.Format(CultureInfo.InvariantCulture, "({0:0.##}, {1:0.##}, {2:0.##})", X, Y, Z);
	}

	private static bool IsFiniteValue(float value)
	{
		if (!float.IsNaN(value))
		{
			return !float.IsInfinity(value);
		}
		return false;
	}
}
