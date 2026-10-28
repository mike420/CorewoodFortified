using System;

namespace CorewoodFortified.LadderAnimation;

internal interface IClimbPresenter : IDisposable
{
	bool Install(string harmonyId);

	void Publish(Player player, in ClimbPresentationFrame frame);

	void End(Player? player, ClimbEnd reason);

	void Update(float deltaTime);
}
