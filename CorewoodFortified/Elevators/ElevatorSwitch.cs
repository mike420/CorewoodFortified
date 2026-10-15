using UnityEngine;

namespace CorewoodFortified;

internal sealed class ElevatorSwitch : MonoBehaviour, Hoverable, Interactable
{
	public Elevator m_elevator;

	private string m_name;

	private Piece m_piece;

	// The pivot of a handle's lever, turned about its X axis (the cog's axle) when the handle is pulled. Null for
	// switches that are not handles
	public Transform m_lever;

	// How far the lever turns when pulled, in degrees, and how long it takes to go over, hold, and come back
	private const float PullAngle = 40f;

	private const float PullTime = 0.2f;

	private const float HoldTime = 0.4f;

	private const float ReturnTime = 0.4f;

	// The stop this switch calls the platform to, or -1 to move on to the next stop
	private int m_stop = -1;

	private Quaternion m_leverRest;

	private void Start()
	{
		if (m_lever)
		{
			m_leverRest = m_lever.localRotation;
		}
		else
		{
			enabled = false;
		}
		m_piece = base.gameObject.GetComponentInParent<Piece>();
		m_name = m_piece.m_name;
		// The platform itself moves on to the next stop; a switch calls it to the floor the switch is on
		if (m_elevator != null && m_elevator.gameObject != base.gameObject)
		{
			Collider[] colliders = GetComponentsInChildren<Collider>();
			if (colliders.Length > 0)
			{
				Bounds bounds = colliders[0].bounds;
				foreach (Collider collider in colliders)
				{
					bounds.Encapsulate(collider.bounds);
				}
				m_stop = m_elevator.GetNearestStop(bounds.center.y);
			}
		}
	}

	public string GetHoverText()
	{
		if (!MainConfig.enableElevators.Value)
		{
			return Localization.instance.Localize(m_name + "\n$text_elevators_disabled");
		}
		return Localization.instance.Localize(m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $text_use_elevator");
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public bool Interact(Humanoid human, bool hold, bool alt)
	{
		if (hold || m_elevator == null)
		{
			return false;
		}
		if (m_elevator.Activate(m_stop) && m_lever)
		{
			m_elevator.SetHandlePulled(m_stop);
		}
		return true;
	}

	// Every client turns the lever from the pull time saved on the elevator, so others see it pulled too
	private void Update()
	{
		float age = m_stop >= 0 && m_elevator != null ? m_elevator.GetHandlePullAge(m_stop) : -1f;
		float pull = 0f;
		if (age >= 0f && age < PullTime)
		{
			pull = Mathf.SmoothStep(0f, 1f, age / PullTime);
		}
		else if (age >= PullTime && age < PullTime + HoldTime)
		{
			pull = 1f;
		}
		else if (age >= PullTime + HoldTime && age < PullTime + HoldTime + ReturnTime)
		{
			pull = Mathf.SmoothStep(1f, 0f, (age - PullTime - HoldTime) / ReturnTime);
		}
		m_lever.localRotation = m_leverRest * Quaternion.Euler(pull * PullAngle, 0f, 0f);
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public float GetHoverOffset()
	{
		return 0f;
	}
}
