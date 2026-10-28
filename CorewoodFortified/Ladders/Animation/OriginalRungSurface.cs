
namespace CorewoodFortified.LadderAnimation;

internal static class OriginalRungSurface
{
	internal static ClimbPoint At(in LadderGeometry run, LadderFace face, float rungPlane, float worldY, float outwardOffset, float actorRightOffset)
	{
		ClimbPoint climbPoint = run.PointAt(worldY - run.Bottom.Y);
		float num = (float)face;
		float num2 = run.StandingSide.X * num;
		float num3 = run.StandingSide.Z * num;
		return new ClimbPoint(climbPoint.X + run.StandingSide.X * rungPlane + num2 * outwardOffset - num3 * actorRightOffset, worldY, climbPoint.Z + run.StandingSide.Z * rungPlane + num3 * outwardOffset + num2 * actorRightOffset);
	}
}
