using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorewoodFortified;

// The local player's ride along a rope. The player is attached to this object, which moves along the rope and
// takes their movement keys as pull and brake
public class ZiplineRider : MonoBehaviour, IDoodadController
{
	private enum EndReason
	{
		Arrived,
		ArrivedBack,
		LetGo,
		Interrupted
	}

	internal static ZiplineRider Current;

	// Ropes are checked for a rider's feet at least this far below them, whatever a player's Hang Height
	internal const float ClearanceHang = 2.3f;

	private const float StartOffset = 0.9f;

	private const float EndMargin = 0.9f;

	private const string NoAnimation = "mm_zipline_none";

	internal static float NoGrabUntil;

	private static bool s_toldControls;

	private static int s_logoutLogged;

	private Player m_player;

	private RopePath m_path;

	private bool m_fromB;

	private float m_length;

	private float m_s;

	private float m_v;

	private bool m_ending;

	private Vector3 m_input;

	private bool m_block;

	private Vector3 m_look;

	private Quaternion m_bodyRotation = Quaternion.identity;

	private float m_pinnedAtStart;

	private bool m_resting;

	private bool m_toldHeavy;

	private Vector3 m_tangent = Vector3.forward;

	private Vector3 m_farEnd;

	private bool m_farEndIgnored;

	private float m_nextScan;

	private float m_nextRopeCheck;

	private CapsuleCollider m_playerCollider;

	private Rigidbody m_rigidbody;

	private readonly List<Collider> m_ignored = new List<Collider>();

	private GameObject m_soundObject;

	private AudioSource m_audio;

	private ZSFX m_sfx;

	private float m_basePitch = 1f;

	private float m_baseVolume = 1f;

	internal static bool TryStart(Player player, ZiplineAnchor anchor, ZiplineRope rope, out string error)
	{
		error = null;
		if (Current != null)
		{
			error = "$text_zipline_already_riding";
			return false;
		}
		if (RopeCarry.Active)
		{
			error = "$text_zipline_rope_in_hand_first";
			return false;
		}
		if (player.IsDead() || player.IsTeleporting() || player.InCutscene())
		{
			return false;
		}
		if (player.IsAttached() || player.GetDoodadController() != null)
		{
			error = "$text_zipline_holding_something";
			return false;
		}
		if (player.IsSwimming())
		{
			error = "$text_zipline_swimming";
			return false;
		}
		if (IsPullingCart(player))
		{
			error = "$text_zipline_cart";
			return false;
		}
		RopePath path = rope.Path;
		if (path == null || path.Length < StartOffset + EndMargin + 1.8f)
		{
			error = "$text_zipline_too_short";
			return false;
		}
		Vector3 point = anchor.RopePoint;
		bool fromB = (path.B - point).sqrMagnitude < (path.A - point).sqrMagnitude;
		Vector3 farEnd = fromB ? path.A : path.B;
		if (ZNetScene.instance != null && ZNetScene.instance.IsAreaReady(farEnd) && ZiplineAnchor.FindAt(farEnd, ZiplineRope.EndTolerance) == null)
		{
			error = "$text_zipline_not_tied_far";
			return false;
		}
		ZiplineRider rider = new GameObject("mm_zipline_rider").AddComponent<ZiplineRider>();
		if (!rider.Begin(player, path, fromB, farEnd, anchor.gameObject, StartOffset, Mathf.Max(2.5f, MainConfig.ziplineCrawlSpeed.Value)))
		{
			error = "$text_zipline_no_grab";
			return false;
		}
		return true;
	}

