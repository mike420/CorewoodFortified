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

		Player player = other.GetComponentInParent<Player>();
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
