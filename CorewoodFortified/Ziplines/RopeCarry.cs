using System;
using UnityEngine;

namespace CorewoodFortified;

// The rope a player has taken from a post and is carrying to another one, drawn from the post to their hand
public class RopeCarry : MonoBehaviour
{
	internal enum Verdict
	{
		Keep,
		NoPlayer,
		Dead,
		Teleported,
		SourceGone,
		TooFar,
		Attached,
		LetGo,
		SourceMoved
	}

	private struct Snapshot
	{
		public bool HasPlayer;

		public bool Dead;

		public bool Teleporting;

		public bool Attached;

		public float Distance;

		public float MaxLength;

		public bool SourceDestroyed;

		public bool SourceChecked;

		public bool SourceRemoved;

		public bool SourceLoaded;

		public bool SourceMoved;

		public bool AreaReady;

		public int MissesInARow;

		public bool LetGo;
	}

	internal static bool Active;

	internal static ZiplineAnchor From;

	internal static Vector3 FromPoint;

	private const float SlackMetres = 1f;

	private const float LetGoHoldSeconds = 1f;

	private const float SourceCheckInterval = 0.25f;

	private const int FailuresBeforeDrop = 30;

	private static RopeTube s_tube;

	private static readonly Vector3[] s_points = new Vector3[20];

	private static ZiplineAnchor s_checkedTarget;

	private static float s_checkedTime;

	private static bool s_checkedOk;

	private static float s_checkedLength;

	private static string s_checkedProblem;

	private static int s_clearanceMask = -1;

	private static float s_warnTime;

	private static bool s_sourceDestroyed;

	private static ZDOID s_fromId = ZDOID.None;

	private static int s_misses;

	private static float s_nextSourceCheck;

	private static bool s_useReleased;

	private static float s_holdTime;

	private static int s_failures;

	private static bool s_failureLogged;

	private static bool s_lineFailureLogged;

	private static bool s_frameFailed;

	private static bool s_postCheckRan;

	private void Awake()
	{
		s_tube = new RopeTube("mm_zipline_carried_rope", transform);
		s_tube.Visible = false;
	}

	internal static bool IsSource(ZiplineAnchor anchor)
	{
		if (!Active || anchor == null)
		{
			return false;
		}
		if (From != null && anchor == From)
		{
			return true;
		}
		float tolerance = ZiplineRope.EndTolerance;
		return (anchor.RopePoint - FromPoint).sqrMagnitude <= tolerance * tolerance;
	}

	// Takes a rope from the post, puts it back, or ties the carried rope to it
	internal static bool AltInteract(ZiplineAnchor anchor, Player player)
	{
		if (!Active)
		{
			if (ZiplineRider.Current != null)
			{
				return false;
			}
			if (player.IsAttached())
			{
				ZiplineMessages.Say(player, "$text_zipline_get_up", "$text_zipline_get_up_short");
				return false;
			}
			if (!PrivateArea.CheckAccess(anchor.transform.position))
			{
				return false;
			}
			int maxRopes = MainConfig.ziplineMaxRopesPerPost.Value;
			if (anchor.RopeCount() >= maxRopes)
			{
				ZiplineMessages.Say(player, maxRopes == 1 ? LocalisedText.Get("text_zipline_post_holds_one") : LocalisedText.Get("text_zipline_post_holds_many", maxRopes.ToString()), "$text_zipline_post_full");
				return false;
			}
			Begin(anchor);
			string length = MainConfig.ziplineMaxRopeLength.Value.ToString("0");
			ZiplineMessages.Say(player, LocalisedText.Get("text_zipline_rope_in_hand", length), LocalisedText.Get("text_zipline_rope_in_hand_short", length));
			return true;
		}
		if (IsSource(anchor))
		{
			Stop();
			ZiplineMessages.Say(player, "$text_zipline_put_rope_back", "$text_zipline_put_rope_back_short");
			return true;
		}
		s_checkedTarget = null;
		if (!CanTieTo(anchor, out float ropeLength, out string problem))
		{
			ZiplineMessages.Say(player, problem, problem);
			return false;
		}
		if (!PrivateArea.CheckAccess(anchor.transform.position))
		{
			return false;
		}
		if (ZiplineRope.Spawn(FromPoint, anchor.RopePoint) == null)
		{
			ZiplineMessages.Say(player, "$text_zipline_not_tied", "$text_zipline_not_tied_short");
			return false;
		}
		Stop();
		ZiplineMessages.Say(player, LocalisedText.Get("text_zipline_ready", ropeLength.ToString("0")), LocalisedText.Get("text_zipline_ready_short", ropeLength.ToString("0")));
		return true;
	}