	// Catching a rope in mid-air rides on in the direction the player was moving, or facing
	internal static bool TryGrab(Player player, ZiplineRope rope, float fromA, Vector3 velocity)
	{
		if (Current != null || RopeCarry.Active || rope == null)
		{
			return false;
		}
		if (player.IsDead() || player.IsTeleporting() || player.InCutscene() || player.IsSwimming())
		{
			return false;
		}
		if (player.IsAttached() || player.GetDoodadController() != null || IsPullingCart(player))
		{
			return false;
		}
		RopePath path = rope.Path;
		if (path == null || path.Length < 4.6f)
		{
			return false;
		}
		Vector3 tangent = path.TangentAt(fromA);
		float along = Vector3.Dot(velocity, tangent);
		bool fromB;
		if (Mathf.Abs(along) > 0.5f)
		{
			fromB = along < 0f;
		}
		else
		{
			Vector3 forward = player.transform.forward;
			fromB = Vector3.Dot(new Vector3(forward.x, 0f, forward.z), new Vector3(tangent.x, 0f, tangent.z)) < 0f;
		}
		float start = Mathf.Clamp(fromB ? path.Length - fromA : fromA, 1f, path.Length - EndMargin - 0.1f);
		float speed = Mathf.Min(Mathf.Abs(along), MainConfig.ziplineMaxSpeed.Value);
		ZiplineRider rider = new GameObject("mm_zipline_rider").AddComponent<ZiplineRider>();
		return rider.Begin(player, path, fromB, fromB ? path.A : path.B, rope.gameObject, start, speed);
	}

	private bool Begin(Player player, RopePath path, bool fromB, Vector3 farEnd, GameObject start, float startS, float startV)
	{
		Current = this;
		m_player = player;
		m_path = path;
		m_fromB = fromB;
		m_farEnd = farEnd;
		m_length = path.Length;
		m_s = startS;
		m_v = startV;
		Vector3 heading = fromB ? path.A - path.B : path.B - path.A;
		heading.y = 0f;
		m_bodyRotation = heading.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(heading.normalized, Vector3.up) : player.transform.rotation;
		m_rigidbody = gameObject.AddComponent<Rigidbody>();
		m_rigidbody.useGravity = false;
		m_rigidbody.isKinematic = false;
		m_rigidbody.detectCollisions = false;
		m_rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
		m_rigidbody.interpolation = RigidbodyInterpolation.None;
		PlaceAttachPoint();
		m_playerCollider = player.m_collider;
		IgnoreCollidersOf(start);
		player.StartDoodadControl(this);
		string animation = MainConfig.ziplineRideAnimation.Value;
		bool hang = string.IsNullOrEmpty(animation) || animation == "hang";
		if (hang || animation == "none")
		{
			animation = NoAnimation;
		}
		player.AttachStart(transform, start, hideWeapons: true, isBed: false, onShip: false, animation, Vector3.zero);
		if (!player.IsAttached() || player.m_attachPoint != transform)
		{
			m_ending = true;
			if ((object)player.GetDoodadController() == this)
			{
				player.StopDoodadControl();
			}
			RestoreCollisions();
			Current = null;
			Destroy(gameObject);
			return false;
		}
		Vector3 axis = path.B - path.A;
		axis.y = 0f;
		RideVisuals.MarkRiding(player, riding: true, axis.sqrMagnitude > 0.0001f ? axis.normalized : player.transform.forward, MainConfig.ziplineHangHeight.Value, hang);
		StartSound();
		ZiplineMessages.Say(player, "$text_zipline_controls", s_toldControls ? null : "$text_zipline_controls_short", MessageHud.MessageType.TopLeft);
		if (MainConfig.ziplineMessages.Value != ZiplineMessages.Level.Off)
		{
			s_toldControls = true;
		}
		return true;
	}

	private static bool IsPullingCart(Player player)
	{
		foreach (Vagon vagon in Vagon.m_instances)
		{
			if (vagon != null && vagon.IsAttached(player))
			{
				return true;
			}
		}
		return false;
	}

	// Distance along the rope from A, whichever end the ride started at
	private float RopeDistance()
	{
		return m_fromB ? m_length - m_s : m_s;
	}

	private void PlaceAttachPoint()
	{
		float s = RopeDistance();
		Vector3 point = m_path.PointAt(s);
		Vector3 tangent = m_path.TangentAt(s);
		if (m_fromB)
		{
			tangent = -tangent;
		}
		m_tangent = tangent;
		Vector3 look = new Vector3(m_look.x, 0f, m_look.z);
		if (look.sqrMagnitude > 0.0001f)
		{
			m_bodyRotation = Quaternion.RotateTowards(m_bodyRotation, Quaternion.LookRotation(look.normalized, Vector3.up), 540f * Time.fixedDeltaTime);
		}
		Vector3 position = point + Vector3.down * MainConfig.ziplineHangHeight.Value;
		transform.SetPositionAndRotation(position, m_bodyRotation);
		if (m_rigidbody != null)
		{
			m_rigidbody.position = position;
			m_rigidbody.rotation = m_bodyRotation;
			m_rigidbody.linearVelocity = tangent * m_v;
			m_rigidbody.angularVelocity = Vector3.zero;
		}
	}

