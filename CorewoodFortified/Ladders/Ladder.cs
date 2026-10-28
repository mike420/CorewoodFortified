using System.Collections.Generic;
using UnityEngine;

namespace CorewoodFortified;

// A climbable ladder section: m_height tall from its pivot, rungs across local X, climbable from either face (local ±Z).
// Sections stacked on each other's snap points climb as one run
public class Ladder : MonoBehaviour, Hoverable, Interactable
{
	internal static readonly List<Ladder> All = new List<Ladder>();

	// Set on the prefab: how tall this section is, from its pivot to its top snap points
	public float m_height = 2f;

	// Half the ladder's thickness, from the prefab's collider
	internal const float HalfDepth = 0.06f;

	private const float StackTolerance = 0.1f;

	private ZNetView m_nview;

	private Piece m_piece;

	internal Vector3 Bottom => transform.position;

	internal Vector3 Top => transform.position + transform.up * m_height;

	internal bool IsValid => this != null && isActiveAndEnabled && m_nview != null && m_nview.IsValid() && Upright;

	private bool Upright => Vector3.Dot(transform.up, Vector3.up) > 0.999f;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_piece = GetComponent<Piece>();
	}

	private void OnEnable()
	{
		All.Add(this);
	}

	private void OnDisable()
	{
		All.Remove(this);
	}

	public string GetHoverName()
	{
		return m_piece != null ? Localization.instance.Localize(m_piece.m_name) : "";
	}

	public string GetHoverText()
	{
		string text = m_piece != null ? m_piece.m_name : "";
		if (!IsValid)
		{
			return Localization.instance.Localize(text);
		}
		if (LadderClimber.IsClimbing(Player.m_localPlayer))
		{
			text += "\n[<color=yellow><b>$KEY_Use</b></color>] $text_ladder_let_go"
				+ "\n[<color=yellow><b>$KEY_Jump</b></color>] $text_ladder_jump_off"
				+ "\n[<color=yellow><b>$KEY_Crouch</b></color>] $text_ladder_drop";
		}
		else
		{
			text += "\n[<color=yellow><b>$KEY_Use</b></color>] $text_ladder_climb";
		}
		return Localization.instance.Localize(text);
	}

	public float GetHoverOffset()
	{
		return 0f;
	}

	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (hold || user == null || user != Player.m_localPlayer || !IsValid)
		{
			return false;
		}
		Player player = (Player)user;
		if (LadderClimber.IsClimbing(player))
		{
			LadderClimber.LetGo();
			return true;
		}
		return LadderClimber.TryStart(player, this);
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// This section and every section stacked straight above and below it, lowest first
	internal List<Ladder> Run()
	{
		List<Ladder> run = new List<Ladder> { this };
		Ladder next = this;
		while ((next = next.FindStacked(above: true, run)) != null)
		{
			run.Add(next);
		}
		next = this;
		while ((next = next.FindStacked(above: false, run)) != null)
		{
			run.Insert(0, next);
		}
		return run;
	}

	private Ladder FindStacked(bool above, List<Ladder> exclude)
	{
		Vector3 joint = above ? Top : Bottom;
		foreach (Ladder other in All)
		{
			if (other == null || exclude.Contains(other) || !other.IsValid)
			{
				continue;
			}
			// Faces either way round, but the rungs must run the same way
			if (Mathf.Abs(Vector3.Dot(other.transform.forward, transform.forward)) < 0.99f)
			{
				continue;
			}
			if (Vector3.Distance(above ? other.Bottom : other.Top, joint) <= StackTolerance)
			{
				return other;
			}
		}
		return null;
	}
}
