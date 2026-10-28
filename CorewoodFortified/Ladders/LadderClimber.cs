using System;
using System.Collections.Generic;
using CorewoodFortified.LadderAnimation;
using HarmonyLib;
using UnityEngine;

namespace CorewoodFortified;

// The local player's climb on a ladder run. Vanilla movement is skipped while climbing and the player's own body
// is moved along the ladder: Forward climbs, Backward climbs down, Run climbs faster for stamina. At the top the
// player steps off onto the floor across the ladder, at the bottom onto the ground. Climbing mechanism and
// animation ported from The Concerned Cat's Concerned Ladders
[HarmonyPatch]
internal static class LadderClimber
{
	private enum Stage
	{
		Off,
		Mounting,
		Climbing,
		Exiting
	}

	private enum Exit
	{
		Stand,
		Drop,
		Jump
	}

	// The body keeps this far, plus its radius, from the middle of the rungs: where the animation's hands reach
	private const float LaneFromRungs = 0.14f;

	// Mounting and stepping off take about this long, whatever the distance
	private const float RouteSeconds = 0.45f;

	// The feet stay this far below the top of the run, so the hands still hold the top rungs
	private const float TopHold = 0.6f;

	// How far the feet hang below the bottom of a run that does not reach the ground
	private const float BottomHang = 1.2f;

	private const float MaxDrift = 0.75f;

	// Where the rungs are on the ladder model, for the hands and feet: six per section, 1/3 m apart from 1/6 m up,
	// so the spacing carries on evenly up a stack of sections
	private const float RungOrigin = 1f / 6f;

	private const float RungPitch = 1f / 3f;

	private const float RungHalfHeight = 0.044f;

	private const float RungHalfDepth = 0.047f;

	private static readonly int s_forwardSpeed = ZSyncAnimation.GetHash("forward_speed");

	private static readonly int s_sidewaySpeed = ZSyncAnimation.GetHash("sideway_speed");

	private static readonly int s_turnSpeed = ZSyncAnimation.GetHash("turn_speed");

	private static readonly int s_inWater = ZSyncAnimation.GetHash("inWater");

	private static readonly int s_onGround = ZSyncAnimation.GetHash("onGround");

	private static readonly int s_falling = ZSyncAnimation.GetHash("falling");

	private static int s_solidMask;

	private static Stage s_stage;

	private static Player s_player;

	private static Rigidbody s_body;

	private static List<Ladder> s_run;

	private static Vector3 s_column;

	// Level and pointing from the ladder towards the climber
	private static Vector3 s_out;

	private static float s_bottom;

	private static float s_top;

	private static float s_lane;

	private static bool s_gravityBefore;

	private static Vector3 s_lastTarget;

	private static float s_speed;

	private static float s_nextTopCheck;

	private static bool s_toldNoTop;

	private static bool s_toldControls;

	private static bool s_fromTop;

	private static float s_routeSpeed;

	private static Vector3 s_routeStart;

	private static float s_routeLength;

	private static float s_routeRise;

	private static ClimbGait s_gait;

	private static readonly List<Vector3> s_route = new List<Vector3>();

	private static readonly List<Collider> s_ignored = new List<Collider>();

	private static OriginalClimbPresenter s_presenter;

	private static bool s_presenterFailed;

	private static bool s_animating;

	private static LadderGeometry s_geometry;

	private static LadderFace s_face;

	private static RungProfile[] s_profiles;

	private static int s_session;

	private static long s_sample;

	private static float s_travel;

	private static float s_lastSampleY;

	private static bool s_lastSampleClimbing;

	private static int SolidMask
	{
		get
		{
			if (s_solidMask == 0)
			{
				s_solidMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle");
			}
			return s_solidMask;
		}
	}

	internal static bool IsClimbing(Player player)
	{
		return s_stage != Stage.Off && player != null && (object)player == s_player;
	}