	private void FixedUpdate()
	{
		if (m_ending)
		{
			return;
		}
		try
		{
			if (!StillHolding())
			{
				End(EndReason.Interrupted);
				return;
			}
			bool slidBack = Step(Time.fixedDeltaTime);
			PlaceAttachPoint();
			if (m_s >= m_length - EndMargin)
			{
				End(EndReason.Arrived);
			}
			else if (slidBack)
			{
				End(EndReason.ArrivedBack);
			}
		}
		catch (Exception ex)
		{
			ZiplineMessages.LogWarning("Zipline ride failed, letting go: " + ex);
			End(EndReason.Interrupted);
		}
	}

	// Moves the rider along the rope; true once they have slid back to the post they started from
	private bool Step(float dt)
	{
		float gravity = -9.81f * MainConfig.ziplineGravity.Value * m_tangent.y;
		float acceleration = gravity;
		if (Mathf.Abs(m_v) > 0.05f)
		{
			acceleration -= 0.2f * Mathf.Sign(m_v);
		}
		acceleration -= 0.0035f * m_v * Mathf.Abs(m_v);
		m_v += acceleration * dt;
		float gravityStep = Mathf.Abs(gravity) * dt;
		if (m_input.z < -0.1f || m_block)
		{
			m_v = Mathf.MoveTowards(m_v, 0f, MainConfig.ziplineBrakeStrength.Value * dt + gravityStep);
		}
		else
		{
			float pullSpeed = MainConfig.ziplinePullSpeed.Value;
			bool pulling = m_input.z > 0.1f;
			bool holding = false;
			if (!pulling)
			{
				m_resting = false;
			}
			if (pulling && m_v < pullSpeed)
			{
				float stamina = MainConfig.ziplinePullStamina.Value * dt;
				if (m_player.IsEncumbered())
				{
					holding = true;
					if (!m_toldHeavy)
					{
						m_toldHeavy = true;
						ZiplineMessages.Say(m_player, "$text_zipline_too_heavy", "$text_zipline_too_heavy_short");
					}
				}
				else if (m_resting && stamina > 0f && m_player.GetStamina() < 0.3f * m_player.GetMaxStamina())
				{
					holding = true;
				}
				else if (stamina <= 0f || m_player.HaveStamina(stamina))
				{
					m_resting = false;
					if (stamina > 0f)
					{
						m_player.UseStamina(stamina);
					}
					m_v = Mathf.MoveTowards(m_v, pullSpeed, 6f * dt + Mathf.Max(0f, -gravity) * dt);
				}
				else
				{
					holding = true;
					if (!m_resting)
					{
						m_resting = true;
						ZiplineMessages.Say(m_player, "$text_zipline_out_of_stamina", "$text_zipline_out_of_stamina_short");
					}
				}
			}
			// Holding on uphill keeps the rider where they are instead of sliding back
			if (holding && m_v < 0.5f && gravity < 0f)
			{
				m_v = Mathf.MoveTowards(m_v, 0f, 6f * dt - gravity * dt);
			}
			float crawl = MainConfig.ziplineCrawlSpeed.Value;
			if (crawl > 0f && m_v < crawl)
			{
				m_v = Mathf.MoveTowards(m_v, crawl, 4f * dt + Mathf.Max(0f, -gravity) * dt);
			}
		}
		if (MainConfig.ziplineAutoBrake.Value)
		{
			float arrival = Mathf.Max(MainConfig.ziplineArrivalSpeed.Value, MainConfig.ziplineCrawlSpeed.Value);
			float left = m_length - EndMargin - m_s;
			if (m_v > arrival)
			{
				float brakingDistance = (m_v * m_v - arrival * arrival) / 12f;
				if (left <= brakingDistance + 1f)
				{
					m_v = Mathf.Max(arrival, m_v - 6f * dt - Mathf.Max(0f, gravity) * dt);
				}
			}
		}
		float maxSpeed = MainConfig.ziplineMaxSpeed.Value;
		m_v = Mathf.Clamp(m_v, -maxSpeed, maxSpeed);
		m_s += m_v * dt;
		if (m_s > StartOffset)
		{
			m_pinnedAtStart = 0f;
			return false;
		}
		bool fastBack = m_v < -0.3f;
		m_s = StartOffset;
		if (m_v < 0f)
		{
			m_v = 0f;
		}
		bool pinned = m_v <= 0.05f && m_input.z <= 0.1f && gravity < 0f;
		m_pinnedAtStart = pinned ? m_pinnedAtStart + dt : 0f;
		return fastBack || m_pinnedAtStart > 0.5f;
	}

