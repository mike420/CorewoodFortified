using System;
using HarmonyLib;
using UnityEngine;

namespace CorewoodFortified;

// Lets the fortified ballista and organ cannon be picked up and put down somewhere else (see TurretMover). Moving one
// keeps everything it holds, as it is the same piece: its ammo, trophy targets and builder stay with it.
//
// The game that moves it takes it, and every game moves its copy at once. The new owner also saves the new place, so
// it loads there for anyone who was not near enough to see it moved
internal sealed class MovableTurret : MonoBehaviour
{
	private const string RpcMoved = "CWF_TurretMoved";

	// The heightmap's event that tells the pieces on it to work out their support again. The publicized game exposes
	// it as both the event and its field, so it is reached through the field
	private static readonly AccessTools.FieldRef<Heightmap, Action> s_clearCache = AccessTools.FieldRefAccess<Heightmap, Action>("m_clearConnectedWearNTearCache");

	private ZNetView m_nview;

	private Turret m_turret;

	private WearNTear m_wear;

	public Turret Turret => m_turret;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_turret = GetComponent<Turret>();
		m_wear = GetComponent<WearNTear>();
		if (m_nview && m_nview.GetZDO() != null)
		{
			m_nview.Register<Vector3, Quaternion>(RpcMoved, RPC_Moved);
		}
	}

	// Its kind of turret is turned on and may be moved
	public bool Allowed()
	{
		if (GetComponent<OrganCannon>())
		{
			return MainConfig.enableOrganCannon.Value && MainConfig.cannonAllowMoving.Value;
		}
		return MainConfig.enableTurrets.Value && MainConfig.turretAllowMoving.Value;
	}

	public bool CanBeMoved()
	{
		return m_nview && m_nview.IsValid() && m_turret && Allowed();
	}

	public void MoveTo(Vector3 position, Quaternion rotation)
	{
		if (!CanBeMoved())
		{
			return;
		}
		m_nview.ClaimOwnership();
		m_nview.InvokeRPC(ZNetView.Everybody, RpcMoved, position, rotation);
	}

	private void RPC_Moved(long sender, Vector3 position, Quaternion rotation)
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		transform.SetPositionAndRotation(position, rotation);
		if (m_nview.IsOwner())
		{
			m_nview.GetZDO().SetPosition(position);
			m_nview.GetZDO().SetRotation(rotation);
		}
		Physics.SyncTransforms();
		ResetSupport();
	}

	// A piece works out what holds it up from where its colliders were when it loaded, and from the ground under it
	// then. Both are worked out again for the new place
	private void ResetSupport()
	{
		if (!m_wear)
		{
			return;
		}
		m_wear.m_colliders = null;
		m_wear.ClearCachedSupport();
		Action clear = m_wear.ClearCachedSupport;
		if (m_wear.m_connectedHeightMap)
		{
			s_clearCache(m_wear.m_connectedHeightMap) = (Action)Delegate.Remove(s_clearCache(m_wear.m_connectedHeightMap), clear);
		}
		m_wear.m_connectedHeightMap = Heightmap.FindHeightmap(transform.position);
		if (m_wear.m_connectedHeightMap)
		{
			s_clearCache(m_wear.m_connectedHeightMap) = (Action)Delegate.Combine(s_clearCache(m_wear.m_connectedHeightMap), clear);
		}
	}
}
