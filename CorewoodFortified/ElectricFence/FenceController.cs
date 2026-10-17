using System.Collections.Generic;
using UnityEngine;

namespace CorewoodFortified;

// Powers the electric fences within its radius. It burns Eitr from its own store while it is switched on and has a
// fence in range; the fences are live only while it has power. Use opens the store, which takes nothing but Eitr,
// and the alternative use switches it on and off. The owner decides and stores the state, so every player sees
// the same fences live
internal sealed class FenceController : MonoBehaviour, Hoverable, Interactable
{
	public static float Radius => MainConfig.fenceControllerRadius.Value;

	// The store's name, which is also how the Eitr-only rule knows its inventory
	public const string ContainerName = "$piece_mm_corewood_fence_controller";

	private const string EitrName = "$item_eitr";

	private const string KeyLive = "cwf_fence_live";
	private const string KeyTurnedOff = "cwf_fence_turned_off";
	private const string KeyPower = "cwf_fence_power";

	private const float CheckInterval = 0.5f;

	public static readonly List<FenceController> s_all = new List<FenceController>();

	public EffectList m_activateEffect = new EffectList();
	public EffectList m_deactivateEffect = new EffectList();

	private ZNetView m_nview;
	private Piece m_piece;
	private Container m_container;
	private GameObject m_onVisual;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_piece = GetComponent<Piece>();
		m_container = GetComponentInChildren<Container>(includeInactive: true);
		m_onVisual = transform.Find("_enabled")?.gameObject;
		if (!m_nview || !m_nview.IsValid())
		{
			return;
		}
		m_nview.Register("CWF_ToggleFenceController", RPC_Toggle);
		s_all.Add(this);
		UpdateVisuals();
		InvokeRepeating(nameof(CheckState), Random.Range(0f, CheckInterval), CheckInterval);
	}

	private void OnDestroy()
	{
		s_all.Remove(this);
	}

	// Power is kept in seconds. When it runs out the next Eitr is taken from the store
	private void CheckState()
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		if (m_nview.IsOwner())
		{
			ZDO zdo = m_nview.GetZDO();
			float power = zdo.GetFloat(KeyPower);
			bool wanted = !IsTurnedOff() && HasFenceInRange();
			if (wanted && power <= 0f && TakeEitr())
			{
				power += MainConfig.fenceEitrMinutes.Value * 60f;
			}
			bool live = wanted && power > 0f;
			if (live)
			{
				power = Mathf.Max(0f, power - CheckInterval);
			}
			if (!Mathf.Approximately(power, zdo.GetFloat(KeyPower)))
			{
				zdo.Set(KeyPower, power);
			}
			if (live != IsLive())
			{
				zdo.Set(KeyLive, live);
			}
		}
		UpdateVisuals();
	}

	private bool HasFenceInRange()
	{
		foreach (ElectricFence fence in ElectricFence.s_all)
		{
			if (fence && InRange(fence.transform.position))
			{
				return true;
			}
		}
		return false;
	}

	private bool TakeEitr()
	{
		Inventory inventory = m_container ? m_container.GetInventory() : null;
		if (inventory == null)
		{
			return false;
		}
		foreach (ItemDrop.ItemData item in inventory.GetAllItems())
		{
			if (IsEitr(item))
			{
				return inventory.RemoveOneItem(item);
			}
		}
		return false;
	}

	private int CountEitr()
	{
		Inventory inventory = m_container ? m_container.GetInventory() : null;
		return inventory != null ? inventory.CountItems(EitrName) : 0;
	}

	public static bool IsEitr(ItemDrop.ItemData item)
	{
		return item != null && item.m_shared.m_name == EitrName;
	}

	private void UpdateVisuals()
	{
		if (m_onVisual)
		{
			m_onVisual.SetActive(IsLive());
		}
	}

	public bool IsLive()
	{
		return m_nview && m_nview.IsValid() && m_nview.GetZDO().GetBool(KeyLive);
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
		string name = GetHoverName();
		if (!PrivateArea.CheckAccess(transform.position, 0f, flash: false))
		{
			return Localization.instance.Localize(name + "\n$piece_noaccess");
		}
		string state = IsTurnedOff() ? "$text_trap_controller_off" : IsLive() ? "$piece_guardstone_active" : "$text_fence_no_power";
		string action = IsTurnedOff() ? "$piece_guardstone_activate" : "$piece_guardstone_deactivate";
		int eitr = CountEitr();
		return Localization.instance.Localize(name + " (" + state + ")\n" + EitrName + ": " + eitr +
			"\n" + PowerLine(eitr) +
			"\n[<color=yellow><b>$KEY_Use</b></color>] $piece_container_open" +
			"\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] " + action);
	}

	// How long the power left and the Eitr in the store last, in the look of the HoverInfo mod's fuel line. It only
	// counts down while the fences are live
	private string PowerLine(int eitr)
	{
		float seconds = m_nview.GetZDO().GetFloat(KeyPower) + eitr * MainConfig.fenceEitrMinutes.Value * 60f;
		if (seconds <= 0f)
		{
			return "<color=#FF8080>$text_fence_no_power</color>";
		}
		return "<color=#A8D8FF>$text_fence_power_lasts " + Duration(seconds) + "</color>";
	}

	private static string Duration(float seconds)
	{
		int total = Mathf.CeilToInt(seconds);
		int hours = total / 3600;
		int minutes = total % 3600 / 60;
		if (hours > 0)
		{
			return hours + "h " + minutes.ToString("00") + "m";
		}
		return minutes > 0 ? minutes + "m " + (total % 60).ToString("00") + "s" : total % 60 + "s";
	}

	public string GetHoverName()
	{
		return m_piece ? m_piece.m_name : "";
	}

	public float GetHoverOffset()
	{
		return 0f;
	}

	// The store sits on a child, which the player's aim never finds, so it is opened from here
	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (!alt)
		{
			return m_container && m_container.Interact(user, hold, alt);
		}
		if (!PrivateArea.CheckAccess(transform.position))
		{
			return true;
		}
		user.Message(MessageHud.MessageType.Center, IsTurnedOff() ? "$text_fence_controller_turned_on" : "$text_fence_controller_turned_off");
		m_nview.InvokeRPC("CWF_ToggleFenceController");
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// The store on the child gets its network object from the root, and is named so the Eitr-only rule finds it
	public static void AddTo(GameObject piece, EffectList activate, EffectList deactivate)
	{
		Container container = piece.GetComponentInChildren<Container>(includeInactive: true);
		if (!container)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " has no Container, it cannot hold Eitr.");
		}
		else
		{
			container.m_rootObjectOverride = piece.GetComponent<ZNetView>();
			container.m_name = ContainerName;
			container.m_privacy = Container.PrivacySetting.Public;
		}
		FenceController controller = piece.AddComponent<FenceController>();
		controller.m_activateEffect = activate;
		controller.m_deactivateEffect = deactivate;
	}
}
