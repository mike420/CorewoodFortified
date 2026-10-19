using UnityEngine;

namespace CorewoodFortified;

// Kicks the organ cannon back when it fires, then rolls it forward again. Only the owner fires, but every client sees
// the time of the last shot, so each plays the recoil itself when that time changes
internal sealed class CannonRecoil : MonoBehaviour
{
	// How far the cannon is thrown back, in metres
	private const float Distance = 0.35f;

	// Seconds to reach the full kick, and to roll back into place after it
	private const float KickTime = 0.08f;

	private const float ReturnTime = 0.9f;

	// Everything that moves with the kick, which sits under the turret's neck so it also turns with it
	public Transform m_recoil;

	private ZNetView m_nview;

	private float m_lastAttack;

	private float m_kickedAt = float.NegativeInfinity;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
	}

	private void Start()
	{
		// A cannon loaded into view is not one that just fired
		if (m_nview && m_nview.IsValid())
		{
			m_lastAttack = m_nview.GetZDO().GetFloat(ZDOVars.s_lastAttack);
		}
	}

	private void Update()
	{
		if (!m_recoil || !m_nview || !m_nview.IsValid())
		{
			return;
		}
		float lastAttack = m_nview.GetZDO().GetFloat(ZDOVars.s_lastAttack);
		if (lastAttack > m_lastAttack)
		{
			m_lastAttack = lastAttack;
			m_kickedAt = Time.time;
		}
		float t = Time.time - m_kickedAt;
		float back = 0f;
		if (t < KickTime)
		{
			back = Mathf.SmoothStep(0f, 1f, t / KickTime);
		}
		else if (t < KickTime + ReturnTime)
		{
			back = 1f - Mathf.SmoothStep(0f, 1f, (t - KickTime) / ReturnTime);
		}
		m_recoil.localPosition = Vector3.back * (back * Distance);
	}
}
