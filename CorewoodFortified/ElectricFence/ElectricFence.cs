using System.Collections.Generic;
using UnityEngine;

namespace CorewoodFortified;

// An electric fence keeps its beams and damage area under an inactive _enabled child, and turns it on while a
// powered fence controller is in range. Every player's game checks the controllers' shared state itself, and
// plays the switch effect itself as the fence goes live or dead
internal sealed class ElectricFence : MonoBehaviour, Hoverable
{
	private const string EnabledChild = "_enabled";

	private const float CheckInterval = 0.5f;

	public static readonly List<ElectricFence> s_all = new List<ElectricFence>();

	public EffectList m_switchEffect = new EffectList();

	private ZNetView m_nview;
	private Piece m_piece;
	private GameObject m_enabledObject;

	// Controllers can load after the fence, so the first check after loading changes the state silently
	private bool m_checkedOnce;

	private void Awake()
	{
		m_piece = GetComponent<Piece>();
		m_nview = GetComponent<ZNetView>();
		m_enabledObject = TrapEnabler.FindEnabledObject(transform);
		if (!m_nview || !m_nview.IsValid())
		{
			return;
		}
		s_all.Add(this);
		InvokeRepeating(nameof(CheckState), Random.Range(0f, CheckInterval), CheckInterval);
	}

	// The placement ghost shows the areas of the controllers it would be in
	private void Update()
	{
		if (m_nview && m_nview.IsValid())
		{
			return;
		}
		foreach (FenceController controller in FenceController.s_all)
		{
			if (controller && controller.InRange(transform.position))
			{
				controller.ShowAreaMarker();
			}
		}
	}

	private void OnDestroy()
	{
		s_all.Remove(this);
	}

	private void CheckState()
	{
		bool live = IsLive();
		if (m_enabledObject && m_enabledObject.activeSelf != live)
		{
			m_enabledObject.SetActive(live);
			if (m_checkedOnce)
			{
				m_switchEffect.Create(transform.position, transform.rotation);
			}
		}
		m_checkedOnce = true;
	}

	private bool IsLive()
	{
		foreach (FenceController controller in FenceController.s_all)
		{
			if (controller && controller.InRange(transform.position) && controller.IsLive())
			{
				return true;
			}
		}
		return false;
	}

	public string GetHoverText()
	{
		if (!m_piece)
		{
			return "";
		}
		bool inRange = false;
		bool switchedOn = false;
		foreach (FenceController controller in FenceController.s_all)
		{
			if (!controller || !controller.InRange(transform.position))
			{
				continue;
			}
			if (controller.IsLive())
			{
				return Localization.instance.Localize(m_piece.m_name + " ($piece_guardstone_active)");
			}
			inRange = true;
			switchedOn |= !controller.IsTurnedOff();
		}
		string state = !inRange ? "\n$text_fence_needs_controller" : switchedOn ? " ($text_fence_no_power)" : " ($piece_guardstone_inactive)";
		return Localization.instance.Localize(m_piece.m_name + state);
	}

	public string GetHoverName()
	{
		return m_piece ? m_piece.m_name : "";
	}

	public float GetHoverOffset()
	{
		return 0f;
	}

	public static void AddTo(GameObject piece, EffectList switchEffect)
	{
		if (!TrapEnabler.FindEnabledObject(piece.transform))
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " has no '" + EnabledChild + "' child, it will never be live.");
		}
		piece.AddComponent<ElectricFence>().m_switchEffect = switchEffect;
	}
}
