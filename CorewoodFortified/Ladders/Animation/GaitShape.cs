namespace CorewoodFortified.LadderAnimation;

internal sealed class GaitShape
{
	internal ClimbLoop Up = new ClimbLoop();

	internal ClimbLoop Down = DefaultDown();

	internal float FootLowRel;

	internal float HandMidFromShoulder;

	internal float HandSettleBelow = 0.38f;

	internal float HandSettleAbove = 0.38f;

	internal float FootSettleBelow;

	internal float FootSettleAbove = 0.4f;

	internal float SettleSeconds = 0.18f;

	internal float RejoinSeconds = 0.12f;

	internal float ReverseSeconds = 0.3f;

	internal bool Valid
	{
		get
		{
			if (Up != null && Down != null && Up.Valid && Down.Valid && Up.StrideRungs == Down.StrideRungs && Between(FootLowRel, -0.5f, 0.5f) && Between(HandMidFromShoulder, -0.6f, 0.6f) && Between(SettleSeconds, 0.02f, 1f) && Between(RejoinSeconds, 0.01f, 1f))
			{
				return Between(ReverseSeconds, 0.01f, 2f);
			}
			return false;
		}
	}

	internal GaitShape Copy()
	{
		GaitShape obj = (GaitShape)MemberwiseClone();
		obj.Up = Up.Copy();
		obj.Down = Down.Copy();
		return obj;
	}

	internal static ClimbLoop DefaultDown()
	{
		ClimbLoop climbLoop = new ClimbLoop();
		climbLoop.Name = "climb_down";
		climbLoop.Start = new float[4] { 0.1f, 0.6f, 0.62f, 0.12f };
		climbLoop.Length = new float[4] { 0.4f, 0.4f, 0.44f, 0.44f };
		climbLoop.Arc = new float[4][][]
		{
			ClimbLoop.Arc3(33, 0f, 0.06f, 0.01f),
			ClimbLoop.Arc3(33, 0f, 0.06f, 0.01f),
			ClimbLoop.Arc3(33, 0f, 0.09f, 0f),
			ClimbLoop.Arc3(33, 0f, 0.09f, 0f)
		};
		climbLoop.Body = ClimbLoop.Dwell(61, 0.5f, new float[2] { 0.62f, 0.12f }, 0.44f, 0.85f);
		return climbLoop;
	}

	private static bool Between(float value, float min, float max)
	{
		if (!float.IsNaN(value) && value >= min)
		{
			return value <= max;
		}
		return false;
	}
}
