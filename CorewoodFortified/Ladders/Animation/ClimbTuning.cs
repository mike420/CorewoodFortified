using System;

namespace CorewoodFortified.LadderAnimation;

internal static class ClimbTuning
{
	internal static GaitShape Gait = GeneratedGait.Shape();

	internal static float PelvisDrop = 0f;

	internal static float PelvisToward = 0.16f;

	internal static float Lean = 12f;

	internal static float SlideLean = 6f;

	internal static float SwayTilt = 3f;

	internal static float SwayShift = 0.03f;

	internal static float HandAbove = 0.055f;

	internal static float HandOut = 0.025f;

	internal static float HandSpan = 0.19f;

	internal static float HandFingerPitch = 70f;

	internal static float HandNormalSign = 1f;

	internal static float HandPalmAlong = 0.07f;

	internal static float HandPalmDepth = -0.01f;

	internal static float FootBallForward = 0.165f;

	internal static float FootAnkleHeight = 0.146f;

	internal static float HandWrapDegrees = 35f;

	internal static float PalmThickness = 0.02f;

	internal static float SoleThickness = 0.03f;

	internal static float FootAbove = 0.045f;

	internal static float FootOut = 0f;

	internal static float FootSpan = 0.128f;

	internal static float ElbowOut = 0.18f;

	internal static float ElbowSide = 0.08f;

	internal static float ElbowDown = 0.32f;

	internal static float KneeToward = 0.22f;

	internal static float KneeSide = 0.04f;

	internal static float KneeDown = 0.15f;

	internal static float RailFromBelow = 0.42f;

	internal static float RailBlend = 0.12f;

	internal static float RailBelowShoulder = 0.3f;

	internal static float RailSide = 0.34f;

	internal static float RailOut = 0.08f;

	internal static float ExitFold = 28f;

	internal static TransitionAction[] Transitions = GeneratedGait.Transitions();

	internal static float SquareHips = 1f;

	internal static float ProgressSmoothing = 0.05f;

	internal static float WeightRiseSeconds = 0.15f;

	internal static float SquareChest = 1f;

	internal static float KneeMidAscent = 1f;

	internal static float KneeAhead = 0.35f;

	internal static float TopHandsFree = 0.42f;

	internal static float TopHandsFreeSpan = 0.25f;

	internal static float LipOnto = 0.18f;

	internal static float FootOnto = 0.2f;

	internal static float TopApproachFrom = 1.15f;

	internal static float TopApproachSpan = 0.5f;

	internal static float TopApproachDrop = 0f;

	internal static float TopApproachToward = 0f;

	internal static float TopApproachLean = 0f;

	internal static float FadeFrom = 0.96f;

	internal const float ReachLimit = 1.01f;

	internal static bool LogSnapDecisions = false;

	internal static bool TraceContacts = false;

	internal static bool LogLandingQueries = false;

	internal static bool LogFit = false;

	internal static int Version { get; private set; }

	internal static TransitionAction Action(TransitionKind kind)
	{
		TransitionAction[] transitions = Transitions;
		foreach (TransitionAction transitionAction in transitions)
		{
			if (transitionAction.Kind == kind)
			{
				return transitionAction;
			}
		}
		transitions = DefaultTransitions.All();
		foreach (TransitionAction transitionAction2 in transitions)
		{
			if (transitionAction2.Kind == kind)
			{
				return transitionAction2;
			}
		}
		throw new InvalidOperationException("No authored transition " + kind.ToString() + ".");
	}

	internal static void Bump()
	{
		Version++;
	}
}
