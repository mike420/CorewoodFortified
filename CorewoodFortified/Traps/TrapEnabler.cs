using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace CorewoodFortified;

// A trap keeps its moving parts and damage area under an inactive _enabled child, so the placement ghost
// stays still and harmless. A placed trap is armed while it has no trap controller in range, or while any
// controller in range is active, and never while traps are turned off in the config; the trap's owner decides
// and stores it, so every player sees the same. An armed trap turns _enabled on only while a character is inside
// its Trigger, and for a moment after. _enabled can sit anywhere in the prefab, such as on a moving blade. An
// Animator outside _enabled with an "Enabled" bool is told the same, so it can play a running and a resting animation
internal sealed class TrapEnabler : MonoBehaviour, Hoverable
{
	private const string EnabledChild = "_enabled";
	private const string ProximityChild = "Trigger";
	private const string KeyEnabled = "cwf_trap_enabled";

	private static readonly int AnimEnabled = Animator.StringToHash("Enabled");

	private const float CheckInterval = 0.5f;

	// How long the trap keeps going after the last character leaves its sphere, so it does not flicker
	private const float NearHoldTime = 2f;

	public EffectList m_activateEffect = new EffectList();
	public EffectList m_deactivateEffect = new EffectList();

	private ZNetView m_nview;
	private Piece m_piece;
	private GameObject m_enabledObject;

	private readonly List<Animator> m_animators = new List<Animator>();

	// Controllers can load after the trap, so the first check after loading changes the state silently
	private bool m_checkedOnce;

	// A trap without a Trigger runs the whole time it is armed
	private bool m_hasProximity;
	private float m_lastNearTime = float.NegativeInfinity;

	private void Awake()
	{
		m_piece = GetComponent<Piece>();
		m_nview = GetComponent<ZNetView>();
		m_enabledObject = FindEnabledObject(transform);
		m_hasProximity = GetComponentInChildren<TrapProximity>(includeInactive: true);
		if (!m_nview || !m_nview.IsValid())
		{
			return;
		}
		UpdateEnabledObject();
		InvokeRepeating(nameof(CheckState), Random.Range(0f, CheckInterval), CheckInterval);
	}

	// Animators only show their parameters once they are running, so they are looked at here rather than in Awake
	private void Start()
	{
		if (!m_nview || !m_nview.IsValid())
		{
			return;
		}
		foreach (Animator animator in GetComponentsInChildren<Animator>())
		{
			if (m_enabledObject && animator.transform.IsChildOf(m_enabledObject.transform))
			{
				continue;
			}
			foreach (AnimatorControllerParameter parameter in animator.parameters)
			{
				if (parameter.nameHash == AnimEnabled && parameter.type == AnimatorControllerParameterType.Bool)
				{
					m_animators.Add(animator);
					break;
				}
			}
		}
		UpdateEnabledObject();
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
		foreach (Animator animator in m_animators)
		{
			if (animator && animator.isActiveAndEnabled && animator.GetBool(AnimEnabled) != enabled)
			{
				animator.SetBool(AnimEnabled, enabled);
			}
		}
	}

	// The first _enabled found anywhere under the trap
	public static GameObject FindEnabledObject(Transform root)
	{
		foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
		{
			if (child != root && child.name == EnabledChild)
			{
				return child.gameObject;
			}
		}
		return null;
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
	public static void AddTo(GameObject piece, EffectList activate, EffectList deactivate, ConfigEntry<float> triggerRadius)
	{
		if (!FindEnabledObject(piece.transform))
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " has no '" + EnabledChild + "' child, it will not work as a trap.");
			return;
		}
		Transform proximity = piece.transform.Find(ProximityChild);
		if (proximity && proximity.GetComponent<Collider>())
		{
			TrapProximity.AddTo(proximity.gameObject, piece.name, triggerRadius);
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
