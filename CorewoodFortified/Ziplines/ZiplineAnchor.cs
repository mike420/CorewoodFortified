using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CorewoodFortified;

// The zipline post: holds ropes, takes a rope to another post and starts a ride
public class ZiplineAnchor : MonoBehaviour, Hoverable, Interactable
{
	internal static readonly List<ZiplineAnchor> All = new List<ZiplineAnchor>();

	internal const string GearName = "zipline_gear";

	// Where ropes are tied, set from the prefab's RopePoint child
	public Vector3 m_localRopePoint = new Vector3(0f, 4.495f, 0f);

	private ZNetView m_nview;

	private WearNTear m_wearNTear;

	private Piece m_piece;

	private bool m_registered;

	private ZDOID m_zdoid = ZDOID.None;

	private Vector3 m_knownPoint;

	private Transform m_gear;

	private float m_nextAim;

	private static readonly List<ZiplineRope> s_ropes = new List<ZiplineRope>();

	private static readonly List<ZiplineRope> s_toRemove = new List<ZiplineRope>();

	private static readonly List<ZiplineRope> s_count = new List<ZiplineRope>();

	private static readonly List<ZiplineRope> s_moved = new List<ZiplineRope>();

	private static readonly List<ZiplineRope> s_aim = new List<ZiplineRope>();

	private static readonly StringBuilder s_text = new StringBuilder();

	internal Vector3 RopePoint => transform.TransformPoint(m_localRopePoint);

	internal ZDOID ZdoId => m_zdoid;

	internal bool IsValid => m_registered && m_nview != null && m_nview.IsValid();

	private string Name => m_piece != null ? m_piece.m_name : "$piece_mm_corewood_zipline_post";

	private void Start()
	{
		m_nview = GetComponent<ZNetView>();
		m_piece = GetComponent<Piece>();
		if (m_nview == null || !m_nview.IsValid())
		{
			return;
		}
		All.Add(this);
		m_registered = true;
		m_zdoid = m_nview.GetZDO().m_uid;
		m_wearNTear = GetComponent<WearNTear>();
		if (m_wearNTear != null)
		{
			m_wearNTear.m_onDestroyed += OnPostDestroyed;
		}
		m_knownPoint = RopePoint;
		m_gear = Utils.FindChild(transform, GearName);
	}

	// Moving the post (with a mod that allows it) takes its ropes along, or down if they no longer fit
	private void CheckMoved()
	{
		if (!IsValid)
		{
			return;
		}
		Vector3 point = RopePoint;
		if ((point - m_knownPoint).sqrMagnitude < 0.0025f)
		{
			return;
		}
		Vector3 previous = m_knownPoint;
		m_knownPoint = point;
		if (RopeCarry.Active && RopeCarry.From == this)
		{
			RopeCarry.FromPoint = point;
		}
		if (!m_nview.IsOwner())
		{
			return;
		}
		s_moved.Clear();
		foreach (ZiplineRope rope in ZiplineRope.All)
		{
			if (rope != null && rope.Path != null && rope.Path.HasEnd(previous, ZiplineRope.EndTolerance))
			{
				s_moved.Add(rope);
			}
		}
		foreach (ZiplineRope rope in s_moved)
		{
			Vector3 other = rope.Path.OtherEnd(previous);
			if (Fits(point, other, rope.SagFactor, out string problem))
			{
				rope.MoveEnd(previous, point);
				continue;
			}
			rope.DestroyRope();
			ZiplineMessages.LogInfo("A moved zipline post's rope no longer fits and was taken down: " + problem);
			Player player = Player.m_localPlayer;
			if (player != null && Vector3.Distance(player.transform.position, point) < 40f)
			{
				ZiplineMessages.Say(player, LocalisedText.Get("text_zipline_rope_taken_down", problem), LocalisedText.Get("text_zipline_rope_taken_down_short", problem));
			}
		}
		s_moved.Clear();
	}

	internal static bool Fits(Vector3 a, Vector3 b, float sag, out string problem)
	{
		problem = null;
		float length = Vector3.Distance(a, b);
		if (length < 4f)
		{
			problem = LocalisedText.Get("text_zipline_too_close");
			return false;
		}
		float maxLength = MainConfig.ziplineMaxRopeLength.Value;
		if (length > maxLength)
		{
			problem = LocalisedText.Get("text_zipline_too_far", length.ToString("0"), maxLength.ToString("0"));
			return false;
		}
		float slope = Mathf.Asin(Mathf.Clamp01(Mathf.Abs(b.y - a.y) / length)) * Mathf.Rad2Deg;
		if (slope > MainConfig.ziplineMaxSlope.Value)
		{
			problem = LocalisedText.Get("text_zipline_too_steep", slope.ToString("0"), MainConfig.ziplineMaxSlope.Value.ToString("0"));
			return false;
		}
		if (MainConfig.ziplineRequireClearance.Value && !RopeCarry.CheckClearance(new RopePath(a, b, sag), out string blocked))
		{
			problem = string.IsNullOrEmpty(blocked) ? LocalisedText.Get("text_zipline_in_the_way") : blocked;
			return false;
		}
		return true;
	}

