using UnityEngine;

namespace CorewoodFortified;

internal sealed class TriggerPlatform : MonoBehaviour
{
	public Elevator m_platform;

	private void OnTriggerEnter(Collider other)
	{
		if (!MainConfig.autoTriggers.Value || m_platform == null || !m_platform.CanAutoTrigger())
		{
			return;
		}

		// Only a player or creature stepping on moves it, not a dropped item, an arrow or anything else landing there
		Character character = other.GetComponentInParent<Character>();
		if (character == null)
		{
			return;
		}
		Player player = character as Player;
		if (player != null)
		{
			if (player != Player.m_localPlayer)
			{
				return;
			}
		}
		else if (!m_platform.IsOwner())
		{
			return;
		}
		m_platform.Activate();
	}
}