	private bool StillHolding()
	{
		if (m_player == null || m_player.IsDead() || m_player.IsTeleporting())
		{
			return false;
		}
		if ((object)m_player.GetDoodadController() != this)
		{
			return false;
		}
		return m_player.IsAttached() && m_player.m_attachPoint == transform;
	}

	private void Update()
	{
		if (m_ending)
		{
			return;
		}
		try
		{
			if (!StillHolding())
			{
				End(EndReason.Interrupted);
				return;
			}
			if (Time.time >= m_nextRopeCheck)
			{
				m_nextRopeCheck = Time.time + 0.5f;
				if (!RopeStillThere())
				{
					End(EndReason.Interrupted);
					ZiplineMessages.Say(m_player, "$text_zipline_came_down", "$text_zipline_came_down");
					return;
				}
			}
			if (!m_farEndIgnored && Time.time >= m_nextScan)
			{
				m_nextScan = Time.time + 0.5f;
				ZiplineAnchor anchor = ZiplineAnchor.FindAt(m_farEnd, ZiplineRope.EndTolerance);
				if (anchor != null)
				{
					IgnoreCollidersOf(anchor.gameObject);
					m_farEndIgnored = true;
				}
			}
			if (m_audio != null)
			{
				UpdateSound();
			}
		}
		catch (Exception ex)
		{
			ZiplineMessages.LogDebug("Zipline update failed: " + ex.Message);
		}
	}

	private bool RopeStillThere()
	{
		if (ZiplineRope.Connects(m_path.A, m_path.B))
		{
			return true;
		}
		// A rope whose middle is not loaded yet is not known to be gone
		return ZNetScene.instance == null || !ZNetScene.instance.IsAreaReady((m_path.A + m_path.B) * 0.5f);
	}

	private void End(EndReason reason)
	{
		if (m_ending)
		{
			return;
		}
		m_ending = true;
		try
		{
			Player player = m_player;
			if (player == null)
			{
				return;
			}
			if (reason != EndReason.LetGo && (object)player.GetDoodadController() == this)
			{
				player.StopDoodadControl();
			}
			if (player.IsAttached() && player.m_attachPoint == transform)
			{
				player.AttachStop();
			}
			RestoreCollisions();
			Rigidbody body = player.m_body;
			if (body != null && !player.IsDead())
			{
				Vector3 flat = new Vector3(m_tangent.x, 0f, m_tangent.z).normalized;
				switch (reason)
				{
				case EndReason.LetGo:
					body.linearVelocity = m_tangent * Mathf.Clamp(m_v, -18f, 18f);
					break;
				case EndReason.Arrived:
					body.linearVelocity = flat * Mathf.Min(Mathf.Abs(m_v), 3f);
					break;
				case EndReason.ArrivedBack:
					body.linearVelocity = -flat * Mathf.Min(Mathf.Abs(m_v), 3f);
					break;
				}
			}
		}
		catch (Exception ex)
		{
			ZiplineMessages.LogWarning("Ending the zipline ride failed: " + ex.Message);
		}
		finally
		{
			NoGrabUntil = Time.time + 1.5f;
			RideVisuals.MarkRiding(m_player, riding: false, Vector3.zero, 0f, hands: false);
			StopSound();
			if (Current == this)
			{
				Current = null;
			}
			Destroy(gameObject);
		}
	}

