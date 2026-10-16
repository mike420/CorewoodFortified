using UnityEngine;

namespace CorewoodFortified;

// Sits on a trap's Trigger collider and tells the trap whenever a character is inside it. Trigger
// messages go to the collider's own object, so this cannot live on the trap's root
internal sealed class TrapProximity : MonoBehaviour
{
	private TrapEnabler m_trap;

	private void Awake()
	{
		m_trap = GetComponentInParent<TrapEnabler>();
	}

	private void OnTriggerEnter(Collider other)
	{
		Check(other);
	}

	private void OnTriggerStay(Collider other)
	{
		Check(other);
	}

	private void Check(Collider other)
	{
		if (!m_trap)
		{
			return;
		}
		Rigidbody body = other.attachedRigidbody;
		if (body ? body.GetComponent<Character>() : other.GetComponentInParent<Character>())
		{
			m_trap.OnCharacterNear();
		}
	}
}
