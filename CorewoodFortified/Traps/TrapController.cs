using System.Collections.Generic;
using UnityEngine;

namespace CorewoodFortified;

// Switches the traps within its radius on while any character is near it, and off when nobody is. It can be
// turned off by hand, and then its traps stay off until it is turned on again. A trap with no controller in
// range is always on
internal sealed class TrapController : MonoBehaviour, Hoverable, Interactable
{
	public static float Radius => MainConfig.trapControllerRadius.Value;

	private const string KeyActive = "cwf_trap_active";
	private const string KeyTurnedOff = "cwf_trap_turned_off";

	private const float CheckInterval = 0.5f;
	private const float MarkerShowTime = 0.5f;

	public static readonly List<TrapController> s_all = new List<TrapController>();

	private static readonly Collider[] s_overlap = new Collider[1];
	private static int s_characterMask;

	public CircleProjector m_areaMarker;
	public EffectList m_activateEffect = new EffectList();
	public EffectList m_deactivateEffect = new EffectList();

	private ZNetView m_nview;
	private Piece m_piece;
	private GameObject m_onVisual;
	private GameObject m_offVisual;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_piece = GetComponent<Piece>();
		m_onVisual = transform.Find("_enabled")?.gameObject;
		m_offVisual = transform.Find("_disabled")?.gameObject;
		if (m_areaMarker)
		{
			m_areaMarker.m_radius = Radius;
		}
		// The placement ghost keeps its marker showing, so the area it will cover is seen while placing it
		if (!m_nview || !m_nview.IsValid())
		{
			return;
		}
		if (m_areaMarker)
		{
			m_areaMarker.gameObject.SetActive(false);
		}
		if (s_characterMask == 0)
		{
			s_characterMask = LayerMask.GetMask("character");
		}
		m_nview.Register("CWF_ToggleTrapController", RPC_Toggle);
		s_all.Add(this);
		UpdateVisuals();
		InvokeRepeating(nameof(CheckState), Random.Range(0f, CheckInterval), CheckInterval);
	}

	// The radius is a synced setting, so the marker follows it while it shows
	private void Update()
	{
		if (m_areaMarker && m_areaMarker.gameObject.activeSelf)
		{
			m_areaMarker.m_radius = Radius;
		}
	}

	private void OnDestroy()
	{
		s_all.Remove(this);
	}

	// The owner decides whether the controller is active and stores it, so every player sees the same traps on
	private void CheckState()
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		if (m_nview.IsOwner())
		{
			bool active = MainConfig.enableTraps.Value && !IsTurnedOff() && Physics.OverlapSphereNonAlloc(transform.position, Radius, s_overlap, s_characterMask) > 0;
			if (active != IsActive())
			{
				m_nview.GetZDO().Set(KeyActive, active);
			}
		}
		UpdateVisuals();
	}

	private void UpdateVisuals()
	{
		bool active = IsActive();
		if (m_onVisual)
		{
			m_onVisual.SetActive(active);
		}
		if (m_offVisual)
		{
			m_offVisual.SetActive(!active);
		}
	}

	public bool IsActive()
	{
		return m_nview && m_nview.IsValid() && m_nview.GetZDO().GetBool(KeyActive);
	}

	public bool IsTurnedOff()
	{
		return m_nview && m_nview.IsValid() && m_nview.GetZDO().GetBool(KeyTurnedOff);
	}

	public bool InRange(Vector3 position)
	{
		return Utils.DistanceXZ(position, transform.position) <= Radius;
	}

	private void RPC_Toggle(long sender)
	{
		if (!m_nview.IsOwner())
		{
			return;
		}
		bool turnedOff = !IsTurnedOff();
		m_nview.GetZDO().Set(KeyTurnedOff, turnedOff);
		(turnedOff ? m_deactivateEffect : m_activateEffect).Create(transform.position, transform.rotation);
		CheckState();
	}

	public string GetHoverText()
	{
		ShowAreaMarker();
		string name = GetHoverName();
		if (!MainConfig.enableTraps.Value)
		{
			return Localization.instance.Localize(name + "\n$text_traps_disabled");
		}
		if (!PrivateArea.CheckAccess(transform.position, 0f, flash: false))
		{
			return Localization.instance.Localize(name + "\n$piece_noaccess");
		}
		string state = IsTurnedOff() ? "$text_trap_controller_off" : (IsActive() ? "$piece_guardstone_active" : "$text_trap_controller_waiting");
		string action = IsTurnedOff() ? "$piece_guardstone_activate" : "$piece_guardstone_deactivate";
		return Localization.instance.Localize(name + " (" + state + ")\n[<color=yellow><b>$KEY_Use</b></color>] " + action);
	}

	public string GetHoverName()
	{
		return m_piece ? m_piece.m_name : "";
	}

	public float GetHoverOffset()
	{
		return 0f;
	}

	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (hold)
		{
			return true;
		}
		if (!MainConfig.enableTraps.Value)
		{
			user.Message(MessageHud.MessageType.Center, "$text_traps_disabled");
			return true;
		}
		if (!PrivateArea.CheckAccess(transform.position))
		{
			return true;
		}
		user.Message(MessageHud.MessageType.Center, IsTurnedOff() ? "$text_trap_controller_turned_on" : "$text_trap_controller_turned_off");
		m_nview.InvokeRPC("CWF_ToggleTrapController");
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public void ShowAreaMarker()
	{
		if (!m_areaMarker)
		{
			return;
		}
		m_areaMarker.gameObject.SetActive(true);
		CancelInvoke(nameof(HideAreaMarker));
		Invoke(nameof(HideAreaMarker), MarkerShowTime);
	}

	private void HideAreaMarker()
	{
		if (m_areaMarker)
		{
			m_areaMarker.gameObject.SetActive(false);
		}
	}
}