	private void OnDestroy()
	{
		if (!m_ending)
		{
			m_ending = true;
			RestoreCollisions();
			RideVisuals.MarkRiding(m_player, riding: false, Vector3.zero, 0f, hands: false);
		}
		if (Current == this)
		{
			Current = null;
		}
	}

	// Saving while riding would otherwise bring the player back in mid-air on the next login
	internal static void SaveSafeLogoutPoint(PlayerProfile profile)
	{
		ZiplineRider current = Current;
		if (current == null || current.m_ending || profile == null || ZNet.instance == null || !MainConfig.ziplineGroundAfterLogout.Value)
		{
			return;
		}
		Player player = current.m_player;
		if (player == null || player != Player.m_localPlayer || player.IsDead())
		{
			return;
		}
		try
		{
			Vector3 spot = current.SafeSpot(out string where);
			profile.SetLogoutPoint(spot);
			if (s_logoutLogged < 5)
			{
				s_logoutLogged++;
				ZiplineMessages.LogInfo("Saved while riding a zipline: the next login starts on " + where + " at " + spot.ToString("F1") + " instead of in the air.");
			}
		}
		catch (Exception ex)
		{
			ZiplineMessages.LogWarning("Saving a safe spot to come back to after a zipline ride failed: " + ex.Message);
		}
	}

	private Vector3 SafeSpot(out string where)
	{
		Vector3 startEnd = m_fromB ? m_path.B : m_path.A;
		Vector3 farEnd = m_fromB ? m_path.A : m_path.B;
		bool nearStart = m_s <= m_length * 0.5f;
		if (ByPost(nearStart ? startEnd : farEnd, nearStart ? farEnd : startEnd, out Vector3 spot))
		{
			where = nearStart ? "the ground by the post you set off from" : "the ground by the post you were riding to";
			return spot;
		}
		if (ByPost(nearStart ? farEnd : startEnd, nearStart ? startEnd : farEnd, out spot))
		{
			where = nearStart ? "the ground by the post you were riding to" : "the ground by the post you set off from";
			return spot;
		}
		where = "the ground under the rope (neither post is loaded)";
		return GroundUnder(transform.position + Vector3.down * 0.5f);
	}

	// A spot on the ground beside the post at end, trying the side away from the rope first
	private static bool ByPost(Vector3 end, Vector3 other, out Vector3 spot)
	{
		spot = end;
		ZiplineAnchor anchor = ZiplineAnchor.FindAt(end, ZiplineRope.EndTolerance);
		if (anchor == null || ZoneSystem.instance == null)
		{
			return false;
		}
		float bottom = float.MaxValue;
		foreach (Collider collider in anchor.GetComponentsInChildren<Collider>())
		{
			if (collider != null && collider.enabled && !collider.isTrigger)
			{
				bottom = Mathf.Min(bottom, collider.bounds.min.y);
			}
		}
		if (bottom == float.MaxValue)
		{
			bottom = anchor.transform.position.y;
		}
		Vector3 away = other - end;
		away.y = 0f;
		away = away.sqrMagnitude > 0.0001f ? away.normalized : Vector3.forward;
		Vector3 side = new Vector3(away.z, 0f, -away.x);
		bool found = false;
		float highest = float.MinValue;
		foreach (Vector3 direction in new[] { away, -away, side, -side })
		{
			Vector3 probe = new Vector3(end.x, bottom + 1.5f, end.z) + direction * 1.2f;
			if (!ZoneSystem.instance.GetSolidHeight(probe, out float height, 0))
			{
				continue;
			}
			if (height >= bottom - 2.5f && height >= ZoneSystem.instance.m_waterLevel)
			{
				spot = new Vector3(probe.x, height, probe.z);
				return true;
			}
			if (height > highest)
			{
				highest = height;
				found = true;
				spot = new Vector3(probe.x, height, probe.z);
			}
		}
		if (!found)
		{
			spot = new Vector3(end.x, bottom, end.z) + away * 1.2f;
		}
		return true;
	}

