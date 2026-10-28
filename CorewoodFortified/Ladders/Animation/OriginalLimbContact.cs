namespace CorewoodFortified.LadderAnimation;

internal readonly struct OriginalLimbContact
{
	internal float Height { get; }

	internal float Clearance { get; }

	internal bool Planted { get; }

	internal float Swing { get; }

	internal OriginalLimbContact(float height, float clearance, bool planted, float swing)
	{
		Height = height;
		Clearance = clearance;
		Planted = planted;
		Swing = swing;
	}
}
