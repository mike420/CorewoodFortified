using System;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CorewoodFortified;

// Picking up a fortified ballista or organ cannon and putting it down somewhere else (this game's player only), as the
// YabbaDabbaDoo car is moved. Look at it and press the move key: a copy of it follows where you aim, red where it
// cannot go. Scroll turns it, attack puts it there, and block or the move key again leaves it where it was.
//
// It stays where it is until it is put down; then MovableTurret moves it for every game at once
internal static class TurretMover
{
	// From the player to the turret, to pick it up
	private const float GrabRange = 8f;

	// From the player to where it goes
	private const float PlaceRange = 12f;

	private const float AimRange = 40f;

	// Degrees each click of the scroll wheel turns it
	private const float TurnStep = 22.5f;

	// No steeper than this where it is put down, in degrees
	private const float SteepestPlace = 30f;

	// The bottom of the turret not counted when looking for what is in its way, so what it stands on does not count
	private const float FootHeight = 0.3f;

	private const float LineWidth = 0.06f;

	private const float RingRadius = 1f;

	private const int RingPoints = 32;

	private static readonly int ColorId = Shader.PropertyToID("_Color");

	private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

	private static readonly Color Blocked = new Color(0.95f, 0.25f, 0.2f);

	private static readonly RaycastHit[] s_hits = new RaycastHit[32];

	private static readonly Collider[] s_overlaps = new Collider[64];

	private static int s_groundMask = -1;

	private static int s_blockMask = -1;

	private static MovableTurret s_turret;

	private static GameObject s_ghost;

	private static float s_yaw;

	private static bool s_valid;

	// The ghost's tint as last set: -1 none yet
	private static int s_tinted = -1;

	private static Vector3 s_origin;

	private static float s_scroll;

	private static bool s_readingScroll;

	// The click that put it down or left it does not also swing a weapon
	private static float s_quietUntil;

	// The turret's solid parts, in its own space, without its foot
	private static Bounds s_body;

	private static LineRenderer s_line, s_fromRing, s_toRing;

	private static Texture2D s_dash;

	// A turret is being moved: the scroll wheel turns it rather than zooming, and the mouse buttons do not attack or block
	public static bool Active => s_turret;

	public static bool QuietCombat => Active || Time.time < s_quietUntil;

	public static bool ScrollTaken => Active && !s_readingScroll;

	// The line under the turret's name: the key that picks it up, when this player could
	public static string HoverLine(MovableTurret turret)
	{
		Player me = Player.m_localPlayer;
		KeyboardShortcut key = MainConfig.turretMoveKey.Value;
		if (key.MainKey == KeyCode.None || !me || Active || !turret.CanBeMoved() || !CanPickUp(me, turret, out _))
		{
			return "";
		}
		return "\n[<color=yellow><b>" + key + "</b></color>] $text_turret_move";
	}

	// Every frame, from the plugin
	public static void Tick()
	{
		Player me = Player.m_localPlayer;
		// The turret was broken or unloaded while being moved: its copy and lines are cleared away
		if (!Active && s_ghost)
		{
			Stop("$text_turret_move_cancelled");
			return;
		}
		if (Active)
		{
			if (!me || me.IsDead() || me.IsAttached() || me.InPlaceMode() || !s_turret.CanBeMoved())
			{
				Stop("$text_turret_move_cancelled");
				return;
			}
			Moving(me);
			return;
		}
		if (!me || Typing() || !Pressed() || EasyRelocateCompat.Busy)
		{
			return;
		}
		GameObject hover = me.GetHoverObject();
		MovableTurret turret = hover ? hover.GetComponentInParent<MovableTurret>() : null;
		if (!turret || !turret.CanBeMoved())
		{
			return;
		}
		if (!CanPickUp(me, turret, out string why))
		{
			if (why != null)
			{
				me.Message(MessageHud.MessageType.Center, why);
			}
			return;
		}
		Start(me, turret);
	}

	private static bool CanPickUp(Player me, MovableTurret turret, out string why)
	{
		why = null;
		if (me.IsAttached() || me.InPlaceMode() || me.IsDead())
		{
			return false;
		}
		if (Flat(me.transform.position - turret.transform.position).magnitude > GrabRange)
		{
			why = "$piece_toofar";
			return false;
		}
		if (!PrivateArea.CheckAccess(turret.transform.position, 0f, flash: false))
		{
			why = "$msg_privatezone";
			return false;
		}
		return true;
	}