	internal static bool TryStart(Player player, Ladder ladder)
	{
		if (s_stage != Stage.Off)
		{
			// A climb whose player has gone (logged out, died and respawned) is over
			if (s_player != null && s_body != null)
			{
				return false;
			}
			Stop(Exit.Drop);
		}
		if (ZiplineRider.Current != null || player.IsDead() || player.IsTeleporting() || player.InCutscene() || player.IsDebugFlying())
		{
			return false;
		}
		if (player.IsAttached() || player.GetDoodadController() != null || player.IsSwimming() || player.InMinorAction())
		{
			return false;
		}
		Rigidbody body = player.m_body;
		CapsuleCollider collider = player.m_collider;
		if (body == null || body.isKinematic || collider == null)
		{
			return false;
		}
		List<Ladder> run = ladder.Run();
		Vector3 forward = ladder.transform.forward;
		forward.y = 0f;
		if (forward.sqrMagnitude < 0.0001f)
		{
			return false;
		}
		forward.Normalize();
		Vector3 column = ladder.Bottom;
		float bottom = run[0].Bottom.y;
		float top = run[run.Count - 1].Top.y;
		Vector3 feet = body.position;
		Vector3 offset = feet - column;
		offset.y = 0f;
		if (offset.magnitude > 2f || feet.y < bottom - 2f || feet.y > top + 1f)
		{
			return false;
		}
		s_player = player;
		s_body = body;
		s_run = run;
		s_column = column;
		s_bottom = bottom;
		s_top = top;
		s_lane = collider.radius + LaneFromRungs;
		s_out = Vector3.Dot(offset, forward) >= 0f ? forward : -forward;
		s_route.Clear();
		s_fromTop = feet.y > top - 0.3f;
		if (s_fromTop)
		{
			// From the floor at the top, over onto the far face and down a little
			s_out = -s_out;
			s_route.Add(LanePoint(feet.y + 0.1f));
			s_route.Add(LanePoint(top - TopHold - 0.4f));
		}
		else
		{
			s_route.Add(LanePoint(Mathf.Clamp(feet.y, bottom - BottomHang, top - TopHold)));
		}
		s_gravityBefore = body.useGravity;
		s_lastTarget = feet;
		s_speed = 0f;
		s_gait = ClimbGait.Hold;
		s_toldNoTop = false;
		s_nextTopCheck = 0f;
		BeginRoute();
		IgnoreRunCollisions(collider);
		s_stage = Stage.Mounting;
		BeginPresentation(forward);
		if (!s_toldControls)
		{
			s_toldControls = true;
			player.Message(MessageHud.MessageType.TopLeft, "$text_ladder_controls");
		}
		return true;
	}

	internal static void LetGo()
	{
		if (s_stage != Stage.Off)
		{
			Stop(Exit.Drop);
		}
	}