	private void Update()
	{
		if (!m_registered || Time.time < m_nextAim)
		{
			return;
		}
		m_nextAim = Time.time + 0.5f;
		try
		{
			CheckMoved();
		}
		catch (Exception ex)
		{
			ZiplineMessages.LogDebug("Moved zipline post check failed: " + ex.Message);
		}
		try
		{
			AimGear();
		}
		catch (Exception ex)
		{
			ZiplineMessages.LogDebug("Zipline pulley aim failed: " + ex.Message);
		}
	}

	// The pulley turns towards the player carrying a rope from it, otherwise along its first rope
	private void AimGear()
	{
		if (m_gear == null)
		{
			return;
		}
		Vector3 point = RopePoint;
		Vector3 direction = Vector3.zero;
		if (RopeCarry.IsSource(this) && Player.m_localPlayer != null)
		{
			direction = Player.m_localPlayer.transform.position - point;
			m_nextAim = Time.time + 0.1f;
		}
		else
		{
			List<ZiplineRope> ropes = GetRopes(s_aim);
			if (ropes.Count > 0 && ropes[0].Path != null)
			{
				direction = ropes[0].Path.OtherEnd(point) - point;
			}
		}
		direction.y = 0f;
		if (direction.sqrMagnitude >= 0.01f)
		{
			m_gear.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
		}
	}

	private void OnDestroy()
	{
		if (m_registered)
		{
			All.Remove(this);
		}
		if (m_wearNTear != null)
		{
			m_wearNTear.m_onDestroyed -= OnPostDestroyed;
		}
		if (!m_registered || !RopeCarry.IsSource(this))
		{
			return;
		}
		// Unloading also destroys the object; only a post whose ZDO is gone was really taken down
		bool removed = false;
		try
		{
			removed = ZDOMan.instance != null && !m_zdoid.IsNone() && ZDOMan.instance.GetZDO(m_zdoid) == null;
		}
		catch (Exception ex)
		{
			ZiplineMessages.LogWarning("Checking a removed zipline post failed: " + ex.Message);
		}
		if (removed)
		{
			RopeCarry.SourceDestroyed(this);
		}
		else
		{
			RopeCarry.SourceUnloaded(this);
		}
	}

	private void OnPostDestroyed()
	{
		try
		{
			RopeCarry.SourceDestroyed(this);
		}
		catch (Exception ex)
		{
			ZiplineMessages.LogWarning("Dropping the carried rope from a destroyed zipline post failed: " + ex.Message);
		}
		try
		{
			GetRopes(s_toRemove);
			foreach (ZiplineRope rope in s_toRemove)
			{
				rope.DestroyRope();
			}
			s_toRemove.Clear();
		}
		catch (Exception ex)
		{
			ZiplineMessages.LogWarning("Removing ropes from a destroyed zipline post failed: " + ex.Message);
		}
	}

	internal static ZiplineAnchor FindAt(Vector3 point, float tolerance)
	{
		float squared = tolerance * tolerance;
		foreach (ZiplineAnchor anchor in All)
		{
			if (anchor != null && anchor.IsValid && (anchor.RopePoint - point).sqrMagnitude <= squared)
			{
				return anchor;
			}
		}
		return null;
	}

	internal List<ZiplineRope> GetRopes(List<ZiplineRope> result)
	{
		result.Clear();
		Vector3 point = RopePoint;
		foreach (ZiplineRope rope in ZiplineRope.All)
		{
			if (rope != null && rope.Path != null && rope.Path.HasEnd(point, ZiplineRope.EndTolerance))
			{
				result.Add(rope);
			}
		}
		return result;
	}

	internal int RopeCount()
	{
		return GetRopes(s_count).Count;
	}

