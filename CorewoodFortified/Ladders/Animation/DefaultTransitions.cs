namespace CorewoodFortified.LadderAnimation;

internal static class DefaultTransitions
{
	private const int LH = 0;

	private const int RH = 1;

	private const int LF = 2;

	private const int RF = 3;

	internal static TransitionAction[] All()
	{
		return new TransitionAction[6]
		{
			new TransitionAction.Builder(TransitionKind.GroundMount, 0.3f).Weight(1, 0.08f, 0f, 0.4f, 1f).Weight(2, 0.15f, 0f, 0.5f, 1f).Weight(0, 0.3f, 0f, 0.65f, 1f)
				.Weight(3, 0.5f, 0f, 0.95f, 1f)
				.Arc(1, 0.08f, 0.45f, 0f, 0.08f, 0.04f)
				.Arc(0, 0.3f, 0.7f, 0f, 0.08f, 0.04f)
				.Arc(2, 0.15f, 0.55f, 0f, 0.1f, 0.06f)
				.Arc(3, 0.5f, 1f, 0f, 0.12f, 0.08f)
				.Body(0f, 0f, 0.25f, 0.2f, 0.9f, 1f)
				.Build(),
			new TransitionAction.Builder(TransitionKind.AirCatch, 0.25f).Weight(0, 0f, 0f, 0.35f, 1f).Weight(1, 0.1f, 0f, 0.45f, 1f).Weight(2, 0.3f, 0f, 0.8f, 1f)
				.Weight(3, 0.45f, 0f, 1f, 1f)
				.Arc(2, 0.3f, 0.8f, 0f, 0.12f, 0.05f)
				.Arc(3, 0.45f, 1f, 0f, 0.12f, 0.05f)
				.Body(0f, 0.2f, 0.8f, 1f)
				.Build(),
			new TransitionAction.Builder(TransitionKind.TopExit, 1f).Weight(0, 0f, 1f, 0.2f, 0f).Weight(1, 0.02f, 1f, 0.22f, 0f).Weight(2, 0.04f, 1f, 0.24f, 0f)
				.Weight(3, 0.06f, 1f, 0.26f, 0f)
				.Body(0f, 1f, 0.3f, 0f)
				.Build(),
			new TransitionAction.Builder(TransitionKind.SideExit, 1f).Weight(0, 0f, 1f, 0.2f, 0f).Weight(1, 0.02f, 1f, 0.22f, 0f).Weight(2, 0.04f, 1f, 0.24f, 0f)
				.Weight(3, 0.06f, 1f, 0.26f, 0f)
				.Body(0f, 1f, 0.3f, 0f)
				.Build(),
			new TransitionAction.Builder(TransitionKind.BottomStepOff, 1f).Weight(0, 0f, 1f, 0.2f, 0f).Weight(1, 0.02f, 1f, 0.22f, 0f).Weight(2, 0.04f, 1f, 0.24f, 0f)
				.Weight(3, 0.06f, 1f, 0.26f, 0f)
				.Body(0f, 1f, 0.3f, 0f)
				.Build(),
			new TransitionAction.Builder(TransitionKind.TopEntry, 1f).Weight(2, 0.5f, 0f, 0.9f, 1f).Weight(3, 0.52f, 0f, 0.92f, 1f).Weight(0, 0.54f, 0f, 0.94f, 1f)
				.Weight(1, 0.56f, 0f, 0.96f, 1f)
				.Body(0f, 0f, 0.5f, 0f, 1f, 1f)
				.Build()
		};
	}
}