	private static Vector3 GroundUnder(Vector3 at)
	{
		ZoneSystem zone = ZoneSystem.instance;
		if (zone == null)
		{
			return at;
		}
		at.y = zone.GetSolidHeight(at, out float height, 0) ? height : zone.GetGroundHeight(at);
		if (at.y < zone.m_waterLevel)
		{
			at.y = zone.m_waterLevel;
		}
		return at;
	}

	// The rider passes through the posts at each end instead of catching on them
	private void IgnoreCollidersOf(GameObject root)
	{
		if (root == null || m_playerCollider == null)
		{
			return;
		}
		foreach (Collider collider in root.GetComponentsInChildren<Collider>())
		{
			if (collider != null && collider.enabled && !collider.isTrigger)
			{
				Physics.IgnoreCollision(m_playerCollider, collider, ignore: true);
				m_ignored.Add(collider);
			}
		}
	}

	private void RestoreCollisions()
	{
		if (m_playerCollider != null)
		{
			foreach (Collider collider in m_ignored)
			{
				if (collider != null)
				{
					Physics.IgnoreCollision(m_playerCollider, collider, ignore: false);
				}
			}
		}
		m_ignored.Clear();
	}

	// The grappling hook's rope sound, looped, its pitch and volume rising with speed
	private void StartSound()
	{
		try
		{
			if (MainConfig.ziplineSoundVolume.Value <= 0f)
			{
				return;
			}
			GameObject prefab = ZiplineVisuals.PullSoundPrefab;
			if (prefab == null || prefab.GetComponent<ZNetView>() != null)
			{
				return;
			}
			m_soundObject = Instantiate(prefab, transform.position + Vector3.up * MainConfig.ziplineHangHeight.Value, Quaternion.identity, transform);
			m_audio = m_soundObject.GetComponentInChildren<AudioSource>();
			if (m_audio == null)
			{
				return;
			}
			m_audio.loop = true;
			m_sfx = m_audio.GetComponent<ZSFX>();
			if (m_sfx != null)
			{
				UpdateSound();
				if (!m_sfx.m_playOnAwake)
				{
					m_sfx.Play();
				}
			}
			else
			{
				m_basePitch = m_audio.pitch > 0.01f ? m_audio.pitch : 1f;
				m_baseVolume = m_audio.volume;
				UpdateSound();
				if (!m_audio.isPlaying)
				{
					m_audio.Play();
				}
			}
		}
		catch (Exception ex)
		{
			ZiplineMessages.LogDebug("Zipline ride sound failed: " + ex.Message);
		}
	}

	private void UpdateSound()
	{
		float speed = Mathf.Clamp01(Mathf.Abs(m_v) / Mathf.Max(1f, MainConfig.ziplineMaxSpeed.Value));
		float pitch = 0.75f + 0.6f * speed;
		float volume = MainConfig.ziplineSoundVolume.Value * Mathf.Clamp01(0.25f + speed);
		if (m_sfx != null)
		{
			m_sfx.SetPitchModifier(pitch);
			m_sfx.SetVolumeModifier(volume);
		}
		else
		{
			m_audio.pitch = m_basePitch * pitch;
			m_audio.volume = m_baseVolume * volume;
		}
	}

	private void StopSound()
	{
		try
		{
			if (m_sfx != null)
			{
				m_sfx.Stop();
			}
		}
		catch (Exception ex)
		{
			ZiplineMessages.LogDebug("Stopping the zipline ride sound failed: " + ex.Message);
		}
		if (m_soundObject != null)
		{
			Destroy(m_soundObject);
		}
		m_soundObject = null;
		m_audio = null;
		m_sfx = null;
	}

	public void OnUseStop(Player player)
	{
		if (!m_ending)
		{
			End(EndReason.LetGo);
		}
	}

	public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block)
	{
		m_input = moveDir;
		m_block = block;
		m_look = lookDir;
	}

	public Component GetControlledComponent()
	{
		return this;
	}

	public Vector3 GetPosition()
	{
		return transform.position;
	}

	public bool IsValid()
	{
		return !m_ending && this != null;
	}
}