	private static void Start(Player me, MovableTurret turret)
	{
		s_turret = turret;
		s_yaw = turret.transform.eulerAngles.y;
		s_origin = turret.transform.position;
		s_scroll = 0f;
		s_valid = false;
		s_tinted = -1;
		s_body = SolidBounds(turret);
		s_ghost = MakeGhost(turret);
		MakeLines();
		Hud.HidePieceSelection();
		me.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$text_turret_move_help").Replace("{key}", MainConfig.turretMoveKey.Value.ToString()));
	}

	private static void Moving(Player me)
	{
		// Scroll: turn it, a notch at a time, as the hammer does
		s_readingScroll = true;
		s_scroll += ZInput.GetMouseScrollWheel();
		s_readingScroll = false;
		const float notch = 0.05f;
		while (s_scroll > notch)
		{
			s_scroll -= notch * 2f;
			s_yaw += TurnStep;
		}
		while (s_scroll < -notch)
		{
			s_scroll += notch * 2f;
			s_yaw -= TurnStep;
		}
		s_scroll = Mathf.Clamp(s_scroll, -notch, notch);

		bool found = Aim(out Vector3 at, out Vector3 normal, out Collider ground);
		Quaternion rotation = Quaternion.Euler(0f, s_yaw, 0f);
		if (found)
		{
			s_ghost.transform.SetPositionAndRotation(at, rotation);
		}
		s_ghost.SetActive(found);
		s_valid = found && Fits(me, at, rotation, normal, ground);
		if ((s_valid ? 1 : 0) != s_tinted)
		{
			s_tinted = s_valid ? 1 : 0;
			Tint(s_valid);
		}
		ShowLines(found, at);

		bool busy = Typing() || Hud.InRadial();
		if (!busy && (ZInput.GetButtonDown("Attack") || ZInput.GetButtonDown("JoyPlace")))
		{
			if (!s_valid)
			{
				me.Message(MessageHud.MessageType.Center, "$msg_invalidplacement");
				return;
			}
			s_turret.MoveTo(at, rotation);
			Stop("$text_turret_moved");
		}
		else if (!busy && (ZInput.GetButtonDown("Block") || Pressed()))
		{
			Stop("$text_turret_move_cancelled");
		}
	}

