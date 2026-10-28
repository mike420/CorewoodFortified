using System;

namespace CorewoodFortified.LadderAnimation;

internal readonly struct ClimbPresentationFrame
{
	internal const float RiseShare = 0.4f;

	internal RungProfile[]? Profiles { get; }

	internal int Session { get; }

	internal int Discontinuity { get; }

	internal int GeometryRevision { get; }

	internal long Sample { get; }

	internal float SampleTime { get; }

	internal PresentedMotion Motion { get; }

	internal ClimbGait Gait { get; }

	internal float Travel { get; }

	internal float StepTravel { get; }

	internal ClimbPoint Feet { get; }

	internal LadderGeometry Run { get; }

	internal LadderFace Face { get; }

	internal float RungPlane { get; }

	internal float RungOrigin { get; }

	internal int LastRung { get; }

	internal float Transition { get; }

	internal ClimbPoint TransitionFrom { get; }

	internal ClimbPoint TransitionTo { get; }

	internal NativeActionParts NativeAction { get; }

	internal bool HammerEquipped { get; }

	internal bool MenuHold { get; }

	internal static float HalfWidth => 0.31f;

	internal static float HalfHeight => 0.04f;

	internal static float HalfDepth => 0.06f;

	internal ClimbPresentationFrame(int session, int discontinuity, int geometryRevision, long sample, float sampleTime, PresentedMotion motion, ClimbGait gait, float travel, float stepTravel, ClimbPoint feet, LadderGeometry run, LadderFace face, float rungPlane, float rungOrigin, int lastRung, float transition, ClimbPoint transitionFrom, ClimbPoint transitionTo, NativeActionParts nativeAction, bool hammerEquipped, bool menuHold, RungProfile[]? profiles = null)
	{
		Session = session;
		Discontinuity = discontinuity;
		GeometryRevision = geometryRevision;
		Sample = sample;
		SampleTime = sampleTime;
		Motion = motion;
		Gait = gait;
		Travel = travel;
		StepTravel = stepTravel;
		Feet = feet;
		Run = run;
		Face = face;
		RungPlane = rungPlane;
		RungOrigin = rungOrigin;
		LastRung = lastRung;
		Transition = transition;
		TransitionFrom = transitionFrom;
		TransitionTo = transitionTo;
		NativeAction = nativeAction;
		HammerEquipped = hammerEquipped;
		MenuHold = menuHold;
		Profiles = profiles;
	}

	internal RungProfile ProfileAt(float worldY)
	{
		RungProfile[] profiles = Profiles;
		if (profiles != null)
		{
			RungProfile[] array = profiles;
			for (int i = 0; i < array.Length; i++)
			{
				RungProfile result = array[i];
				if (worldY >= result.BottomY - 0.001f && worldY < result.TopY + 0.001f)
				{
					return result;
				}
			}
		}
		return RungProfile.Wood;
	}

	internal static int LastRungOf(float runHeight, float rungOrigin, float pitch)
	{
		if (!LimbGait.Finite(runHeight) || !LimbGait.Finite(pitch) || pitch <= 0f || runHeight < rungOrigin)
		{
			return -1;
		}
		return (int)Math.Floor((runHeight - rungOrigin) / pitch + 0.0001f);
	}
}