	private static void Begin(ZiplineAnchor anchor)
	{
		Active = true;
		From = anchor;
		FromPoint = anchor.RopePoint;
		s_fromId = anchor.ZdoId;
		s_misses = 0;
		s_checkedTarget = null;
		s_sourceDestroyed = false;
		s_nextSourceCheck = 0f;
		s_useReleased = false;
		s_holdTime = 0f;
		s_failures = 0;
		s_warnTime = 0f;
	}

	internal static void Stop()
	{
		Active = false;
		From = null;
		s_fromId = ZDOID.None;
		s_misses = 0;
		s_checkedTarget = null;
		s_sourceDestroyed = false;
		s_holdTime = 0f;
		s_failures = 0;
		if (s_tube != null)
		{
			s_tube.Visible = false;
		}
	}

	internal static void SourceDestroyed(ZiplineAnchor anchor)
	{
		if (IsSource(anchor))
		{
			s_sourceDestroyed = true;
			From = null;
		}
	}

	internal static void SourceUnloaded(ZiplineAnchor anchor)
	{
		if (Active && From == anchor)
		{
			From = null;
		}
		s_nextSourceCheck = 0f;
	}

	private static bool SourceRemoved()
	{
		return !s_fromId.IsNone() && ZDOMan.instance != null && ZDOMan.instance.GetZDO(s_fromId) == null;
	}

	// Cached briefly, since the hover text asks every frame and the clearance check casts along the whole rope
	internal static bool CanTieTo(ZiplineAnchor target, out float length, out string problem)
	{
		if (s_checkedTarget == target && Time.time - s_checkedTime < 0.3f)
		{
			length = s_checkedLength;
			problem = s_checkedProblem;
			return s_checkedOk;
		}
		bool ok = Check(target, out length, out problem);
		s_checkedTarget = target;
		s_checkedTime = Time.time;
		s_checkedOk = ok;
		s_checkedLength = length;
		s_checkedProblem = problem;
		return ok;
	}

	private static bool Check(ZiplineAnchor target, out float length, out string problem)
	{
		problem = null;
		Vector3 from = FromPoint;
		Vector3 to = target.RopePoint;
		length = Vector3.Distance(from, to);
		if (s_sourceDestroyed || SourceRemoved() || (From == null && s_misses >= 2))
		{
			problem = LocalisedText.Get("text_zipline_source_gone");
			return false;
		}
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
		float slope = Mathf.Asin(Mathf.Clamp01(Mathf.Abs(to.y - from.y) / length)) * Mathf.Rad2Deg;
		if (slope > MainConfig.ziplineMaxSlope.Value)
		{
			problem = LocalisedText.Get("text_zipline_too_steep", slope.ToString("0"), MainConfig.ziplineMaxSlope.Value.ToString("0"));
			return false;
		}
		int maxRopes = MainConfig.ziplineMaxRopesPerPost.Value;
		if (target.RopeCount() >= maxRopes)
		{
			problem = LocalisedText.Get("text_zipline_no_slots");
			return false;
		}
		if (From != null && From.RopeCount() >= maxRopes)
		{
			problem = LocalisedText.Get("text_zipline_source_no_slots");
			return false;
		}
		if (ZiplineRope.Connects(from, to))
		{
			problem = LocalisedText.Get("text_zipline_connected");
			return false;
		}
		if (MainConfig.ziplineRequireClearance.Value && !CheckClearance(new RopePath(from, to, MainConfig.ziplineRopeSag.Value), out problem))
		{
			return false;
		}
		return true;
	}

	// Follows a rider's feet along the rope, skipping a few metres at each post where they get on and off
	internal static bool CheckClearance(RopePath path, out string problem)
	{
		problem = null;
		float hang = Mathf.Max(MainConfig.ziplineHangHeight.Value, ZiplineRider.ClearanceHang);
		float length = path.Length;
		float endSkip = Mathf.Min(3f, length * 0.2f);
		int steps = Mathf.Max(4, Mathf.CeilToInt(length / 1.5f));
		float waterLevel = ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 30f;
		if (s_clearanceMask == -1)
		{
			s_clearanceMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle");
		}
		Vector3 previous = Vector3.zero;
		bool havePrevious = false;
		for (int i = 0; i <= steps; i++)
		{
			float along = length * i / steps;
			Vector3 feet = path.PointAt(along) + Vector3.down * hang;
			Vector3 body = feet + Vector3.up;
			if (along >= endSkip && along <= length - endSkip)
			{
				if (Heightmap.GetHeight(feet, out float ground) && feet.y < ground + 0.3f)
				{
					problem = LocalisedText.Get("text_zipline_hits_ground", along.ToString("0"));
					return false;
				}
				if (feet.y < waterLevel + 0.3f)
				{
					problem = LocalisedText.Get("text_zipline_hits_water", along.ToString("0"));
					return false;
				}
				if (havePrevious)
				{
					Vector3 step = body - previous;
					float distance = step.magnitude;
					if (distance > 0.01f && Physics.SphereCast(previous, 0.35f, step / distance, out RaycastHit hit, distance, s_clearanceMask, QueryTriggerInteraction.Ignore) && hit.collider != null && hit.collider.GetComponentInParent<ZiplineAnchor>() == null && hit.collider.GetComponentInParent<Character>() == null)
					{
						problem = LocalisedText.Get("text_zipline_blocked", Describe(hit.collider), along.ToString("0"));
						return false;
					}
				}
			}
			previous = body;
			havePrevious = true;
		}
		return true;
	}

