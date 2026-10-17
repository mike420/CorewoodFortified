using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace CorewoodFortified;

// The organ cannon is a vanilla Turret built around the model. The whole cannon turns on the spot towards the target,
// its wheels rolling opposite ways, and every volley fires one cannon ball from each of its barrels in turn.
// Loading, trophy targets, hover text and range circle are all the vanilla turret's
internal sealed class OrganCannon : MonoBehaviour
{
	public const string PrefabName = "mm_corewood_organ_cannon";

	// What the cannon takes as ammo, and what a cannon ball is
	public const string AmmoType = "$ammo_mm_cannonball";

	private const string BarrelsChild = "barrels";

	// Meshes with this in their name are the wheels, which roll as the cannon turns
	private const string WheelName = "wheel";

	// How far in front of a barrel's mouth a ball starts, so it does not start inside the barrel
	private const float MuzzleClearance = 0.2f;

	// Vertices this close to a barrel's front count as its mouth
	private const float MouthDepth = 0.05f;

	// The vanilla ballista's settling, which the barrels share so they keep up with a moving target
	private const float LookAcceleration = 8f;

	private const float LookDeacceleration = 8f;

	private const float LookMinDegreesDelta = 0.01f;

	private const float TurnRate = 60f;

	private const float SweepTime = 8f;

	private static readonly List<Turret> s_turrets = new List<Turret>();

	private static GameObject s_prefab;

	// Each barrel's mouth, in the space of the turret's neck, which turns the model side to side but does not tilt,
	// as the barrels do not
	public List<Vector3> m_muzzles = new List<Vector3>();

	private Turret m_turret;

	private int m_nextBarrel;

	private void Awake()
	{
		m_turret = GetComponent<Turret>();
		if (m_turret)
		{
			s_turrets.Add(m_turret);
		}
	}

	private void OnDestroy()
	{
		s_turrets.Remove(m_turret);
	}

	// Turret.ShootProjectile, firing one ball from each barrel in turn instead of one from the eye
	private void FireVolley()
	{
		Turret turret = m_turret;
		ZDO zdo = turret.m_nview.GetZDO();
		Transform neck = turret.m_turretNeck.transform;
		Transform body = turret.m_turretBody.transform;
		Transform eye = turret.m_eye.transform;
		turret.m_shootEffect.Create(eye.position, eye.rotation);
		zdo.Set(ZDOVars.s_lastAttack, (float)ZNet.instance.GetTimeSeconds());
		turret.m_lastAmmo = turret.GetAmmoItem();
		ItemDrop.ItemData ball = turret.m_lastAmmo;
		if (ball == null || !ball.m_shared.m_attack.m_attackProjectile || m_muzzles.Count == 0)
		{
			return;
		}
		int ammo = zdo.GetInt(ZDOVars.s_ammo);
		int count = Mathf.Min(MainConfig.cannonVolleySize.Value, ammo, m_muzzles.Count);
		zdo.Set(ZDOVars.s_ammo, ammo - count);
		float accuracy = ball.m_shared.m_attack.m_projectileAccuracy;
		for (int i = 0; i < count; i++)
		{
			// The ball leaves the barrel's mouth, but along the body, which also tilts to aim
			Vector3 position = neck.TransformPoint(m_muzzles[m_nextBarrel]);
			m_nextBarrel = (m_nextBarrel + 1) % m_muzzles.Count;
			Vector3 direction = body.forward;
			Vector3 side = Vector3.Cross(direction, Vector3.up);
			direction = Quaternion.AngleAxis(Random.Range(-accuracy, accuracy), side) * direction;
			direction = Quaternion.AngleAxis(Random.Range(-accuracy, accuracy), Vector3.up) * direction;
			GameObject projectile = Instantiate(ball.m_shared.m_attack.m_attackProjectile, position, Quaternion.LookRotation(direction));
			HitData hit = new HitData
			{
				m_toolTier = (short)ball.m_shared.m_toolTier,
				m_pushForce = ball.m_shared.m_attackForce,
				m_backstabBonus = ball.m_shared.m_backstabBonus,
				m_staggerMultiplier = ball.m_shared.m_attack.m_staggerMultiplier,
				m_blockable = ball.m_shared.m_blockable,
				m_dodgeable = ball.m_shared.m_dodgeable,
				m_skill = ball.m_shared.m_skillType,
				m_itemWorldLevel = (byte)Game.m_worldLevel,
				m_hitType = HitData.HitType.Turret
			};
			hit.m_damage.Add(ball.GetDamage());
			projectile.GetComponent<IProjectile>()?.Setup(null, direction * ball.m_shared.m_attack.m_projectileVel, turret.m_hitNoise, hit, null, ball);
			projectile.AddComponent<CannonBallShot>();
			turret.m_lastProjectile = projectile;
		}
	}