	// The rope that runs closest to the way the camera is looking
	internal ZiplineRope PickRope(List<ZiplineRope> ropes, out int index)
	{
		index = -1;
		if (ropes.Count == 0)
		{
			return null;
		}
		Vector3 look = GameCamera.instance != null ? GameCamera.instance.transform.forward : (Player.m_localPlayer != null ? Player.m_localPlayer.transform.forward : Vector3.forward);
		look.y = 0f;
		if (look.sqrMagnitude < 0.0001f)
		{
			look = Vector3.forward;
		}
		look.Normalize();
		Vector3 point = RopePoint;
		float best = float.MinValue;
		for (int i = 0; i < ropes.Count; i++)
		{
			Vector3 direction = ropes[i].Path.OtherEnd(point) - point;
			direction.y = 0f;
			float score = direction.sqrMagnitude > 0.0001f ? Vector3.Dot(direction.normalized, look) : -2f;
			if (score > best)
			{
				best = score;
				index = i;
			}
		}
		return index < 0 ? null : ropes[index];
	}

	public string GetHoverName()
	{
		return Localization.instance.Localize(Name);
	}

	public float GetHoverOffset()
	{
		return 0.5f;
	}

	public string GetHoverText()
	{
		try
		{
			s_text.Length = 0;
			s_text.Append(Name);
			if (!MainConfig.enableZiplines.Value)
			{
				s_text.Append("\n$text_ziplines_disabled");
				return Localization.instance.Localize(s_text.ToString());
			}
			if (!IsValid)
			{
				return Localization.instance.Localize(s_text.ToString());
			}
			if (RopeCarry.Active)
			{
				if (RopeCarry.IsSource(this))
				{
					s_text.Append("\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $text_zipline_put_back");
				}
				else if (RopeCarry.CanTieTo(this, out float length, out string problem))
				{
					s_text.Append("\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] " + LocalisedText.Get("text_zipline_tie_here", length.ToString("0")));
				}
				else
				{
					s_text.Append("\n<color=#ff9a7a>" + problem + "</color>");
				}
				return Localization.instance.Localize(s_text.ToString());
			}
			List<ZiplineRope> ropes = GetRopes(s_ropes);
			Vector3 point = RopePoint;
			ZiplineRope picked = PickRope(ropes, out int index);
			if (picked != null)
			{
				float rise = picked.Path.OtherEnd(point).y - point.y;
				string slope = Mathf.Abs(rise) < 1f ? LocalisedText.Get("text_zipline_level") : LocalisedText.Get(rise < 0f ? "text_zipline_down" : "text_zipline_up", Mathf.Abs(rise).ToString("0"));
				s_text.Append("\n[<color=yellow><b>$KEY_Use</b></color>] " + LocalisedText.Get("text_zipline_ride", picked.Path.Length.ToString("0"), slope));
				if (ropes.Count > 1)
				{
					s_text.Append("\n<color=#c0c0c0>" + LocalisedText.Get("text_zipline_rope_of", (index + 1).ToString(), ropes.Count.ToString()) + "</color>");
				}
			}
			int maxRopes = MainConfig.ziplineMaxRopesPerPost.Value;
			if (ropes.Count < maxRopes)
			{
				s_text.Append("\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $text_zipline_take_rope");
			}
			else
			{
				s_text.Append("\n<color=#c0c0c0>" + (maxRopes == 1 ? LocalisedText.Get("text_zipline_slot_used") : LocalisedText.Get("text_zipline_slots_used", maxRopes.ToString())) + "</color>");
			}
			return Localization.instance.Localize(s_text.ToString());
		}
		catch (Exception ex)
		{
			ZiplineMessages.LogDebug("Zipline hover text failed: " + ex.Message);
			return Localization.instance.Localize(Name);
		}
	}

	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		Player player = user as Player;
		if (player == null || player != Player.m_localPlayer || !IsValid || !MainConfig.enableZiplines.Value)
		{
			return false;
		}
		try
		{
			if (alt)
			{
				return RopeCarry.AltInteract(this, player);
			}
			if (RopeCarry.Active)
			{
				if (RopeCarry.IsSource(this))
				{
					ZiplineMessages.Say(player, "$text_zipline_holding_this", "$text_zipline_holding_this_short");
				}
				else
				{
					ZiplineMessages.Say(player, "$text_zipline_tie_hint", "$text_zipline_tie_hint_short");
				}
				return false;
			}
			ZiplineRope rope = PickRope(GetRopes(s_ropes), out _);
			if (rope == null)
			{
				ZiplineMessages.Say(player, "$text_zipline_no_rope", "$text_zipline_no_rope_short");
				return false;
			}
			if (!ZiplineRider.TryStart(player, this, rope, out string error) && !string.IsNullOrEmpty(error))
			{
				ZiplineMessages.Say(player, error, error);
			}
			return false;
		}
		catch (Exception ex)
		{
			ZiplineMessages.LogWarning("Zipline interaction failed: " + ex);
			return false;
		}
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}
}