	private static string Describe(Collider collider)
	{
		Transform root = collider.transform.root;
		string name = root != null ? root.name : collider.name;
		int clone = name.IndexOf("(Clone)", StringComparison.Ordinal);
		if (clone > 0)
		{
			name = name.Substring(0, clone);
		}
		if (collider.GetComponentInParent<Piece>() != null)
		{
			return LocalisedText.Get("text_zipline_building_piece", name.Trim());
		}
		if (collider.gameObject.layer == LayerMask.NameToLayer("terrain"))
		{
			return LocalisedText.Get("text_zipline_the_ground");
		}
		return name.Trim();
	}

	private static Verdict Judge(Snapshot s)
	{
		if (!s.HasPlayer)
		{
			return Verdict.NoPlayer;
		}
		if (s.Dead)
		{
			return Verdict.Dead;
		}
		if (s.Teleporting)
		{
			return Verdict.Teleported;
		}
		if (s.SourceDestroyed || s.SourceRemoved)
		{
			return Verdict.SourceGone;
		}
		if (!(s.Distance <= s.MaxLength + SlackMetres))
		{
			return Verdict.TooFar;
		}
		if (s.Attached)
		{
			return Verdict.Attached;
		}
		if (s.LetGo)
		{
			return Verdict.LetGo;
		}
		if (s.SourceChecked)
		{
			if (s.SourceLoaded && s.SourceMoved)
			{
				return Verdict.SourceMoved;
			}
			if (!s.SourceLoaded && s.AreaReady && s.MissesInARow >= 2)
			{
				return Verdict.SourceGone;
			}
		}
		return Verdict.Keep;
	}

	private static string BriefDropMessage(Verdict verdict, float maxLength)
	{
		switch (verdict)
		{
		case Verdict.Teleported:
			return "$text_zipline_portal_short";
		case Verdict.SourceGone:
			return "$text_zipline_post_gone_short";
		case Verdict.TooFar:
			return LocalisedText.Get("text_zipline_ran_out_short", maxLength.ToString("0"));
		case Verdict.SourceMoved:
			return "$text_zipline_post_moved_short";
		default:
			return null;
		}
	}

	private static string DropMessage(Verdict verdict, float maxLength)
	{
		switch (verdict)
		{
		case Verdict.Teleported:
			return "$text_zipline_portal";
		case Verdict.SourceGone:
			return "$text_zipline_post_gone";
		case Verdict.TooFar:
			return LocalisedText.Get("text_zipline_ran_out", maxLength.ToString("0"));
		case Verdict.Attached:
		case Verdict.LetGo:
			return "$text_zipline_let_go";
		case Verdict.SourceMoved:
			return "$text_zipline_post_moved";
		default:
			return null;
		}
	}

	private static Snapshot Observe(Player player)
	{
		Snapshot s = new Snapshot { HasPlayer = player != null };
		if (!s.HasPlayer)
		{
			return s;
		}
		s.MaxLength = MainConfig.ziplineMaxRopeLength.Value;
		s.Distance = Vector3.Distance(player.transform.position, FromPoint);
		s.SourceDestroyed = s_sourceDestroyed;
		try
		{
			s.Dead = player.IsDead();
			s.Teleporting = player.IsTeleporting();
			s.Attached = player.IsAttached() || ZiplineRider.Current != null;
			s.LetGo = HeldUseToLetGo(player);
		}
		catch (Exception ex)
		{
			Failed("player check", ex);
		}
		if (Time.time >= s_nextSourceCheck)
		{
			s_nextSourceCheck = Time.time + SourceCheckInterval;
			s_postCheckRan = true;
			try
			{
				float tolerance = ZiplineRope.EndTolerance;
				if (From != null && !From.IsValid)
				{
					From = null;
				}
				if (From == null)
				{
					From = ZiplineAnchor.FindAt(FromPoint, tolerance);
				}
				s.SourceLoaded = From != null;
				s.SourceRemoved = SourceRemoved();
				if (s.SourceLoaded)
				{
					s.SourceMoved = (From.RopePoint - FromPoint).sqrMagnitude > tolerance * tolerance;
					s_misses = 0;
				}
				else
				{
					s.AreaReady = ZNetScene.instance != null && ZNetScene.instance.IsAreaReady(FromPoint);
					s_misses = s.AreaReady ? s_misses + 1 : 0;
				}
				s.MissesInARow = s_misses;
				s.SourceChecked = true;
			}
			catch (Exception ex)
			{
				Failed("post check", ex);
			}
		}
		return s;
	}

