using System;
using System.Globalization;

namespace CorewoodFortified.LadderAnimation;

internal readonly struct ClimbHeading : IEquatable<ClimbHeading>
{
	public static ClimbHeading None => default(ClimbHeading);

	public float X { get; }

	public float Z { get; }

	public bool IsKnown => X * X + Z * Z > 0f;

	private ClimbHeading(float x, float z)
	{
		X = x;
		Z = z;
	}

	public static ClimbHeading FromXz(float x, float z)
	{
		float num = (float)Math.Sqrt(x * x + z * z);
		if (num <= 0.0001f || float.IsNaN(num) || float.IsInfinity(num))
		{
			return None;
		}
		return new ClimbHeading(x / num, z / num);
	}

	public static ClimbHeading FromTo(ClimbPoint from, ClimbPoint to)
	{
		return FromXz(to.X - from.X, to.Z - from.Z);
	}

	public ClimbHeading Opposite()
	{
		if (!IsKnown)
		{
			return None;
		}
		return new ClimbHeading(0f - X, 0f - Z);
	}

	public float Agreement(ClimbHeading other)
	{
		if (!IsKnown || !other.IsKnown)
		{
			return 0f;
		}
		return X * other.X + Z * other.Z;
	}

	public bool Equals(ClimbHeading other)
	{
		if (X.Equals(other.X))
		{
			return Z.Equals(other.Z);
		}
		return false;
	}

	public override bool Equals(object? obj)
	{
		if (obj is ClimbHeading other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return X.GetHashCode() * 31 + Z.GetHashCode();
	}

	public override string ToString()
	{
		if (!IsKnown)
		{
			return "(none)";
		}
		return string.Format(CultureInfo.InvariantCulture, "({0:0.##}, {1:0.##})", X, Z);
	}
}
