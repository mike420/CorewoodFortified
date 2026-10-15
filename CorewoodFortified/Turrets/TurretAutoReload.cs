using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CorewoodFortified;

// Keeps the fortified ballista loaded with bolts from chests nearby.
//
// It is safe in multiplayer because each change is made only by the machine the game lets save it. The turret's
// owner asks the chest's owner for bolts. The chest's owner takes them out, but only while nobody has the chest
// open (opening a chest makes whoever opened it its owner), and sends them to the turret's owner, who loads them.
// The chest's owner also checks the request itself: that it comes from a fortified ballista in range, asks for
// bolts that ballista can fire, and that the ballista's builder may open the chest
internal sealed class TurretAutoReload : MonoBehaviour
{
	private const string RpcTakeAmmo = "CF_TurretTakeAmmo";

	private const string RpcGiveAmmo = "CF_TurretGiveAmmo";

	// Bolts passed on by a machine that was sent them but no longer owns the turret. Never passed on again
	private const string RpcPassAmmo = "CF_TurretPassAmmo";

	// Seconds between looks for bolts
	private const float CheckInterval = 5f;

	// Seconds to wait for a chest's answer before asking again
	private const float ReplyTimeout = 10f;

	// Chests a little past the range still answer, so one right at the edge is not refused over rounding
	private const float RangeTolerance = 1f;

	private static readonly List<Container> s_containers = new List<Container>();

	private static readonly HashSet<string> s_allowedAmmo = new HashSet<string>();

	private static int s_turretHash;

	private Turret m_turret;

	private ZNetView m_nview;

	private float m_nextCheck;

	private float m_waitingUntil;

	// Called once the prefab's turret is set up from the vanilla ballista, so its ammo list is known
	public static void Init(GameObject prefab)
	{
		Turret turret = prefab.GetComponent<Turret>();
		if (!turret)
		{
			Jotunn.Logger.LogWarning("[CorewoodFortified] " + prefab.name + " has no Turret component, it will not reload from chests.");
			return;
		}
		s_turretHash = prefab.name.GetStableHashCode();
		s_allowedAmmo.Clear();
		foreach (Turret.AmmoType ammo in turret.m_allowedAmmo)
		{
			if (ammo.m_ammo)
			{
				s_allowedAmmo.Add(ammo.m_ammo.name);
			}
		}
		prefab.AddComponent<TurretAutoReload>();
	}

	private void Awake()
	{
		m_turret = GetComponent<Turret>();
		m_nview = GetComponent<ZNetView>();
		if (!m_turret || !m_nview || m_nview.GetZDO() == null)
		{
			enabled = false;
			return;
		}
		m_nview.Register<string, int>(RpcGiveAmmo, RPC_GiveAmmo);
		m_nview.Register<string, int>(RpcPassAmmo, RPC_PassAmmo);
		// Turrets loaded together look at different times rather than all at once
		m_nextCheck = Time.time + Random.Range(1f, CheckInterval);
	}

	private void Update()
	{
		if (Time.time < m_nextCheck)
		{
			return;
		}
		m_nextCheck = Time.time + CheckInterval;
		if (!MainConfig.enableTurrets.Value || !MainConfig.turretAutoReload.Value || !m_nview.IsValid() || !m_nview.IsOwner() || Time.time < m_waitingUntil)
		{
			return;
		}
		int space = m_turret.m_maxAmmo - m_turret.GetAmmo();
		if (space > 0)
		{
			RequestAmmo(space);
		}
	}

	// Asks the nearest chest holding bolts this turret can load for as many as fit
	private void RequestAmmo(int space)
	{
		string loaded = m_turret.GetAmmo() > 0 ? m_turret.GetAmmoType() : null;
		long creator = m_nview.GetZDO().GetLong(ZDOVars.s_creator, 0L);
		float range = MainConfig.turretAutoReloadRange.Value;
		Vector3 position = transform.position;
		Container best = null;
		string bestAmmo = null;
		float bestDistance = float.MaxValue;
		s_containers.RemoveAll(container => !container);
		foreach (Container container in s_containers)
		{
			float distance = Vector3.Distance(position, container.transform.position);
			if (distance > range || distance >= bestDistance || !CanTakeFrom(container, creator))
			{
				continue;
			}
			string ammo = FindAmmo(container.GetInventory(), loaded);
			if (ammo != null)
			{
				best = container;
				bestAmmo = ammo;
				bestDistance = distance;
			}
		}
		if (!best)
		{
			return;
		}
		best.m_nview.InvokeRPC(RpcTakeAmmo, m_nview.GetZDO().m_uid, bestAmmo, space);
		m_waitingUntil = Time.time + ReplyTimeout;
	}

	// The bolts this turret can take from the inventory: the kind already loaded, or if it is empty, the first kind
	// it can fire that the inventory holds. A turret holds one kind at a time
	private string FindAmmo(Inventory inventory, string loaded)
	{
		foreach (Turret.AmmoType ammo in m_turret.m_allowedAmmo)
		{
			if (!ammo.m_ammo || (loaded != null && ammo.m_ammo.name != loaded))
			{
				continue;
			}
			if (inventory.CountItems(ammo.m_ammo.m_itemData.m_shared.m_name) > 0)
			{
				return ammo.m_ammo.name;
			}
		}
		return null;
	}