	// The vanilla hover offers to add a missile, which the cannon names as a cannon ball
	[HarmonyPatch(typeof(Turret), nameof(Turret.GetHoverText))]
	private static class Turret_GetHoverText
	{
		private static void Postfix(Turret __instance, ref string __result)
		{
			if (string.IsNullOrEmpty(__result) || !__instance.GetComponent<OrganCannon>())
			{
				return;
			}
			__result = __result.Replace(Localization.instance.Localize("$piece_turret_addammo"), Localization.instance.Localize("$text_cannon_add_ammo"));
		}
	}

	[HarmonyPatch(typeof(Turret), nameof(Turret.ShootProjectile))]
	private static class Turret_ShootProjectile
	{
		private static bool Prefix(Turret __instance)
		{
			OrganCannon cannon = __instance.GetComponent<OrganCannon>();
			if (!cannon)
			{
				return true;
			}
			cannon.FireVolley();
			return false;
		}
	}

	// Builds the turret on the prefab. The model's barrels face along its side, so the whole model is first turned
	// to face the piece's front, which is where a turret looks. The model and its colliders then go under the
	// turret's neck, which only turns side to side, about the middle between the wheels, so the whole cannon turns
	// on the spot. The turret's body is unseen: it also tilts to aim, and the balls fire along it from each barrel's
	// mouth, which is found from its mesh and turns with the neck
	public static bool Setup(GameObject piece, ItemDrop ball, Turret vanilla, EffectList shootEffect)
	{
		Transform root = piece.transform;
		Transform barrels = Utils.FindChild(root, BarrelsChild);
		if (!barrels)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " has no '" + BarrelsChild + "' child, it cannot be a cannon.");
			return false;
		}
		MeshFilter[] barrelMeshes = barrels.GetComponentsInChildren<MeshFilter>(includeInactive: true);
		if (barrelMeshes.Length == 0)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + piece.name + " has no barrel meshes under '" + BarrelsChild + "', it cannot be a cannon.");
			return false;
		}

		FaceForward(root, barrels, barrelMeshes);

		List<Transform> wheels = new List<Transform>();
		foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
		{
			if (child.name.IndexOf(WheelName, System.StringComparison.OrdinalIgnoreCase) >= 0 && child.GetComponent<MeshFilter>())
			{
				wheels.Add(child);
			}
		}
		Vector3 pivot = Vector3.zero;
		foreach (Transform wheel in wheels)
		{
			pivot += MeshBounds(root, wheel).center;
		}
		pivot = wheels.Count > 0 ? pivot / wheels.Count : root.InverseTransformPoint(barrels.position);
		pivot.y = 0f;

		Transform neck = AddChild(root, "_cannon_neck", pivot);
		foreach (Transform child in new List<Transform>(root.Cast<Transform>()))
		{
			if (child != neck)
			{
				child.SetParent(neck, worldPositionStays: true);
			}
		}
		CannonWheels.Setup(piece, neck, wheels);

		Vector3 bodyPosition = pivot;
		bodyPosition.y = root.InverseTransformPoint(barrels.position).y;
		Transform body = AddChild(root, "_cannon_body", bodyPosition);
		OrganCannon cannon = piece.AddComponent<OrganCannon>();
		foreach (MeshFilter barrel in barrelMeshes)
		{
			cannon.m_muzzles.Add(FindMuzzle(barrel, neck));
		}
		// The volley runs from one side to the other
		cannon.m_muzzles.Sort((a, b) => a.x.CompareTo(b.x));
		Vector3 eye = Vector3.zero;
		foreach (Vector3 muzzle in cannon.m_muzzles)
		{
			eye += muzzle;
		}
		// The neck and body do not turn yet, so the middle mouth carries over to the body as it stands
		eye = body.InverseTransformPoint(neck.TransformPoint(eye / cannon.m_muzzles.Count));

		Turret turret = piece.AddComponent<Turret>();
		turret.m_name = piece.GetComponent<Piece>().m_name;
		turret.m_turretBody = body.gameObject;
		turret.m_turretNeck = neck.gameObject;
		turret.m_turretBodyArmed = AddChild(root, "_cannon_armed", Vector3.zero).gameObject;
		turret.m_turretBodyUnarmed = AddChild(root, "_cannon_unarmed", Vector3.zero).gameObject;
		turret.m_eye = AddChild(body, "_cannon_eye", eye).gameObject;
		AddChild(root, "_cannon_marker", Vector3.zero).gameObject.AddComponent<CircleProjector>();
		turret.m_ammoType = AmmoType;
		turret.m_defaultAmmo = null;
		turret.m_allowedAmmo = new List<Turret.AmmoType>
		{
			new Turret.AmmoType { m_ammo = ball, m_visual = AddChild(body, "_cannon_ball_visual", Vector3.zero).gameObject }
		};
		turret.m_turnRate = TurnRate;
		turret.m_noTargetScanRate = SweepTime;
		turret.m_lookAcceleration = LookAcceleration;
		turret.m_lookDeacceleration = LookDeacceleration;
		turret.m_lookMinDegreesDelta = LookMinDegreesDelta;
		turret.m_targetTamedConfig = false;
		turret.m_shootEffect = shootEffect;
		if (vanilla)
		{
			turret.m_attackWarmup = vanilla.m_attackWarmup;
			turret.m_hitNoise = vanilla.m_hitNoise;
			turret.m_shootWhenAimDiff = vanilla.m_shootWhenAimDiff;
			turret.m_updateTargetIntervalNear = vanilla.m_updateTargetIntervalNear;
			turret.m_updateTargetIntervalFar = vanilla.m_updateTargetIntervalFar;
			turret.m_holdRepeatInterval = vanilla.m_holdRepeatInterval;
			turret.m_configTargets = new List<Turret.TrophyTarget>(vanilla.m_configTargets);
			turret.m_maxConfigTargets = vanilla.m_maxConfigTargets;
			turret.m_addAmmoEffect = vanilla.m_addAmmoEffect;
			turret.m_reloadEffect = vanilla.m_reloadEffect;
			turret.m_warmUpStartEffect = vanilla.m_warmUpStartEffect;
			turret.m_newTargetEffect = vanilla.m_newTargetEffect;
			turret.m_lostTargetEffect = vanilla.m_lostTargetEffect;
			turret.m_setTargetEffect = vanilla.m_setTargetEffect;
			turret.m_markerHideTime = vanilla.m_markerHideTime;
		}

		if (!s_prefab)
		{
			MainConfig.cannonRange.ConfigFile.SettingChanged += OnSettingChanged;
		}
		s_prefab = piece;
		Apply(turret);
		return true;
	}

	// Turns the model about its up axis so the barrels point along the piece's front
	private static void FaceForward(Transform root, Transform barrels, MeshFilter[] barrelMeshes)
	{
		Bounds bounds = MeshBounds(root, barrelMeshes[0].transform);
		foreach (MeshFilter barrel in barrelMeshes)
		{
			bounds.Encapsulate(MeshBounds(root, barrel.transform));
		}
		// The barrels run along the longer of the two flat axes, out from their pivot
		Vector3 fromPivot = bounds.center - root.InverseTransformPoint(barrels.position);
		Vector3 facing = bounds.size.x > bounds.size.z ? new Vector3(Mathf.Sign(fromPivot.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(fromPivot.z));
		float angle = Vector3.SignedAngle(facing, Vector3.forward, Vector3.up);
		if (Mathf.Abs(angle) < 1f)
		{
			return;
		}
		Quaternion turn = Quaternion.Euler(0f, angle, 0f);
		foreach (Transform child in root)
		{
			child.localPosition = turn * child.localPosition;
			child.localRotation = turn * child.localRotation;
		}
	}

	// The middle of the barrel's mouth: the mean of its frontmost vertices, or the front of its bounds when the mesh
	// cannot be read
	private static Vector3 FindMuzzle(MeshFilter barrel, Transform space)
	{
		Mesh mesh = barrel.sharedMesh;
		if (!mesh)
		{
			return space.InverseTransformPoint(barrel.transform.position);
		}
		Vector3 muzzle;
		if (mesh.isReadable)
		{
			Vector3[] vertices = mesh.vertices;
			float front = float.MinValue;
			for (int i = 0; i < vertices.Length; i++)
			{
				vertices[i] = space.InverseTransformPoint(barrel.transform.TransformPoint(vertices[i]));
				front = Mathf.Max(front, vertices[i].z);
			}
			Vector3 sum = Vector3.zero;
			int count = 0;
			foreach (Vector3 vertex in vertices)
			{
				if (vertex.z >= front - MouthDepth)
				{
					sum += vertex;
					count++;
				}
			}
			muzzle = sum / count;
		}
		else
		{
			Bounds local = mesh.bounds;
			muzzle = space.InverseTransformPoint(barrel.transform.TransformPoint(local.center));
			muzzle.z = space.InverseTransformPoint(barrel.transform.TransformPoint(local.max)).z;
		}
		return muzzle + Vector3.forward * MuzzleClearance;
	}

	// The box round a mesh, in the space of the given transform
	public static Bounds MeshBounds(Transform space, Transform meshTransform)
	{
		MeshFilter filter = meshTransform.GetComponent<MeshFilter>();
		Bounds bounds = new Bounds(space.InverseTransformPoint(meshTransform.position), Vector3.zero);
		if (!filter || !filter.sharedMesh)
		{
			return bounds;
		}
		Bounds local = filter.sharedMesh.bounds;
		for (int i = 0; i < 8; i++)
		{
			Vector3 corner = local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
			Vector3 point = space.InverseTransformPoint(meshTransform.TransformPoint(corner));
			if (i == 0)
			{
				bounds = new Bounds(point, Vector3.zero);
			}
			bounds.Encapsulate(point);
		}
		return bounds;
	}

	// A new empty child. The parent is a prefab, so nothing on the child wakes until the piece is placed
	private static Transform AddChild(Transform parent, string name, Vector3 localPosition)
	{
		Transform child = new GameObject(name).transform;
		child.SetParent(parent, worldPositionStays: false);
		child.localPosition = localPosition;
		child.localRotation = Quaternion.identity;
		return child;
	}

	private static void OnSettingChanged(object sender, SettingChangedEventArgs e)
	{
		if (e.ChangedSetting.Definition.Section != MainConfig.cannonRange.Definition.Section)
		{
			return;
		}
		if (s_prefab)
		{
			Apply(s_prefab.GetComponent<Turret>());
		}
		foreach (Turret turret in s_turrets)
		{
			Apply(turret);
		}
	}

	private static void Apply(Turret turret)
	{
		if (!turret)
		{
			return;
		}
		turret.m_viewDistance = MainConfig.cannonRange.Value;
		turret.m_horizontalAngle = MainConfig.cannonFireArc.Value / 2f;
		turret.m_attackCooldown = MainConfig.cannonReloadTime.Value;
		turret.m_maxAmmo = MainConfig.cannonMaxAmmo.Value;
		turret.m_targetPlayers = MainConfig.cannonTargetPlayers.Value;
		turret.m_targetTamed = MainConfig.cannonTargetTamed.Value;
		turret.m_targetEnemies = MainConfig.cannonTargetEnemies.Value;
		// The turret sizes its range circle to the range only when it wakes, so a placed one is resized here
		if (turret.m_marker)
		{
			turret.m_marker.m_radius = turret.m_viewDistance;
		}
	}
}