	// Called every frame: the animation lets go of rigs it no longer drives
	internal static void Tick()
	{
		if (s_presenter == null)
		{
			return;
		}
		try
		{
			s_presenter.Update(Time.deltaTime);
		}
		catch (Exception ex)
		{
			PresentationFailed(ex);
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(Character), "UpdateMotion")]
	private static bool UpdateMotionPrefix(Character __instance)
	{
		if (s_stage == Stage.Off || (object)__instance != s_player)
		{
			return true;
		}
		try
		{
			Drive(Time.fixedDeltaTime);
		}
		catch (Exception ex)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] Ladder climb failed, letting go: " + ex);
			Stop(Exit.Drop);
		}
		return s_stage == Stage.Off;
	}

	// Jump pushes off the ladder and Crouch lets go, instead of their vanilla actions
	[HarmonyPrefix]
	[HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
	private static void SetControlsPrefix(Player __instance, ref bool jump, ref bool crouch)
	{
		if (s_stage == Stage.Off || (object)__instance != s_player)
		{
			return;
		}
		bool jumped = jump;
		bool crouched = crouch;
		jump = false;
		crouch = false;
		if (s_stage != Stage.Climbing)
		{
			return;
		}
		try
		{
			if (jumped)
			{
				JumpOff();
			}
			else if (crouched)
			{
				Stop(Exit.Drop);
			}
		}
		catch (Exception ex)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] Leaving the ladder failed: " + ex);
			Stop(Exit.Drop);
		}
	}

	private static void Drive(float dt)
	{
		if (!StillClimbing())
		{
			Stop(Exit.Drop);
			return;
		}
		switch (s_stage)
		{
		case Stage.Mounting:
			if (FollowRoute(dt))
			{
				s_stage = Stage.Climbing;
			}
			break;
		case Stage.Exiting:
			if (FollowRoute(dt))
			{
				Stop(Exit.Stand);
				return;
			}
			break;
		case Stage.Climbing:
			Climb(dt);
			break;
		}
		if (s_stage == Stage.Off)
		{
			return;
		}
		// Nothing on a ladder counts as a fall: fall damage starts from where the player leaves it
		s_player.m_maxAirAltitude = s_body.position.y;
		// The climbing animation holds the walking pose still and moves the limbs itself, except while stepping off
		Animate(s_animating && s_stage != Stage.Exiting ? 0f : s_speed);
		Present();
	}

	private static bool StillClimbing()
	{
		Player player = s_player;
		if (player == null || s_body == null || s_body.isKinematic)
		{
			return false;
		}
		if (player.IsDead() || player.IsTeleporting() || player.IsAttached() || player.InCutscene() || player.IsDebugFlying())
		{
			return false;
		}
		if (player.GetDoodadController() != null || player.IsStaggering() || player.IsKnockedBack())
		{
			return false;
		}
		// Pushed off by something else
		if (Vector3.Distance(s_body.position, s_lastTarget) > MaxDrift)
		{
			return false;
		}
		foreach (Ladder ladder in s_run)
		{
			if (ladder == null || !ladder.IsValid)
			{
				return false;
			}
		}
		return true;
	}

	private static void Climb(float dt)
	{
		float y = s_body.position.y;
		float input = ReadInput();
		float speed = MainConfig.ladderClimbSpeed.Value;
		s_gait = input == 0f ? ClimbGait.Hold : ClimbGait.Normal;
		if (s_player.IsEncumbered())
		{
			speed *= 0.5f;
		}
		else if (s_player.m_run && input != 0f)
		{
			float stamina = MainConfig.ladderFastClimbStamina.Value * dt;
			if (stamina <= 0f || s_player.HaveStamina(stamina))
			{
				if (stamina > 0f)
				{
					s_player.UseStamina(stamina);
				}
				speed = MainConfig.ladderFastClimbSpeed.Value;
				s_gait = ClimbGait.Fast;
			}
		}
		float v = input * speed;
		float lowest = s_bottom - BottomHang;
		float highest = s_top - TopHold;
		if (v < 0f && GroundUnder(y, out float ground) && ground > lowest)
		{
			if (y + v * dt <= ground + 0.02f)
			{
				Stop(Exit.Stand);
				return;
			}
			lowest = ground;
		}
		if (v > 0f && y >= highest - 0.3f)
		{
			if (Time.time >= s_nextTopCheck)
			{
				s_nextTopCheck = Time.time + 0.25f;
				if (TryTopExit())
				{
					s_stage = Stage.Exiting;
					FollowRoute(dt);
					return;
				}
			}
			if (y >= highest - 0.01f && !s_toldNoTop)
			{
				s_toldNoTop = true;
				s_player.Message(MessageHud.MessageType.TopLeft, "$text_ladder_no_top");
			}
		}
		float next = Mathf.Clamp(y + v * dt, Mathf.Min(lowest, y), Mathf.Max(highest, y));
		if (Mathf.Abs(next - y) < 0.0001f)
		{
			s_gait = ClimbGait.Hold;
		}
		MoveTo(LanePoint(next), dt);
	}

	// Forward on the keys or stick climbs up, whichever way the camera looks
	private static float ReadInput()
	{
		Vector3 move = s_player.m_moveDir;
		Vector3 look = s_player.GetLookDir();
		look.y = 0f;
		if (look.sqrMagnitude < 0.001f)
		{
			look = -s_out;
		}
		float input = Vector3.Dot(move, look.normalized);
		return Mathf.Abs(input) < 0.15f ? 0f : Mathf.Clamp(input, -1f, 1f);
	}

	private static void BeginRoute()
	{
		s_routeStart = s_body.position;
		s_routeLength = 0f;
		s_routeRise = 0f;
		Vector3 from = s_routeStart;
		bool rising = true;
		foreach (Vector3 point in s_route)
		{
			Vector3 leg = point - from;
			s_routeLength += leg.magnitude;
			// The first stretch that is more up or down than across, for the animation's rise and its step over
			if (rising && Mathf.Abs(leg.y) > new Vector2(leg.x, leg.z).magnitude)
			{
				s_routeRise += leg.magnitude;
			}
			else if (s_routeRise > 0f)
			{
				rising = false;
			}
			from = point;
		}
		s_routeSpeed = s_routeLength > 0.01f ? Mathf.Clamp(s_routeLength / RouteSeconds, 0.35f, 6f) : 1.8f;
	}

	private static bool FollowRoute(float dt)
	{
		if (s_route.Count == 0)
		{
			return true;
		}
		Vector3 target = s_route[0];
		Vector3 step = Vector3.MoveTowards(s_body.position, target, s_routeSpeed * dt);
		MoveTo(step, dt);
		if ((target - step).sqrMagnitude < 0.0004f)
		{
			s_route.RemoveAt(0);
		}
		return s_route.Count == 0;
	}

	// How far through mounting or stepping off, 0 to 1; the climb up or down is the first 40% of the animation
	private static float RouteProgress()
	{
		if (s_routeLength <= 0.001f || s_route.Count == 0)
		{
			return 1f;
		}
		float left = 0f;
		Vector3 from = s_body.position;
		foreach (Vector3 point in s_route)
		{
			left += Vector3.Distance(from, point);
			from = point;
		}
		float done = Mathf.Clamp01(1f - left / s_routeLength);
		bool split = s_stage == Stage.Exiting || s_fromTop;
		if (!split || s_routeRise < 0.1f || s_routeRise >= s_routeLength - 0.05f)
		{
			return done;
		}
		float metres = done * s_routeLength;
		return metres <= s_routeRise ? 0.4f * metres / s_routeRise : 0.4f + 0.6f * (metres - s_routeRise) / (s_routeLength - s_routeRise);
	}

	private static void MoveTo(Vector3 target, float dt)
	{
		Vector3 velocity = Vector3.ClampMagnitude((target - s_body.position) / dt, 6f);
		s_body.useGravity = false;
		s_body.linearVelocity = velocity;
		s_body.angularVelocity = Vector3.zero;
		s_body.MoveRotation(Quaternion.RotateTowards(s_body.rotation, Quaternion.LookRotation(-s_out, Vector3.up), 720f * dt));
		s_lastTarget = target;
		s_speed = s_stage == Stage.Exiting ? new Vector2(velocity.x, velocity.z).magnitude : velocity.y;
	}

	private static Vector3 LanePoint(float y)
	{
		return new Vector3(s_column.x, y, s_column.z) + s_out * s_lane;
	}

	private static bool GroundUnder(float y, out float ground)
	{
		ground = 0f;
		if (Physics.Raycast(LanePoint(y) + Vector3.up * 0.3f, Vector3.down, out RaycastHit hit, 2.5f, SolidMask, QueryTriggerInteraction.Ignore)
			&& !s_ignored.Contains(hit.collider))
		{
			ground = hit.point.y;
			return true;
		}
		return false;
	}

	// A floor across the top of the ladder, straight over it first, then to either side
	private static bool TryTopExit()
	{
		Vector3 across = -s_out;
		Vector3 side = Vector3.Cross(Vector3.up, across);
		Vector3 top = new Vector3(s_column.x, s_top, s_column.z);
		float radius = s_player.m_collider.radius;
		foreach (float distance in new[] { Ladder.HalfDepth + radius + 0.35f, Ladder.HalfDepth + radius + 0.75f })
		{
			foreach (float sideways in new[] { 0f, 0.4f, -0.4f })
			{
				Vector3 spot = top + across * distance + side * sideways;
				if (!Physics.Raycast(spot + Vector3.up * 1.6f, Vector3.down, out RaycastHit hit, 2.6f, SolidMask, QueryTriggerInteraction.Ignore))
				{
					continue;
				}
				if (s_ignored.Contains(hit.collider) || hit.normal.y < 0.7f || hit.point.y < s_top - 0.5f || hit.point.y > s_top + 0.6f)
				{
					continue;
				}
				float floor = hit.point.y;
				if (!HasRoom(hit.point) || !HasRoom(LanePoint(floor)))
				{
					continue;
				}
				s_route.Clear();
				s_route.Add(LanePoint(floor + 0.1f));
				s_route.Add(new Vector3(hit.point.x, floor + 0.1f, hit.point.z));
				BeginRoute();
				return true;
			}
		}
		return false;
	}

	private static bool HasRoom(Vector3 feet)
	{
		CapsuleCollider collider = s_player.m_collider;
		float radius = collider.radius * 0.9f;
		Vector3 low = feet + Vector3.up * (radius + 0.1f);
		Vector3 high = feet + Vector3.up * Mathf.Max(radius + 0.1f, collider.height - radius);
		foreach (Collider hit in Physics.OverlapCapsule(low, high, radius, SolidMask, QueryTriggerInteraction.Ignore))
		{
			if (hit != collider && !s_ignored.Contains(hit))
			{
				return false;
			}
		}
		return true;
	}

	private static void JumpOff()
	{
		Vector3 direction = s_out;
		Vector3 move = s_player.m_moveDir;
		move.y = 0f;
		// Jumping with a direction held goes that way, unless it is back into the ladder
		if (move.sqrMagnitude > 0.01f && Vector3.Dot(move.normalized, s_out) > -0.3f)
		{
			direction = move.normalized;
		}
		bool strong = !s_player.IsEncumbered() && s_player.HaveStamina(s_player.m_jumpStaminaUsage);
		if (strong)
		{
			s_player.UseStamina(s_player.m_jumpStaminaUsage);
		}
		else if (Hud.instance != null)
		{
			Hud.instance.StaminaBarEmptyFlash();
		}
		Stop(strong ? Exit.Jump : Exit.Drop, strong ? direction * 3f + Vector3.up * 5f : direction);
	}

	private static void Stop(Exit exit, Vector3 velocity = default)
	{
		s_stage = Stage.Off;
		Player player = s_player;
		Rigidbody body = s_body;
		EndPresentation(player, exit);
		try
		{
			RestoreCollisions(player != null ? player.m_collider : null);
			if (body != null && player != null && !player.IsDead())
			{
				body.useGravity = s_gravityBefore;
				body.linearVelocity = velocity;
				body.angularVelocity = Vector3.zero;
				player.m_maxAirAltitude = body.position.y;
				player.m_currentVel = new Vector3(velocity.x, 0f, velocity.z);
				player.m_lastGroundTouch = exit == Exit.Stand ? 0f : 1f;
				Animate(0f);
			}
		}
		catch (Exception ex)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] Letting go of the ladder failed: " + ex.Message);
		}
		finally
		{
			s_player = null;
			s_body = null;
			s_run = null;
			s_profiles = null;
			s_route.Clear();
			s_ignored.Clear();
		}
	}

	// The walking animation, its speed following the climb
	private static void Animate(float speed)
	{
		ZSyncAnimation zanim = s_player != null ? s_player.m_zanim : null;
		if (zanim == null)
		{
			return;
		}
		zanim.SetFloat(s_forwardSpeed, Mathf.Clamp(speed, -1.8f, 1.8f));
		zanim.SetFloat(s_sidewaySpeed, 0f);
		zanim.SetFloat(s_turnSpeed, 0f);
		zanim.SetBool(s_inWater, value: false);
		zanim.SetBool(s_onGround, value: true);
		zanim.SetBool(s_falling, value: false);
	}

	// Sets up the climbing animation for this climb, which falls back to the walking pose if it cannot run
	private static void BeginPresentation(Vector3 forward)
	{
		s_animating = false;
		if (!MainConfig.ladderClimbAnimation.Value || s_presenterFailed)
		{
			return;
		}
		Ladder bottom = s_run[0];
		Ladder top = s_run[s_run.Count - 1];
		ClimbPoint low = new ClimbPoint(s_column.x, bottom.Bottom.y, s_column.z);
		ClimbPoint high = new ClimbPoint(s_column.x, top.Top.y, s_column.z);
		if (!LadderGeometry.TryMeasure(low, high, ClimbHeading.FromXz(forward.x, forward.z), 0.8f, RungPitch, out s_geometry))
		{
			return;
		}
		s_face = Vector3.Dot(s_out, forward) >= 0f ? LadderFace.Front : LadderFace.Rear;
		s_profiles = new RungProfile[s_run.Count];
		for (int i = 0; i < s_run.Count; i++)
		{
			s_profiles[i] = new RungProfile(s_run[i].Bottom.y, s_run[i].Top.y, RungHalfHeight, RungHalfDepth, 0f);
		}
		if (s_presenter == null)
		{
			s_presenter = new OriginalClimbPresenter(message => Jotunn.Logger.LogDebug("[CorewoodFortified] " + message));
			if (!s_presenter.Install(CorewoodFortifiedMod.PluginGUID + ".ladderanimation"))
			{
				s_presenter = null;
				s_presenterFailed = true;
				Jotunn.Logger.LogWarning("[CorewoodFortified] The ladder climbing animation could not start; ladders are climbed in the walking pose.");
				return;
			}
		}
		s_session++;
		s_travel = 0f;
		s_lastSampleY = s_body.position.y;
		s_lastSampleClimbing = false;
		s_animating = true;
	}

	private static void Present()
	{
		if (!s_animating)
		{
			return;
		}
		try
		{
			Vector3 position = s_body.position;
			float step = s_stage == Stage.Climbing && s_lastSampleClimbing ? position.y - s_lastSampleY : 0f;
			s_travel += step;
			s_lastSampleY = position.y;
			s_lastSampleClimbing = s_stage == Stage.Climbing;
			PresentedMotion motion;
			switch (s_stage)
			{
			case Stage.Mounting:
				motion = s_fromTop ? PresentedMotion.TopEntry : PresentedMotion.GroundMount;
				break;
			case Stage.Exiting:
				motion = PresentedMotion.TopFinish;
				break;
			default:
				motion = s_gait == ClimbGait.Fast ? PresentedMotion.Fast : s_gait == ClimbGait.Normal ? PresentedMotion.Climb : PresentedMotion.Hold;
				break;
			}
			bool routing = s_stage != Stage.Climbing;
			Vector3 end = routing && s_route.Count > 0 ? s_route[s_route.Count - 1] : position;
			Player player = s_player;
			bool placing = player.InPlaceMode();
			NativeActionParts native = player.InMinorAction()
				? NativeActionParts.LeftArm | NativeActionParts.RightArm | NativeActionParts.UpperBody
				: placing ? NativeActionParts.RightArm : NativeActionParts.None;
			bool menu = !player.TakeInput() || InventoryGui.IsVisible() || Hud.InRadial() || Hud.IsPieceSelectionVisible();
			int lastRung = ClimbPresentationFrame.LastRungOf(s_geometry.Height, RungOrigin, s_geometry.RungPitch);
			ClimbPresentationFrame frame = new ClimbPresentationFrame(s_session, 0, 0, ++s_sample, Time.fixedTime, motion, s_gait, s_travel, step,
				Point(position), s_geometry, s_face, 0f, RungOrigin, lastRung, routing ? RouteProgress() : -1f,
				Point(routing ? s_routeStart : position), Point(end), native, placing, menu, s_profiles);
			s_presenter.Publish(player, in frame);
		}
		catch (Exception ex)
		{
			PresentationFailed(ex);
		}
	}

	private static void EndPresentation(Player player, Exit exit)
	{
		if (!s_animating)
		{
			return;
		}
		s_animating = false;
		try
		{
			s_presenter?.End(player, exit == Exit.Stand ? ClimbEnd.Landed : exit == Exit.Jump ? ClimbEnd.Jump : ClimbEnd.Drop);
		}
		catch (Exception ex)
		{
			PresentationFailed(ex);
		}
	}

	// The climb goes on in the walking pose for the rest of the session
	private static void PresentationFailed(Exception ex)
	{
		Jotunn.Logger.LogWarning("[CorewoodFortified] The ladder climbing animation stopped, climbing in the walking pose from now on: " + ex.Message);
		s_animating = false;
		s_presenterFailed = true;
		OriginalClimbPresenter presenter = s_presenter;
		s_presenter = null;
		try
		{
			presenter?.Dispose();
		}
		catch
		{
		}
	}

	private static ClimbPoint Point(Vector3 point)
	{
		return new ClimbPoint(point.x, point.y, point.z);
	}

	// The climber passes over the top of the ladder and close by its rungs without catching on them
	private static void IgnoreRunCollisions(CapsuleCollider playerCollider)
	{
		s_ignored.Clear();
		foreach (Ladder ladder in s_run)
		{
			foreach (Collider collider in ladder.GetComponentsInChildren<Collider>())
			{
				if (collider != null && collider.enabled && !collider.isTrigger)
				{
					Physics.IgnoreCollision(playerCollider, collider, ignore: true);
					s_ignored.Add(collider);
				}
			}
		}
	}

	private static void RestoreCollisions(CapsuleCollider playerCollider)
	{
		if (playerCollider == null)
		{
			return;
		}
		foreach (Collider collider in s_ignored)
		{
			if (collider != null)
			{
				Physics.IgnoreCollision(playerCollider, collider, ignore: false);
			}
		}
	}
}