	// Holding Use for a second while looking at nothing usable drops the rope
	private static bool HeldUseToLetGo(Player player)
	{
		if (!ZInput.GetButton("Use") && !ZInput.GetButton("JoyUse"))
		{
			s_useReleased = true;
			s_holdTime = 0f;
			return false;
		}
		if (!s_useReleased || !player.TakeInput() || player.InPlaceMode() || Hud.InRadial() || Hud.IsPieceSelectionVisible() || LookingAtSomethingUsable(player))
		{
			s_holdTime = 0f;
			return false;
		}
		s_holdTime += Time.deltaTime;
		return s_holdTime >= LetGoHoldSeconds;
	}

	private static bool LookingAtSomethingUsable(Player player)
	{
		GameObject hover = player.GetHoverObject();
		if (hover == null)
		{
			return false;
		}
		return hover.GetComponentInParent<Interactable>() != null || hover.GetComponentInParent<Hoverable>() != null;
	}

	private static void Failed(string what, Exception ex)
	{
		s_frameFailed = true;
		if (!s_failureLogged)
		{
			s_failureLogged = true;
			ZiplineMessages.LogWarning("Carried rope " + what + " failed (reported once): " + ex);
		}
	}

	private void Update()
	{
		if (!Active)
		{
			if (s_tube != null && s_tube.Visible)
			{
				s_tube.Visible = false;
			}
			return;
		}
		// Turning ziplines off drops a rope being carried; a ride already under way finishes
		if (!MainConfig.enableZiplines.Value)
		{
			Stop();
			ZiplineMessages.Say(Player.m_localPlayer, "$text_zipline_turned_off", "$text_zipline_turned_off_short");
			return;
		}
		Player player = null;
		s_frameFailed = false;
		s_postCheckRan = false;
		try
		{
			player = Player.m_localPlayer;
			Verdict verdict = Judge(Observe(player));
			if (verdict != Verdict.Keep)
			{
				float maxLength = MainConfig.ziplineMaxRopeLength.Value;
				Stop();
				ZiplineMessages.Say(player, DropMessage(verdict, maxLength), BriefDropMessage(verdict, maxLength));
				return;
			}
		}
		catch (Exception ex)
		{
			Failed("update", ex);
		}
		if (s_frameFailed)
		{
			if (++s_failures >= FailuresBeforeDrop)
			{
				ZiplineMessages.LogWarning("The carried rope's checks kept failing, so the rope was dropped.");
				Stop();
				ZiplineMessages.Say(player, "$text_zipline_went_wrong", "$text_zipline_went_wrong_short");
				return;
			}
		}
		else if (s_postCheckRan)
		{
			s_failures = 0;
		}
		if (player == null)
		{
			return;
		}
		try
		{
			float distance = Vector3.Distance(player.transform.position, FromPoint);
			float maxLength = MainConfig.ziplineMaxRopeLength.Value;
			if (distance > maxLength - 10f && Time.time >= s_warnTime)
			{
				s_warnTime = MainConfig.ziplineMessages.Value == ZiplineMessages.Level.All ? Time.time + 3f : float.MaxValue;
				string text = LocalisedText.Get("text_zipline_almost_out", Mathf.Max(0f, maxLength - distance).ToString("0"));
				ZiplineMessages.Say(player, text, text, MessageHud.MessageType.TopLeft);
			}
			DrawCarriedRope(player, distance);
		}
		catch (Exception ex)
		{
			if (!s_lineFailureLogged)
			{
				s_lineFailureLogged = true;
				ZiplineMessages.LogWarning("Drawing the carried rope failed (reported once): " + ex);
			}
		}
	}

	private static void DrawCarriedRope(Player player, float distance)
	{
		if (s_tube == null)
		{
			return;
		}
		Vector3 hand = player.transform.position + Vector3.up * 1.2f + player.transform.forward * 0.25f;
		VisEquipment equipment = player.GetComponent<VisEquipment>();
		if (equipment != null && equipment.m_rightHand != null)
		{
			hand = equipment.m_rightHand.position;
		}
		float sag = Mathf.Clamp(distance * 0.08f, 0.2f, 4f);
		int last = s_points.Length - 1;
		for (int i = 0; i <= last; i++)
		{
			float t = (float)i / last;
			s_points[i] = Vector3.Lerp(FromPoint, hand, t) + Vector3.down * (4f * sag * t * (1f - t));
		}
		s_tube.Build(s_points, MainConfig.ziplineRopeWidth.Value);
	}
}
