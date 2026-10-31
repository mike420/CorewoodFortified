using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CorewoodFortified;

// Brings a ship in to a dock capstan by itself. A ship bigger than the raft that comes towards a capstan from in front
// of it, within 30 m, is brought in without turning, keeping the heading it came in on, until the nearest part of its
// hull is 6 m out from the capstan's attach points, and held there. Once there it is anchored, as a ship is when everyone has left it: it does not drift or turn, but still
// rides the waves, and waves slamming into it do it no damage. If a dock lies alongside it once it is there, within
// 15 m, it is moved sideways up to that dock, stopping just short of it. Pressing forward or back at the helm lets it go again, and it is not caught by that capstan again
// until it has sailed out of range. If anything is in the way the ship is left alone, and everyone on board is told.
//
// Only the game that owns the ship moves it, as it does all of the ship's physics. Which capstan it is docked at is kept
// on the ship, so whoever owns it next keeps holding it there
internal sealed class ShipDocking : MonoBehaviour
{
	private const float Range = 30f;

	private const float DockDistance = 6f;

	// How fast the ship has to be closing on the capstan for it to be caught, in metres per second
	private const float MinApproachSpeed = 0.5f;

	private const float MaxSpeed = 2.5f;

	private const float SpeedPerMetre = 0.4f;

	private const float Acceleration = 4f;

	private const float MaxTurnRate = 15f;

	private const float TurnRatePerDegree = 0.5f;

	private const float TurnAcceleration = 20f;

	// How quickly an anchored ship is drawn back to its place, per metre or degree off it, and how fast at most
	private const float AnchorPull = 0.5f;

	private const float AnchorMaxSpeed = 0.5f;

	private const float AnchorMaxTurnRate = 2f;

	// How far to the side a dock can be for the ship to be moved over to it, and how far short of it it stops
	private const float SideReach = 15f;

	private const float SideGap = 0.05f;

	private const float BlockedCheckInterval = 0.25f;

	private const float BlockedMessageInterval = 3f;

	private const string RpcBlocked = "CWF_DockingBlocked";

	// A message in the middle of the screen fades out over 4 s, so "Auto Docking..." is shown again this often while the
	// ship is being brought in
	private const float DockingMessageRepeat = 2f;

	// Where the capstan the ship is docked at stands. Its place, not its ZDOID, as the game gives every ZDO a new ZDOID
	// when the world loads
	private static readonly int s_caughtKey = "cwf_dock_caught".GetStableHashCode();

	private static readonly int s_capstanAtKey = "cwf_dock_capstan_at".GetStableHashCode();

	// Set once the ship lies in its place, so every game shows it tied up (see MooringRopes)
	internal static readonly int s_mooredKey = "cwf_moored".GetStableHashCode();

	// How far the ship is moved sideways to a dock alongside it, and whether that has been looked for yet
	private static readonly int s_sideShiftKey = "cwf_dock_side_shift".GetStableHashCode();

	private static readonly int s_sideCheckedKey = "cwf_dock_side_checked".GetStableHashCode();

	private static int s_blockMask;

	private static readonly Collider[] s_overlaps = new Collider[32];

	private static readonly RaycastHit[] s_hits = new RaycastHit[32];

	private enum State
	{
		Free,
		Docking,
		Docked
	}

	private Ship m_ship;

	private ZNetView m_nview;

	private Rigidbody m_body;

	private State m_state;

	private DockCapstan m_capstan;

	// The heading the ship came in on, which it keeps while it is brought in and held
	private Quaternion m_heading;

	private Vector3 m_sideShift;

	private bool m_sideChecked;

	private Collider[] m_hull;


	// The capstan the ship was last let go from, which cannot catch it again until it has left its range
	private DockCapstan m_leftCapstan;

	private float m_nextBlockedCheck;

	private float m_nextBlockedMessage;

	private bool m_showingDocking;

	private float m_nextDockingMessage;

	private void Awake()
	{
		m_ship = GetComponent<Ship>();
		m_nview = GetComponent<ZNetView>();
		m_body = GetComponent<Rigidbody>();
		if (s_blockMask == 0)
		{
			s_blockMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle");
		}
		m_nview.Register(RpcBlocked, RPC_Blocked);
	}

