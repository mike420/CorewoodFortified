using System;
using UnityEngine;

namespace CorewoodFortified;

// Catches a rope when the local player jumps or falls into it, away from the posts
public class RopeGrab : MonoBehaviour
{
	private const float Reach = 0.6f;

	private const float PostClearance = 2.5f;

	private void Update()
	{
		try
		{
			if (!MainConfig.enableZiplines.Value || !MainConfig.ziplineGrabOnTouch.Value ||ZiplineRider.Current != null || RopeCarry.Active || Time.time < ZiplineRider.NoGrabUntil || ZiplineRope.All.Count == 0)
			{
				return;
			}
			Player player = Player.m_localPlayer;
			if (player == null || player.IsDead() || player.IsOnGround() || player.IsSwimming() || player.IsAttached() || player.InPlaceMode() || player.GetDoodadController() != null || player.IsTeleporting())
			{
				return;
			}
			Vector3 feet = player.transform.position;
			float hang = MainConfig.ziplineHangHeight.Value;
			Vector3 hands = feet + Vector3.up * (hang * 0.6f);
			foreach (ZiplineRope rope in ZiplineRope.All)
			{
				RopePath path = rope != null ? rope.Path : null;
				if (path == null)
				{
					continue;
				}
				float t = path.ClosestT(hands);
				Vector3 point = path.PointT(t);
				float above = point.y - feet.y;
				if (above < Reach || above > hang + 0.4f)
				{
					continue;
				}
				Vector3 across = point - feet;
				across.y = 0f;
				if (across.sqrMagnitude > Reach * Reach)
				{
					continue;
				}
				float fromA = path.DistanceAtT(t);
				if (fromA < PostClearance || fromA > path.Length - PostClearance)
				{
					continue;
				}
				if (!ZiplineRider.TryGrab(player, rope, fromA, player.GetVelocity()))
				{
					ZiplineRider.NoGrabUntil = Time.time + 1f;
				}
				break;
			}
		}
		catch (Exception ex)
		{
			ZiplineMessages.LogDebug("Zipline rope grab check failed: " + ex.Message);
			ZiplineRider.NoGrabUntil = Time.time + 2f;
		}
	}
}
