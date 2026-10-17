using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace CorewoodFortified;

// The organ cannon's ammo. The bundle's cannon ball is made an ammo item that fires a cannon ball projectile, built
// from the vanilla ballista bolt's with the bolt's model swapped for the ball's. Every trader sells it
internal static class CannonBall
{
	public const string PrefabName = "mm_cannon_ball";

	private const string ProjectileName = "mm_cannon_ball_projectile";

	private const string VisualChild = "attach";

	// Ten balls for one coin at every trader
	private const int TradeStack = 10;

	private const int TradePrice = 1;

	private const float Speed = 40f;

	private const float Gravity = 5f;

	private const float SpreadDegrees = 3f;

	private const float PushForce = 60f;

	private static ItemDrop s_ball;

	public static ItemDrop Add(Turret vanilla)
	{
		GameObject prefab = CorewoodFortifiedMod.cwfortified.LoadAsset<GameObject>(PrefabName);
		ItemDrop ball = prefab ? prefab.GetComponent<ItemDrop>() : null;
		if (!ball)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + PrefabName + " is missing from the asset bundle or has no ItemDrop, the organ cannon has no ammo.");
			return null;
		}
		AddCollider(prefab);

		ItemDrop.ItemData.SharedData shared = ball.m_itemData.m_shared;
		shared.m_itemType = ItemDrop.ItemData.ItemType.AmmoNonEquipable;
		shared.m_ammoType = OrganCannon.AmmoType;
		shared.m_description = "$item_mm_cannonball_description";
		// Worth nothing to sell, or buying ten for a coin and selling them back would make money
		shared.m_value = 0;
		shared.m_attackForce = PushForce;
		shared.m_attack.m_attackProjectile = AddProjectile(prefab, vanilla);
		shared.m_attack.m_projectileVel = Speed;
		shared.m_attack.m_projectileAccuracy = SpreadDegrees;
		shared.m_attack.m_projectiles = 1;
		ApplyDamage(shared);
		MainConfig.cannonBallDamage.SettingChanged += (sender, e) => ApplyDamage(shared);

		ItemManager.Instance.AddItem(new CustomItem(prefab, fixReference: false));
		s_ball = ball;
		return ball;
	}

	private static void ApplyDamage(ItemDrop.ItemData.SharedData shared)
	{
		shared.m_damages = new HitData.DamageTypes { m_blunt = MainConfig.cannonBallDamage.Value };
	}

	// The bundle's ball has no collider, so a dropped one would fall through the ground and could not be picked up
	private static void AddCollider(GameObject prefab)
	{
		if (prefab.GetComponent<Collider>())
		{
			return;
		}
		MeshFilter mesh = prefab.GetComponentInChildren<MeshFilter>(includeInactive: true);
		SphereCollider collider = prefab.AddComponent<SphereCollider>();
		if (mesh && mesh.sharedMesh)
		{
			Bounds bounds = mesh.sharedMesh.bounds;
			collider.center = prefab.transform.InverseTransformPoint(mesh.transform.TransformPoint(bounds.center));
			collider.radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
		}
	}

	// The vanilla bolt's projectile, with the bolt's model hidden and the ball's in its place. The ball neither
	// sticks in what it hits nor stays there
	private static GameObject AddProjectile(GameObject ballPrefab, Turret vanilla)
	{
		GameObject bolt = null;
		if (vanilla)
		{
			foreach (Turret.AmmoType ammo in vanilla.m_allowedAmmo)
			{
				if (ammo.m_ammo && ammo.m_ammo.m_itemData.m_shared.m_attack.m_attackProjectile)
				{
					bolt = ammo.m_ammo.m_itemData.m_shared.m_attack.m_attackProjectile;
					break;
				}
			}
		}
		if (!bolt)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] The vanilla ballista bolt's projectile was not found, the organ cannon cannot fire.");
			return null;
		}
		GameObject projectile = PrefabManager.Instance.CreateClonedPrefab(ProjectileName, bolt);
		foreach (Renderer renderer in projectile.GetComponentsInChildren<Renderer>(includeInactive: true))
		{
			if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
			{
				renderer.enabled = false;
			}
		}
		Transform source = ballPrefab.transform.Find(VisualChild);
		GameObject visual = null;
		if (source)
		{
			visual = Object.Instantiate(source.gameObject, projectile.transform, worldPositionStays: false);
			visual.name = "cannon_ball_visual";
			// Centred on the projectile, as the ball's mesh sits on its pivot rather than round it
			MeshFilter mesh = visual.GetComponentInChildren<MeshFilter>();
			if (mesh && mesh.sharedMesh)
			{
				visual.transform.localPosition -= projectile.transform.InverseTransformPoint(mesh.transform.TransformPoint(mesh.sharedMesh.bounds.center));
			}
		}
		else
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + PrefabName + " has no '" + VisualChild + "' child, cannon balls will fly unseen.");
		}
		Projectile component = projectile.GetComponent<Projectile>();
		if (component)
		{
			component.m_gravity = Gravity;
			component.m_stayAfterHitStatic = false;
			component.m_stayAfterHitDynamic = false;
			component.m_attachToRigidBody = false;
			component.m_attachToClosestBone = false;
			component.m_respawnItemOnHit = false;
			component.m_spawnOnHit = null;
			if (visual)
			{
				component.m_hideOnHit = visual;
			}
		}
		PrefabManager.Instance.AddPrefab(new CustomPrefab(projectile, fixReference: false));
		return projectile;
	}

	// Traders are scene objects, not prefabs, so each one gets the balls as it starts
	[HarmonyPatch(typeof(Trader), "Start")]
	private static class Trader_Start
	{
		private static void Postfix(Trader __instance)
		{
			if (!s_ball || __instance.m_items.Exists(item => item.m_prefab == s_ball))
			{
				return;
			}
			__instance.m_items.Add(new Trader.TradeItem
			{
				m_prefab = s_ball,
				m_stack = TradeStack,
				m_price = TradePrice,
				m_requiredGlobalKey = "",
				m_buyPlayerEffects = new EffectList(),
				m_name = "",
				m_tooltip = "",
				m_buyKey = "",
				m_incrementKey = ""
			});
		}
	}
}

// Marks a ball fired by the organ cannon, so Spare Buildings can keep it from damaging pieces
internal sealed class CannonBallShot : MonoBehaviour
{
}