	// Player-built chests the turret's builder may open. Graves, carts and ships are left alone
	private static bool CanTakeFrom(Container container, long creator)
	{
		return container.m_nview && container.m_nview.IsValid() && !container.m_wagon && container.GetComponent<Piece>() && !container.GetComponent<TombStone>() && !container.GetComponentInParent<Ship>() && container.CheckAccess(creator);
	}

	// Run by the chest's owner. Takes out the bolts and sends them to the turret's owner, with 0 if none were taken
	// so the turret can ask again. The answer always goes to one machine, so bolts can never arrive twice
	private static void RPC_TakeAmmo(Container container, long sender, ZDOID turretId, string ammo, int amount)
	{
		ZDO turret = ZDOMan.instance.GetZDO(turretId);
		// A turret with no owner is changing hands. It is left to ask again rather than answered with a message to
		// everyone
		long owner = turret != null ? turret.GetOwner() : 0L;
		if (owner == 0L)
		{
			return;
		}
		int taken = 0;
		GameObject prefab = ZNetScene.instance.GetPrefab(ammo);
		ItemDrop item = prefab ? prefab.GetComponent<ItemDrop>() : null;
		bool valid = container.m_nview.IsOwner() && !container.IsInUse() && amount > 0 && item && s_allowedAmmo.Contains(ammo) && turret.GetPrefab() == s_turretHash && Vector3.Distance(turret.GetPosition(), container.transform.position) <= MainConfig.turretAutoReloadRange.Value + RangeTolerance && CanTakeFrom(container, turret.GetLong(ZDOVars.s_creator, 0L));
		if (valid)
		{
			string name = item.m_itemData.m_shared.m_name;
			taken = Mathf.Min(amount, container.GetInventory().CountItems(name));
			if (taken > 0)
			{
				container.GetInventory().RemoveItem(name, taken);
			}
		}
		ZRoutedRpc.instance.InvokeRoutedRPC(owner, turretId, RpcGiveAmmo, ammo, taken);
	}

	// Run by the machine the chest took the turret's owner to be. Loads the bolts the chest sent
	private void RPC_GiveAmmo(long sender, string ammo, int count)
	{
		if (m_nview.IsOwner())
		{
			LoadAmmo(ammo, count);
			return;
		}
		if (count <= 0)
		{
			return;
		}
		// The turret changed owner while the bolts were on their way. They are passed on once to the new owner, or
		// dropped here if it has none just now
		long owner = m_nview.GetZDO().GetOwner();
		if (owner != 0L)
		{
			ZRoutedRpc.instance.InvokeRoutedRPC(owner, m_nview.GetZDO().m_uid, RpcPassAmmo, ammo, count);
		}
		else
		{
			DropBolts(ammo, count);
		}
	}

	private void RPC_PassAmmo(long sender, string ammo, int count)
	{
		if (m_nview.IsOwner())
		{
			LoadAmmo(ammo, count);
		}
		else if (count > 0)
		{
			DropBolts(ammo, count);
		}
	}

	private void LoadAmmo(string ammo, int count)
	{
		m_waitingUntil = 0f;
		if (count <= 0)
		{
			return;
		}
		int loaded = m_turret.GetAmmo();
		int added = 0;
		if (m_turret.IsItemAllowed(ammo) && (loaded == 0 || m_turret.GetAmmoType() == ammo))
		{
			added = Mathf.Clamp(m_turret.m_maxAmmo - loaded, 0, count);
		}
		if (added > 0)
		{
			ZDO zdo = m_nview.GetZDO();
			zdo.Set(ZDOVars.s_ammo, loaded + added, false);
			zdo.Set(ZDOVars.s_ammoType, ammo);
			Transform body = m_turret.m_turretBody.transform;
			m_turret.m_addAmmoEffect.Create(body.position, body.rotation);
			m_turret.UpdateVisualBolt();
		}
		// Bolts that no longer fit, because a player loaded the turret while they were on their way, are dropped by
		// it rather than lost
		if (count > added)
		{
			DropBolts(ammo, count - added);
		}
	}

	private void DropBolts(string ammo, int count)
	{
		GameObject prefab = ZNetScene.instance.GetPrefab(ammo);
		ItemDrop item = prefab ? prefab.GetComponent<ItemDrop>() : null;
		if (!item)
		{
			return;
		}
		int maxStack = Mathf.Max(1, item.m_itemData.m_shared.m_maxStackSize);
		while (count > 0)
		{
			int stack = Mathf.Min(count, maxStack);
			ItemDrop drop = Instantiate(prefab, transform.position + Vector3.up, Quaternion.identity).GetComponent<ItemDrop>();
			drop.SetStack(stack);
			count -= stack;
		}
	}

	// Every chest gets the request handler, as any chest may be asked for bolts
	[HarmonyPatch(typeof(Container), "Awake")]
	private static class Container_Awake
	{
		private static void Postfix(Container __instance)
		{
			if (!__instance.m_nview || __instance.m_nview.GetZDO() == null)
			{
				return;
			}
			Container container = __instance;
			s_containers.Add(container);
			container.m_nview.Register<ZDOID, string, int>(RpcTakeAmmo, (sender, turretId, ammo, amount) => RPC_TakeAmmo(container, sender, turretId, ammo, amount));
		}
	}
}
