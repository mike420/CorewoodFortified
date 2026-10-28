using System.Collections.Generic;
using HarmonyLib;
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

	// How near the player must be to get on, from their middle to the nearest point up the ladder
	private const float UseDistance = 1.5f;

	private ZNetView m_nview;

	private Piece m_piece;

	internal Vector3 Bottom => transform.position;

	internal Vector3 Top => transform.position + transform.up * m_height;

	internal ZDOID ZdoId => m_nview != null && m_nview.IsValid() ? m_nview.GetZDO().m_uid : ZDOID.None;

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
		if (!MainConfig.enableLadders.Value)
		{
			return Localization.instance.Localize(text + "\n$text_ladders_disabled");
		}
		if (!IsValid)
		{
			return Localization.instance.Localize(text);
		}
		bool climbing = LadderClimber.IsClimbing(Player.m_localPlayer);
		if (!climbing && !InReach(Player.m_localPlayer))
		{
			return Localization.instance.Localize(text);
		}
		if (climbing)
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
		if (hold || user == null || user != Player.m_localPlayer || !IsValid || !MainConfig.enableLadders.Value)
		{
			return false;
		}
		Player player = (Player)user;
		if (LadderClimber.IsClimbing(player))
		{
			LadderClimber.LetGo();
			return true;
		}
		return InReach(player) && LadderClimber.TryStart(player, this);
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private bool InReach(Player player)
	{
		if (player == null)
		{
			return false;
		}
		Vector3 middle = player.transform.position + Vector3.up * 1f;
		Vector3 up = Top - Bottom;
		float along = Mathf.Clamp01(Vector3.Dot(middle - Bottom, up) / up.sqrMagnitude);
		return Vector3.Distance(middle, Bottom + up * along) <= UseDistance;
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

	// Hides the ladders from the build menu while ladders are turned off; placed ones stay but cannot be climbed
	[HarmonyPatch(typeof(PieceTable), nameof(PieceTable.UpdateAvailable))]
	private static class PieceTableUpdateAvailablePatch
	{
		private static void Postfix(PieceTable __instance)
		{
			if (MainConfig.enableLadders.Value)
			{
				return;
			}
			__instance.m_availablePieces.RemoveWhere(IsLadder);
			__instance.m_enabledPieces.RemoveWhere(IsLadder);
			foreach (List<Piece> category in __instance.m_availablePiecesByCategory)
			{
				category.RemoveAll(IsLadder);
			}
		}
	}

	private static bool IsLadder(Piece piece)
	{
		return piece != null && piece.GetComponent<Ladder>() != null;
	}
}
