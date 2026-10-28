using System;

namespace CorewoodFortified.LadderAnimation;

[Flags]
internal enum NativeActionParts
{
	None = 0,
	LeftArm = 1,
	RightArm = 2,
	UpperBody = 4,
	Head = 8
}