	// Run straight after the ship's own physics, so the ship's drag does not undo the steering until the next step
	private void Steer(float dt)
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner() || !m_ship.m_floatCollider)
		{
			return;
		}
		if (!MainConfig.enableAutoDocking.Value)
		{
			if (m_state != State.Free)
			{
				Release();
			}
			return;
		}
		if (m_state == State.Free)
		{
			ResumeSaved();
		}
		if (m_state == State.Free)
		{
			TryDock();
			return;
		}
		if (!m_capstan)
		{
			Release();
			return;
		}
		// The sail stays furled and the oars still until someone at the helm lets the ship go (see LetGo)
		m_ship.m_speed = Ship.Speed.Stop;
		m_ship.m_sailForce = Vector3.zero;
		m_ship.m_windChangeVelocity = Vector3.zero;
		GetTarget(m_capstan, out Vector3 target, out Quaternion rotation);
		// Moving over to a dock alongside was already found clear by the sweep that found the dock
		if (m_state == State.Docking && !m_sideChecked && Time.time >= m_nextBlockedCheck)
		{
			m_nextBlockedCheck = Time.time + BlockedCheckInterval;
			if (IsBlocked(target, rotation))
			{
				m_leftCapstan = m_capstan;
				Release();
				SayBlocked();
				return;
			}
		}
		if (m_state == State.Docked)
		{
			Anchor(target, rotation);
			return;
		}
		MoveTowards(target, rotation, dt);
		if (IsAt(target, rotation))
		{
			if (!m_sideChecked)
			{
				SetSideShift(FindSideShift(target), true);
				if (m_sideShift.sqrMagnitude > 0.01f)
				{
					return;
				}
			}
			m_state = State.Docked;
			m_nview.GetZDO().Set(s_mooredKey, true);
		}
	}

	// Someone at the helm pressed forward or back, so the ship is theirs again. Presses while it is still being brought
	// in are ignored, as one is easily made to slow down just as it is caught
	private void LetGo()
	{
		if (m_state != State.Docked || !m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		m_leftCapstan = m_capstan;
		Release();
	}

	private void TryDock()
	{
		if (m_leftCapstan && !InRange(m_leftCapstan))
		{
			m_leftCapstan = null;
		}
		DockCapstan best = null;
		float bestDistance = float.MaxValue;
		Vector3 centre = Centre();
		Vector3 velocity = Vector3.ProjectOnPlane(m_body.linearVelocity, Vector3.up);
		foreach (DockCapstan capstan in DockCapstan.All)
		{
			if (!capstan || capstan == m_leftCapstan || !InRange(capstan))
			{
				continue;
			}
			Vector3 toCapstan = Vector3.ProjectOnPlane(capstan.DockPoint - centre, Vector3.up);
			if (Vector3.Dot(velocity, toCapstan.normalized) < MinApproachSpeed)
			{
				continue;
			}
			if (toCapstan.magnitude < bestDistance)
			{
				bestDistance = toCapstan.magnitude;
				best = capstan;
			}
		}
		if (!best || Time.time < m_nextBlockedCheck)
		{
			return;
		}
		m_nextBlockedCheck = Time.time + BlockedCheckInterval;
		GetTarget(best, out Vector3 target, out Quaternion rotation);
		if (IsBlocked(target, rotation))
		{
			SayBlocked();
			return;
		}
		m_capstan = best;
		m_state = State.Docking;
		m_heading = FlatHeading();
		SetSideShift(Vector3.zero, false);
		m_ship.m_speed = Ship.Speed.Stop;
		SaveCapstan(m_nview.GetZDO(), best);
		// Not tied up until it lies in its place, whatever an earlier docking left behind
		m_nview.GetZDO().Set(s_mooredKey, false);
	}

	// Picks up a docking the ship's last owner had, once its capstan has loaded
	private void ResumeSaved()
	{
		DockCapstan capstan = SavedCapstan(m_nview.GetZDO());
		if (capstan)
		{
			m_capstan = capstan;
			m_state = State.Docking;
			m_heading = FlatHeading();
			ZDO zdo = m_nview.GetZDO();
			m_sideShift = zdo.GetVec3(s_sideShiftKey, Vector3.zero);
			m_sideChecked = zdo.GetBool(s_sideCheckedKey);
		}
	}

	private void Release()
	{
		m_state = State.Free;
		m_capstan = null;
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			SaveCapstan(m_nview.GetZDO(), null);
			m_nview.GetZDO().Set(s_mooredKey, false);
			SetSideShift(Vector3.zero, false);
		}
	}

	private static void SaveCapstan(ZDO zdo, DockCapstan capstan)
	{
		zdo.Set(s_caughtKey, (bool)capstan);
		zdo.Set(s_capstanAtKey, capstan ? capstan.transform.position : Vector3.zero);
	}

	internal static bool IsCaught(ZDO zdo)
	{
		return zdo.GetBool(s_caughtKey);
	}

	// The capstan the ship is docked at, if it has loaded
	internal static DockCapstan SavedCapstan(ZDO zdo)
	{
		return IsCaught(zdo) ? DockCapstan.FindAt(zdo.GetVec3(s_capstanAtKey, Vector3.zero)) : null;
	}

	private void SetSideShift(Vector3 shift, bool isChecked)
	{
		m_sideShift = shift;
		m_sideChecked = isChecked;
		ZDO zdo = m_nview.GetZDO();
		zdo.Set(s_sideShiftKey, shift);
		zdo.Set(s_sideCheckedKey, isChecked);
	}

	// Sweeps the ship's own colliders out to each side from where it lies, as they are, so a ladder or shield that would
	// pass over a dock's deck does not count as touching it. If the first thing it would touch on a side is a dock, the
	// nearer such dock, the ship is to move over to it, stopping just short. Worked out from the target, so the gap is
	// right however near the target the ship stopped
	private Vector3 FindSideShift(Vector3 target)
	{
		Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
		Vector3 shift = Vector3.zero;
		float bestDistance = float.MaxValue;
		foreach (Vector3 side in new[] { right, -right })
		{
			RaycastHit[] hits = m_body.SweepTestAll(side, SideReach, QueryTriggerInteraction.Ignore);
			RaycastHit first = default;
			bool found = false;
			// Only solid things count, not players on board. Anything the ship already touches, such as the sea floor
			// under its keel, is left out, and so is the capstan's own dock section, which the ship keeps its distance from
			foreach (RaycastHit hit in hits)
			{
				if (hit.distance <= 0f || (s_blockMask & (1 << hit.collider.gameObject.layer)) == 0 || IsOwnCollider(hit.collider)
					|| hit.collider.transform.IsChildOf(m_capstan.transform) || (found && hit.distance >= first.distance))
				{
					continue;
				}
				first = hit;
				found = true;
			}
			// Nothing that side, or what is there is not a dock
			if (!found || !first.collider.GetComponentInParent<DockSupports>() || first.distance >= bestDistance)
			{
				continue;
			}
			bestDistance = first.distance;
			float fromTarget = Vector3.Dot(Vector3.ProjectOnPlane(Centre() - target, Vector3.up), side);
			shift = side * Mathf.Max(0f, fromTarget + first.distance - SideGap);
		}
		return shift;
	}

	// In front of the capstan's dock and near enough to it
	// The 30 m is to the nearest part of the hull, not the ship's middle, so a long ship coming in bow first is caught
	// when its bow is 30 m off, not half a ship nearer
	private bool InRange(DockCapstan capstan)
	{
		Vector3 dockPoint = capstan.DockPoint;
		Vector3 offset = Vector3.ProjectOnPlane(Centre() - dockPoint, Vector3.up);
		if (Vector3.Dot(offset, capstan.Front) <= 0f)
		{
			return false;
		}
		Vector3 nearest = Vector3.ProjectOnPlane(NearestHullPoint(dockPoint) - dockPoint, Vector3.up);
		return nearest.magnitude <= Range;
	}

	// The point of the ship's solid colliders nearest the given point. Hull mesh colliders that are not convex cannot be
	// measured to exactly, so their bounds stand in for them
	private Vector3 NearestHullPoint(Vector3 point)
	{
		if (m_hull == null)
		{
			m_hull = GetComponentsInChildren<Collider>();
		}
		Vector3 nearest = Centre();
		float nearestDistance = float.MaxValue;
		foreach (Collider collider in m_hull)
		{
			if (!collider || collider.isTrigger || !collider.enabled)
			{
				continue;
			}
			Vector3 candidate = collider is MeshCollider mesh && !mesh.convex ? collider.bounds.ClosestPoint(point) : collider.ClosestPoint(point);
			float distance = Vector3.ProjectOnPlane(candidate - point, Vector3.up).sqrMagnitude;
			if (distance < nearestDistance)
			{
				nearestDistance = distance;
				nearest = candidate;
			}
		}
		return nearest;
	}

	// Straight out in front of the capstans, with the part of the hull nearest the dock 6 m out from the attach points,
	// and the ship on the heading it came in on
	private void GetTarget(DockCapstan capstan, out Vector3 target, out Quaternion rotation)
	{
		Vector3 centre = Centre();
		Vector3 front = capstan.Front;
		// How far the hull reaches from the ship's middle towards the dock, as it lies now
		Vector3 nearest = NearestHullPoint(centre - front * 1000f);
		float reach = Mathf.Max(0f, Vector3.Dot(centre - nearest, front));
		target = capstan.DockPoint + front * (DockDistance + reach);
		if (m_state != State.Free)
		{
			target += m_sideShift;
		}
		target.y = centre.y;
		rotation = m_state == State.Free ? FlatHeading() : m_heading;
	}

	private Quaternion FlatHeading()
	{
		Vector3 heading = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
		return heading.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(heading, Vector3.up) : Quaternion.identity;
	}

	private Vector3 Centre()
	{
		BoxCollider box = m_ship.m_floatCollider;
		return box.transform.TransformPoint(box.center);
	}

	private Vector3 HalfSize(float shrink)
	{
		BoxCollider box = m_ship.m_floatCollider;
		Vector3 size = Vector3.Scale(box.size, box.transform.lossyScale) * 0.5f;
		return new Vector3(size.x * shrink, Mathf.Max(size.y, 0.5f), size.z * shrink);
	}

	// Sets the ship's speed and turn towards what takes it to the target, a little more each step
	// Slows the ship and moves it to the target, keeping it on its heading
	private void MoveTowards(Vector3 target, Quaternion rotation, float dt)
	{
		Vector3 heading = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
		float angle = Vector3.SignedAngle(heading, rotation * Vector3.forward, Vector3.up);

		Vector3 offset = Vector3.ProjectOnPlane(target - Centre(), Vector3.up);
		Vector3 wanted = Vector3.ClampMagnitude(offset * SpeedPerMetre, MaxSpeed);
		Vector3 velocity = m_body.linearVelocity;
		Vector3 flat = Vector3.MoveTowards(new Vector3(velocity.x, 0f, velocity.z), wanted, Acceleration * dt);
		m_body.linearVelocity = new Vector3(flat.x, velocity.y, flat.z);

		float wantedTurn = Mathf.Clamp(angle * TurnRatePerDegree, -MaxTurnRate, MaxTurnRate) * Mathf.Deg2Rad;
		Vector3 angular = m_body.angularVelocity;
		angular.y = Mathf.MoveTowards(angular.y, wantedTurn, TurnAcceleration * Mathf.Deg2Rad * dt);
		m_body.angularVelocity = angular;
	}

	// Holds the ship still where it lies, leaving it free to rise and fall with the waves. It is only nudged back, slowly,
	// if a wave or a bump has moved it off its place
	private void Anchor(Vector3 target, Quaternion rotation)
	{
		Vector3 offset = Vector3.ProjectOnPlane(target - Centre(), Vector3.up);
		Vector3 drift = Vector3.ClampMagnitude(offset * AnchorPull, AnchorMaxSpeed);
		Vector3 velocity = m_body.linearVelocity;
		m_body.linearVelocity = new Vector3(drift.x, velocity.y, drift.z);

		Vector3 heading = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
		float angle = Vector3.SignedAngle(heading, rotation * Vector3.forward, Vector3.up);
		Vector3 angular = m_body.angularVelocity;
		angular.y = Mathf.Clamp(angle * AnchorPull, -AnchorMaxTurnRate, AnchorMaxTurnRate) * Mathf.Deg2Rad;
		m_body.angularVelocity = angular;
	}

	private bool IsAt(Vector3 target, Quaternion rotation)
	{
		Vector3 offset = Vector3.ProjectOnPlane(target - Centre(), Vector3.up);
		float angle = Vector3.Angle(Vector3.ProjectOnPlane(transform.forward, Vector3.up), rotation * Vector3.forward);
		return offset.magnitude < 0.5f && angle < 5f;
	}

	// Anything solid, other than the ship itself, along the way the ship's middle goes, or where it will lie
	private bool IsBlocked(Vector3 target, Quaternion rotation)
	{
		Vector3 centre = Centre();
		Vector3 halfSize = HalfSize(0.9f);
		Vector3 path = target - centre;
		int hits = path.magnitude < 0.1f ? 0 : Physics.SphereCastNonAlloc(centre, halfSize.x, path.normalized, s_hits, path.magnitude, s_blockMask, QueryTriggerInteraction.Ignore);
		for (int i = 0; i < hits; i++)
		{
			if (!IsOwnCollider(s_hits[i].collider))
			{
				return true;
			}
		}
		int overlaps = Physics.OverlapBoxNonAlloc(target, halfSize, s_overlaps, rotation, s_blockMask, QueryTriggerInteraction.Ignore);
		for (int i = 0; i < overlaps; i++)
		{
			if (!IsOwnCollider(s_overlaps[i]))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsOwnCollider(Collider collider)
	{
		return collider.attachedRigidbody == m_body || collider.transform.IsChildOf(transform);
	}

	private void SayBlocked()
	{
		if (Time.time < m_nextBlockedMessage)
		{
			return;
		}
		m_nextBlockedMessage = Time.time + BlockedMessageInterval;
		m_nview.InvokeRPC(ZNetView.Everybody, RpcBlocked);
	}

	private void RPC_Blocked(long sender)
	{
		SayOnBoard("$text_docking_blocked");
	}

	// Keeps "Auto Docking..." up for everyone on board while the ship has been caught but does not yet lie in its place.
	// Every game reads that from the ship's ZDO, so it shows whoever owns the ship
	private void Update()
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		ZDO zdo = m_nview.GetZDO();
		bool docking = IsCaught(zdo) && !zdo.GetBool(s_mooredKey);
		if (!docking)
		{
			m_showingDocking = false;
			return;
		}
		if (m_showingDocking && Time.time < m_nextDockingMessage)
		{
			return;
		}
		Player player = Player.m_localPlayer;
		if (player && m_ship.IsPlayerInBoat(player) && MessageHud.instance)
		{
			// Only the first showing goes in the message log
			MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$text_auto_docking", log: !m_showingDocking);
		}
		m_showingDocking = true;
		m_nextDockingMessage = Time.time + DockingMessageRepeat;
	}

	// Shown in the middle of the screen to the local player, if they are on this ship
	private void SayOnBoard(string text)
	{
		Player player = Player.m_localPlayer;
		if (player && m_ship.IsPlayerInBoat(player))
		{
			player.Message(MessageHud.MessageType.Center, text);
		}
	}

	private static bool CanDock(Ship ship)
	{
		return Utils.GetPrefabName(ship.gameObject) != "Raft";
	}

	[HarmonyPatch(typeof(Ship), "Awake")]
	private static class ShipAwakePatch
	{
		private static void Postfix(Ship __instance)
		{
			ZNetView nview = __instance.GetComponent<ZNetView>();
			if (nview && nview.GetZDO() != null && CanDock(__instance))
			{
				__instance.gameObject.AddComponent<ShipDocking>();
				__instance.gameObject.AddComponent<MooringRopes>();
			}
		}
	}

	// A docked ship takes no damage from waves. The game damages a ship when a wave drops its hull hard, at most once
	// every few seconds, so marking an impact as just happened keeps it from happening, while the ship still keeps
	// track of the water for when it sails off
	[HarmonyPatch(typeof(Ship), "UpdateWaterForce")]
	private static class ShipUpdateWaterForcePatch
	{
		private static void Prefix(Ship __instance, float time)
		{
			ShipDocking docking = __instance.GetComponent<ShipDocking>();
			if (docking && docking.m_state == State.Docked)
			{
				__instance.m_lastWaterImpactTime = time;
			}
		}
	}

	// Forward and back from the helm reach the ship's owner as these, so a press from anyone at the helm lets it go
	[HarmonyPatch(typeof(Ship), "RPC_Forward")]
	private static class ShipForwardPatch
	{
		private static void Prefix(Ship __instance)
		{
			ShipDocking docking = __instance.GetComponent<ShipDocking>();
			if (docking)
			{
				docking.LetGo();
			}
		}
	}

	[HarmonyPatch(typeof(Ship), "RPC_Backward")]
	private static class ShipBackwardPatch
	{
		private static void Prefix(Ship __instance)
		{
			ShipDocking docking = __instance.GetComponent<ShipDocking>();
			if (docking)
			{
				docking.LetGo();
			}
		}
	}

	[HarmonyPatch(typeof(Ship), nameof(Ship.CustomFixedUpdate))]
	private static class ShipCustomFixedUpdatePatch
	{
		private static void Postfix(Ship __instance, float fixedDeltaTime)
		{
			ShipDocking docking = __instance.GetComponent<ShipDocking>();
			if (docking)
			{
				docking.Steer(fixedDeltaTime);
			}
		}
	}
}
