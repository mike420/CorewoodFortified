using System;

namespace CorewoodFortified.LadderAnimation;

internal readonly struct LadderGeometry
{
	public const float MinimumClimbableHeight = 0.9f;

	public const float ConventionalRungPitch = 0.35f;

	public ClimbPoint Bottom { get; }

	public ClimbPoint Top { get; }

	public ClimbHeading StandingSide { get; }

	public float Width { get; }

	public float RungPitch { get; }

	public float Height => Math.Max(0f, Top.Y - Bottom.Y);

	public ClimbHeading FacingWhileClimbing => StandingSide.Opposite();

	public bool IsClimbable
	{
		get
		{
			if (Height >= 0.9f && StandingSide.IsKnown)
			{
				return Width > 0f;
			}
			return false;
		}
	}

	private LadderGeometry(ClimbPoint bottom, ClimbPoint top, ClimbHeading standingSide, float width, float rungPitch)
	{
		Bottom = bottom;
		Top = top;
		StandingSide = standingSide;
		Width = width;
		RungPitch = rungPitch;
	}

	public static bool TryMeasure(ClimbPoint bottom, ClimbPoint top, ClimbHeading standingSide, float width, float rungPitch, out LadderGeometry geometry)
	{
		geometry = default(LadderGeometry);
		if (!bottom.IsFinite || !top.IsFinite || !standingSide.IsKnown)
		{
			return false;
		}
		if (float.IsNaN(width) || width <= 0f || width > 4f)
		{
			return false;
		}
		if (float.IsNaN(rungPitch) || rungPitch <= 0f || rungPitch > 1f)
		{
			rungPitch = 0.35f;
		}
		if (top.Y - bottom.Y < 0.9f)
		{
			return false;
		}
		geometry = new LadderGeometry(bottom, top, standingSide, width, rungPitch);
		return true;
	}

	public ClimbPoint PointAt(float progress)
	{
		float height = Height;
		float num = ((height <= 0f) ? 0f : Math.Min(Math.Max(progress, 0f), height));
		float num2 = ((height <= 0f) ? 0f : (num / height));
		return new ClimbPoint(Bottom.X + (Top.X - Bottom.X) * num2, Bottom.Y + (Top.Y - Bottom.Y) * num2, Bottom.Z + (Top.Z - Bottom.Z) * num2);
	}

	public float ProgressOf(ClimbPoint point)
	{
		return Math.Min(Math.Max(point.Y - Bottom.Y, 0f), Height);
	}

	public float SidewaysOffsetOf(ClimbPoint point)
	{
		ClimbPoint climbPoint = PointAt(ProgressOf(point));
		float num = point.X - climbPoint.X;
		float num2 = point.Z - climbPoint.Z;
		float num3 = num * StandingSide.X + num2 * StandingSide.Z;
		float num4 = num - StandingSide.X * num3;
		float num5 = num2 - StandingSide.Z * num3;
		return (float)Math.Sqrt(num4 * num4 + num5 * num5);
	}

	public float ReachFrom(ClimbPoint point)
	{
		ClimbPoint climbPoint = PointAt(ProgressOf(point));
		float num = point.X - climbPoint.X;
		float num2 = point.Z - climbPoint.Z;
		return num * StandingSide.X + num2 * StandingSide.Z;
	}

	public ClimbPoint ClimbPositionAt(float progress, float bodyOffset)
	{
		return PointAt(progress).Offset(StandingSide, bodyOffset);
	}

	public int RungAt(float progress)
	{
		float num = ((RungPitch <= 0f) ? 0.35f : RungPitch);
		return (int)Math.Floor(Math.Min(Math.Max(progress, 0f), Height) / num);
	}

	public override string ToString()
	{
		return "ladder " + Bottom.ToString() + " -> " + Top.ToString() + " (" + Height.ToString("0.##") + " m, standing side " + StandingSide.ToString() + ")";
	}
}
