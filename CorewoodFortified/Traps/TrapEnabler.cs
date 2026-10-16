using UnityEngine;

namespace CorewoodFortified;

// A trap keeps its moving parts and damage area under an inactive _enabled child, so the placement ghost
// stays still and harmless. A placed trap is armed while it has no trap controller in range, or while any
// controller in range is active, and never while traps are turned off in the config; the trap's owner decides
// and stores it, so every player sees the same. An armed trap turns _enabled on only while a character is inside
// its Trigger, and for a moment after
internal sealed class TrapEnabler : MonoBehaviour, Hoverable
{
	private const string EnabledChild = "_enabled";
	private const string ProximityChild = "Trigger";
	private const string KeyEnabled = "cwf_trap_enabled";

	private const float CheckInterval = 0.5f;

	// How long the trap keeps going after the last character leaves its sphere, so it does not flicker
	private const float NearHoldTime = 2f;

	public EffectList m_activateEffect = new EffectList();
	public EffectList m_deactivateEffect = new EffectList();

	private ZNetView m_nview;
	private Piece m_piece;
	private GameObject m_enabledObject;

	// Controllers can load after the trap, so the first check after loading changes the state silently
	private bool m_checkedOnce;

	// A trap without a Trigger runs the whole time it is armed
	private bool m_hasProximity;
	private float m_lastNearTime = float.NegativeInfinity;

	private void Awake()
	{
		m_piece = GetComponent<Piece>();
		m_nview = GetComponent<ZNetView>();
		m_enabledObject = transform.Find(EnabledChild)?.gameObject;
		m_hasProximity = GetComponentInChildren<TrapProximity>(includeInactive: true);
		if (!m_nview || !m_nview.IsValid())
		{
			return;
		}
		UpdateEnabledObject();
		InvokeRepeating(nameof(CheckState), Random.Range(0f, CheckInterval), CheckInterval);
	}

	// The placement ghost shows the areas of the controllers it would be in
	private void Update()
	{
		if (m_nview && m_nview.IsValid())
		{
			return;
		}
		foreach (TrapController controller in TrapController.s_all)
		{
			if (controller && controller.InRange(transform.position))
			{
				controller.ShowAreaMarker();
			}
		}
	}

	private void CheckState()
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		if (m_nview.IsOwner())
		{
			bool enabled = ShouldBeEnabled();
			if (enabled != IsEnabled())
			{
				m_nview.GetZDO().Set(KeyEnabled, enabled);
				if (m_checkedOnce)
				{
					(enabled ? m_activateEffect : m_deactivateEffect).Create(transform.position, transform.rotation);
				}
			}
		}
		m_checkedOnce = true;
		UpdateEnabledObject();
	}

	private bool ShouldBeEnabled()
	{
		if (!MainConfig.enableTraps.Value)
		{
			return false;
		}
		bool anyInRange = false;
		foreach (TrapController controller in TrapController.s_all)
		{
			if (!controller || !controller.InRange(transform.position))
			{
				continue;
			}
			if (controller.IsActive())
			{
				return true;
			}
			anyInRange = true;
		}
		return !anyInRange;
	}

	private bool IsEnabled()
	{
		return m_nview && m_nview.IsValid() && m_nview.GetZDO().GetBool(KeyEnabled);
	}

	// Called by TrapProximity every physics step a character is inside the Trigger. Each player's game checks its
	// own Trigger, so the trap springs at once rather than waiting for the owner
	public void OnCharacterNear()
	{
		if (!m_nview || !m_nview.IsValid())
		{
			return;
		}
		m_lastNearTime = Time.time;
		UpdateEnabledObject();
	}

	private void UpdateEnabledObject()
	{
		bool enabled = IsEnabled() && (!m_hasProximity || Time.time - m_lastNearTime < NearHoldTime);
		if (m_enabledObject && m_enabledObject.activeSelf != enabled)
		{
			m_enabledObject.SetActive(enabled);
		}
	}

	public string GetHoverText()
	{
		if (!m_piece)
		{
			return "";
		}
		if (!MainConfig.enableTraps.Value)
		{
			return Localization.instance.Localize(m_piece.m_name + "\n$text_traps_disabled");
		}
		return Localization.instance.Localize(m_piece.m_name + " (" + (IsEnabled() ? "$piece_guardstone_active" : "$piece_guardstone_inactive") + ")");
	}

	public string GetHoverName()
	{
		return m_piece ? m_piece.m_name : "";
	}

	public float GetHoverOffset()
	{
		return 0f;
	}

	// Checks the prefab once when it is added, so a missing child shows in the log rather than as a trap that
	// silently does nothing
	public static void AddTo(GameObject piece, EffectList activate, EffectList deactivate)
	{
		if (!piece.transform.Find(EnabledChild))
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " has no '" + EnabledChild + "' child, it will not work as a trap.");
			return;
		}
		Transform proximity = piece.transform.Find(ProximityChild);
		if (proximity && proximity.GetComponent<Collider>())
		{
			proximity.gameObject.AddComponent<TrapProximity>();
		}
		else
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " has no '" + ProximityChild + "' trigger, it will run the whole time it is on.");
		}
		TrapEnabler enabler = piece.AddComponent<TrapEnabler>();
		enabler.m_activateEffect = activate;
		enabler.m_deactivateEffect = deactivate;
	}
}
