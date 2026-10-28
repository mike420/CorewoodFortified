namespace CorewoodFortified.LadderAnimation;

internal readonly struct RungProfile
{
	internal float BottomY { get; }

	internal float TopY { get; }

	internal float HalfHeight { get; }

	internal float HalfDepth { get; }

	internal float Radius { get; }

	internal bool Round => Radius > 0f;

	internal static RungProfile Wood => new RungProfile(float.MinValue, float.MaxValue, 0.04f, 0.06f, 0f);

	internal RungProfile(float bottomY, float topY, float halfHeight, float halfDepth, float radius)
	{
		BottomY = bottomY;
		TopY = topY;
		HalfHeight = halfHeight;
		HalfDepth = halfDepth;
		Radius = radius;
	}
}