	private static void Stop(string message)
	{
		s_turret = null;
		s_quietUntil = Time.time + 0.5f;
		if (s_ghost)
		{
			Object.Destroy(s_ghost);
			s_ghost = null;
		}
		ShowLines(false, Vector3.zero);
		if (Player.m_localPlayer)
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, message);
		}
	}

	// ---- where it goes ----------------------------------------------------------------------------------------------

	// Where the player is aiming: the turret stands there, upright
	private static bool Aim(out Vector3 at, out Vector3 normal, out Collider ground)
	{
		at = Vector3.zero;
		normal = Vector3.up;
		ground = null;
		Camera camera = Utils.GetMainCamera();
		if (!camera)
		{
			return false;
		}
		Masks();
		return Cast(camera.transform.position, camera.transform.forward, AimRange, out at, out normal, out ground);
	}

	// It can go there: near enough, not too steep, on what its piece may stand on, nothing in the way, and somewhere
	// this player may build
	private static bool Fits(Player me, Vector3 at, Quaternion rotation, Vector3 normal, Collider ground)
	{
		if (Flat(me.transform.position - at).magnitude > PlaceRange)
		{
			return false;
		}
		if (Vector3.Angle(normal, Vector3.up) > SteepestPlace)
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(at, 0f, flash: false) || Location.IsInsideNoBuildLocation(at))
		{
			return false;
		}
		Piece piece = s_turret.GetComponent<Piece>();
		if (piece)
		{
			if (piece.m_noInWater && Floating.GetLiquidLevel(at) > at.y)
			{
				return false;
			}
			WearNTear under = ground ? ground.GetComponentInParent<WearNTear>() : null;
			if (piece.m_notOnWood && under && (under.m_materialType == WearNTear.MaterialType.Wood || under.m_materialType == WearNTear.MaterialType.HardWood))
			{
				return false;
			}
			if (piece.m_groundOnly && (!ground || !ground.GetComponent<Heightmap>()))
			{
				return false;
			}
		}
		if (s_body.size.y <= 0f)
		{
			return true;
		}
		Vector3 centre = at + rotation * s_body.center;
		Vector3 half = s_body.extents * 0.9f;
		int count = Physics.OverlapBoxNonAlloc(centre, half, s_overlaps, rotation, s_blockMask, QueryTriggerInteraction.Ignore);
		for (int i = 0; i < count; i++)
		{
			Collider hit = s_overlaps[i];
			if (hit && hit.GetComponentInParent<MovableTurret>() != s_turret)
			{
				return false;
			}
		}
		return true;
	}

	// The box round the turret's solid parts, in its own space, with its foot cut off
	private static Bounds SolidBounds(MovableTurret turret)
	{
		Transform root = turret.transform;
		bool any = false;
		Bounds bounds = default;
		foreach (Collider collider in turret.GetComponentsInChildren<Collider>())
		{
			if (collider.isTrigger || !collider.enabled)
			{
				continue;
			}
			Bounds world = collider.bounds;
			for (int i = 0; i < 8; i++)
			{
				Vector3 corner = world.center + Vector3.Scale(world.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
				Vector3 point = root.InverseTransformPoint(corner);
				if (!any)
				{
					bounds = new Bounds(point, Vector3.zero);
					any = true;
				}
				bounds.Encapsulate(point);
			}
		}
		if (!any)
		{
			return default;
		}
		Vector3 min = bounds.min;
		min.y = Mathf.Max(min.y, FootHeight);
		Vector3 max = bounds.max;
		if (max.y <= min.y)
		{
			return default;
		}
		bounds.SetMinMax(min, max);
		return bounds;
	}

	// The nearest solid thing along the line, not the turret being moved nor anyone
	private static bool Cast(Vector3 from, Vector3 direction, float range, out Vector3 point, out Vector3 normal, out Collider collider)
	{
		point = Vector3.zero;
		normal = Vector3.up;
		collider = null;
		int count = Physics.RaycastNonAlloc(from, direction, s_hits, range, s_groundMask, QueryTriggerInteraction.Ignore);
		float nearest = float.MaxValue;
		for (int i = 0; i < count; i++)
		{
			RaycastHit hit = s_hits[i];
			if (hit.distance >= nearest || hit.collider.GetComponentInParent<MovableTurret>() == s_turret || hit.collider.GetComponentInParent<Character>())
			{
				continue;
			}
			nearest = hit.distance;
			point = hit.point;
			normal = hit.normal;
			collider = hit.collider;
		}
		return nearest < float.MaxValue;
	}

	private static void Masks()
	{
		if (s_groundMask < 0)
		{
			s_groundMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle");
			s_blockMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "vehicle", "character", "character_net", "character_ghost");
		}
	}

	// ---- what the player sees ---------------------------------------------------------------------------------------

	// A copy of how the turret looks right now: its meshes only, nothing on it works. Its range circle is left out
	private static GameObject MakeGhost(MovableTurret turret)
	{
		GameObject ghost = new GameObject("cwf_turret_ghost");
		Transform root = turret.transform;
		ghost.transform.localScale = root.lossyScale;
		Transform marker = turret.Turret && turret.Turret.m_marker ? turret.Turret.m_marker.transform : null;
		foreach (MeshRenderer renderer in turret.GetComponentsInChildren<MeshRenderer>())
		{
			MeshFilter filter = renderer.GetComponent<MeshFilter>();
			if (!renderer.enabled || !filter || !filter.sharedMesh || (marker && renderer.transform.IsChildOf(marker)))
			{
				continue;
			}
			GameObject part = new GameObject(renderer.name);
			part.transform.SetParent(ghost.transform, false);
			Transform t = renderer.transform;
			part.transform.localPosition = root.InverseTransformPoint(t.position);
			part.transform.localRotation = Quaternion.Inverse(root.rotation) * t.rotation;
			part.transform.localScale = Divide(t.lossyScale, root.lossyScale);
			part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
			MeshRenderer copy = part.AddComponent<MeshRenderer>();
			copy.sharedMaterials = renderer.sharedMaterials;
			copy.shadowCastingMode = renderer.shadowCastingMode;
		}
		ghost.SetActive(false);
		return ghost;
	}

	private static Vector3 Divide(Vector3 a, Vector3 b)
	{
		return new Vector3(a.x / Mathf.Max(0.0001f, b.x), a.y / Mathf.Max(0.0001f, b.y), a.z / Mathf.Max(0.0001f, b.z));
	}

	// As the hammer's ghost: as it is where it can go, red where it cannot
	private static void Tint(bool valid)
	{
		if (!s_ghost || !MaterialMan.instance)
		{
			return;
		}
		if (valid)
		{
			MaterialMan.instance.ResetValue(s_ghost, ColorId);
			MaterialMan.instance.ResetValue(s_ghost, EmissionId);
		}
		else
		{
			MaterialMan.instance.SetValue(s_ghost, ColorId, Blocked);
			MaterialMan.instance.SetValue(s_ghost, EmissionId, Blocked * 0.7f);
		}
	}

	// A dashed line on the ground from where the turret is to where it would go, and a ring at each end
	private static void MakeLines()
	{
		if (s_line)
		{
			return;
		}
		Shader shader = Shader.Find("Sprites/Default");
		if (!shader)
		{
			return;
		}
		s_dash = new Texture2D(8, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
		for (int i = 0; i < 8; i++)
		{
			s_dash.SetPixel(i, 0, i < 5 ? Color.white : Color.clear);
		}
		s_dash.Apply();
		GameObject holder = new GameObject("cwf_turret_move_lines");
		Object.DontDestroyOnLoad(holder);
		s_line = Line(holder, shader, false);
		s_line.textureMode = LineTextureMode.Tile;
		s_line.material.mainTexture = s_dash;
		s_fromRing = Line(holder, shader, true);
		s_toRing = Line(holder, shader, true);
	}

	private static LineRenderer Line(GameObject holder, Shader shader, bool loop)
	{
		GameObject go = new GameObject(loop ? "ring" : "line");
		go.transform.SetParent(holder.transform, false);
		LineRenderer line = go.AddComponent<LineRenderer>();
		line.useWorldSpace = true;
		line.loop = loop;
		line.widthMultiplier = LineWidth;
		line.numCapVertices = 2;
		line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		line.receiveShadows = false;
		line.material = new Material(shader) { renderQueue = 3100 };
		line.startColor = line.endColor = new Color(1f, 1f, 1f, 0.85f);
		line.enabled = false;
		return line;
	}

	private static void ShowLines(bool show, Vector3 to)
	{
		if (!s_line)
		{
			return;
		}
		s_line.enabled = s_fromRing.enabled = s_toRing.enabled = show;
		if (!show)
		{
			return;
		}
		Vector3 from = OnGround(s_origin);
		Vector3 end = OnGround(to);
		s_line.positionCount = 2;
		s_line.SetPosition(0, from);
		s_line.SetPosition(1, end);
		s_line.material.mainTextureScale = new Vector2(Mathf.Max(0.01f, Vector3.Distance(from, end) / 0.5f), 1f);
		Ring(s_fromRing, from);
		Ring(s_toRing, end);
		Color colour = s_valid ? new Color(1f, 1f, 1f, 0.85f) : new Color(Blocked.r, Blocked.g, Blocked.b, 0.85f);
		s_toRing.startColor = s_toRing.endColor = colour;
	}

	private static void Ring(LineRenderer ring, Vector3 centre)
	{
		ring.positionCount = RingPoints;
		for (int i = 0; i < RingPoints; i++)
		{
			float angle = i * Mathf.PI * 2f / RingPoints;
			ring.SetPosition(i, centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * RingRadius);
		}
	}

	private static Vector3 OnGround(Vector3 point)
	{
		Masks();
		Vector3 ground = Cast(point + Vector3.up * 2f, Vector3.down, 8f, out Vector3 hit, out _, out _) ? hit : point;
		ground.y = Mathf.Max(ground.y, Floating.GetLiquidLevel(ground)) + 0.05f;
		return ground;
	}

	// ---- input ------------------------------------------------------------------------------------------------------

	private static bool Pressed()
	{
		KeyboardShortcut shortcut = MainConfig.turretMoveKey.Value;
		if (shortcut.MainKey == KeyCode.None || !BepInEx.UnityInput.Current.GetKeyDown(shortcut.MainKey))
		{
			return false;
		}
		foreach (KeyCode modifier in shortcut.Modifiers)
		{
			if (!BepInEx.UnityInput.Current.GetKey(modifier))
			{
				return false;
			}
		}
		return true;
	}

	private static bool Typing()
	{
		return (Chat.instance && Chat.instance.HasFocus()) || Console.IsVisible() || TextInput.IsVisible() || Menu.IsVisible()
			|| InventoryGui.IsVisible() || Minimap.IsOpen() || StoreGui.IsVisible();
	}

	private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

	// ---- patches ----------------------------------------------------------------------------------------------------

	// The key to move it, under its name
	[HarmonyPatch(typeof(Turret), nameof(Turret.GetHoverText))]
	private static class Turret_GetHoverText
	{
		private static void Postfix(Turret __instance, ref string __result)
		{
			if (string.IsNullOrEmpty(__result))
			{
				return;
			}
			MovableTurret turret = __instance.GetComponent<MovableTurret>();
			if (turret)
			{
				__result += Localization.instance.Localize(HoverLine(turret));
			}
		}
	}

	// While a turret is being moved the scroll wheel turns it, so the camera does not zoom with it
	[HarmonyPatch(typeof(ZInput), nameof(ZInput.GetMouseScrollWheel))]
	private static class ZInput_GetMouseScrollWheel
	{
		private static void Postfix(ref float __result)
		{
			if (ScrollTaken)
			{
				__result = 0f;
			}
		}
	}

	// While a turret is being moved, and just after, attack puts it down and block leaves it: neither swings or raises
	// anything
	[HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
	private static class Player_SetControls
	{
		private static void Prefix(Player __instance, ref bool attack, ref bool attackHold, ref bool secondaryAttack, ref bool secondaryAttackHold, ref bool block, ref bool blockHold)
		{
			if (__instance == Player.m_localPlayer && QuietCombat)
			{
				attack = attackHold = secondaryAttack = secondaryAttackHold = block = blockHold = false;
			}
		}
	}
}

// Easy Relocate (ModdedWolf.EasyRelocate) moves any built piece with its own key. With it installed, the key press
// that starts, puts down or cancels one of the two does not also start the other. Patched by hand, only when it is
// there
internal static class EasyRelocateCompat
{
	public const string Guid = "ModdedWolf.EasyRelocate";

	private static Func<bool> s_busy;

	// Easy Relocate is moving something, or has only just put it down or cancelled it
	public static bool Busy
	{
		get
		{
			try
			{
				return s_busy != null && s_busy();
			}
			catch
			{
				s_busy = null;
				return false;
			}
		}
	}

	public static void Apply(Harmony harmony)
	{
		if (!Chainloader.PluginInfos.TryGetValue(Guid, out BepInEx.PluginInfo info) || info.Instance == null)
		{
			return;
		}
		try
		{
			Assembly assembly = info.Instance.GetType().Assembly;
			Type controller = assembly.GetType("EasyRelocate.RelocateController");
			const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic;
			MethodInfo begin = controller?.GetMethod("TryBeginRelocate", any | BindingFlags.Instance);
			MethodInfo busy = controller?.GetProperty("ShouldSuppressCombatInput", any | BindingFlags.Static)?.GetGetMethod(true);
			if (begin != null)
			{
				harmony.Patch(begin, prefix: new HarmonyMethod(typeof(EasyRelocateCompat), nameof(NotWhileMovingTurret)));
			}
			if (busy != null && busy.ReturnType == typeof(bool))
			{
				s_busy = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), busy);
			}
			if (begin == null || s_busy == null)
			{
				Jotunn.Logger.LogWarning("[CorewoodFortified] Easy Relocate " + info.Metadata.Version + " has changed: its key and the turret move key may both act on one press.");
			}
		}
		catch (Exception ex)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] Could not keep Easy Relocate and the turret move key apart: " + ex.Message);
		}
	}

	private static bool NotWhileMovingTurret() => !TurretMover.QuietCombat;
}
