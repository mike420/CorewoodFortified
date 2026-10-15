using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorewoodFortified;

// Draws a trolley over every player riding a zipline, and raises their arms to its bar. Riders mark themselves on
// their own ZDO, so other players see them riding too
[DefaultExecutionOrder(10000)]
public class RideVisuals : MonoBehaviour
{
	private sealed class Rider
	{
		public Player Player;

		public ZNetView NView;

		public GameObject Trolley;

		public Transform Strap;

		public Transform Bar;

		// How far below the wheels the strap starts, from the trolley prefab
		public float StrapDrop = DefaultStrapDrop;

		public bool BonesSearched;

		public Transform[] Left;

		public Transform[] Right;

		public float Reach;
	}

	internal const string KeyRiding = "mm_cf_zl_ride";

	internal const string KeyAxis = "mm_cf_zl_axis";

	internal const string KeyHang = "mm_cf_zl_hang";

	internal const string KeyHands = "mm_cf_zl_hands";

	private const float DefaultStrapDrop = 0.1f;

	private const float GripHalfWidth = 0.21f;

	private static readonly Dictionary<Player, Rider> s_riders = new Dictionary<Player, Rider>();

	private static readonly List<Player> s_gone = new List<Player>();

	private static bool s_boneWarning;

	private static bool s_failureLogged;

	internal static void MarkRiding(Player player, bool riding, Vector3 axis, float hang, bool hands)
	{
		try
		{
			ZNetView nview = player != null ? player.GetComponent<ZNetView>() : null;
			if (nview == null || !nview.IsValid() || !nview.IsOwner())
			{
				return;
			}
			ZDO zdo = nview.GetZDO();
			zdo.Set(KeyRiding, riding);
			if (riding)
			{
				zdo.Set(KeyAxis, axis);
				zdo.Set(KeyHang, hang);
				zdo.Set(KeyHands, hands);
			}
		}
		catch (Exception ex)
		{
			ZiplineMessages.LogWarning("Marking the zipline rider failed: " + ex.Message);
		}
	}

	private void LateUpdate()
	{
		try
		{
			foreach (Player player in Player.GetAllPlayers())
			{
				if (player == null)
				{
					continue;
				}
				bool riding = IsRiding(player);
				bool known = s_riders.TryGetValue(player, out Rider rider);
				if (riding && !known)
				{
					rider = Create(player);
					s_riders[player] = rider;
				}
				else if (!riding && known)
				{
					Remove(player, rider);
					continue;
				}
				if (riding && rider != null)
				{
					Pose(rider);
				}
			}
			s_gone.Clear();
			foreach (KeyValuePair<Player, Rider> entry in s_riders)
			{
				if (entry.Key == null || !IsRiding(entry.Key))
				{
					s_gone.Add(entry.Key);
				}
			}
			foreach (Player player in s_gone)
			{
				Remove(player, s_riders[player]);
			}
		}
		catch (Exception ex)
		{
			if (!s_failureLogged)
			{
				s_failureLogged = true;
				ZiplineMessages.LogWarning("Drawing zipline riders failed (reported once): " + ex);
			}
		}
	}

	private static bool IsRiding(Player player)
	{
		if (player == null)
		{
			return false;
		}
		ZNetView nview = player.GetComponent<ZNetView>();
		return nview != null && nview.IsValid() && nview.GetZDO().GetBool(KeyRiding) && !player.IsDead();
	}

	private static Rider Create(Player player)
	{
		Rider rider = new Rider
		{
			Player = player,
			NView = player.GetComponent<ZNetView>()
		};
		rider.Trolley = ZiplineVisuals.MakeTrolley(out rider.Strap, out rider.Bar);
		if (rider.Strap != null)
		{
			rider.StrapDrop = -rider.Strap.localPosition.y;
		}
		return rider;
	}

	private static void Remove(Player player, Rider rider)
	{
		if (rider != null && rider.Trolley != null)
		{
			Destroy(rider.Trolley);
		}
		s_riders.Remove(player);
	}

	private static void Pose(Rider r)
	{
		ZDO zdo = r.NView != null && r.NView.IsValid() ? r.NView.GetZDO() : null;
		if (zdo == null || r.Trolley == null)
		{
			return;
		}
		Transform body = r.Player.transform;
		Vector3 feet = body.position;
		float hang = zdo.GetFloat(KeyHang, ZiplineRider.ClearanceHang);
		Vector3 axis = zdo.GetVec3(KeyAxis, body.forward);
		axis.y = 0f;
		if (axis.sqrMagnitude < 0.0001f)
		{
			axis = body.forward;
		}
		axis.Normalize();
		if (!r.BonesSearched)
		{
			FindBones(r, feet);
		}
		Vector3 wheels = feet + Vector3.up * hang;
		r.Trolley.transform.SetPositionAndRotation(wheels, Quaternion.LookRotation(axis, Vector3.up));
		// The bar hangs where the rider's hands reach, the strap stretching down to it
		float reach = r.Reach > 0.5f ? r.Reach : hang - 0.3f;
		reach = Mathf.Clamp(reach, hang - 1.2f, hang - r.StrapDrop - 0.08f);
		Vector3 grip = feet + Vector3.up * reach;
		if (r.Bar != null)
		{
			r.Bar.SetPositionAndRotation(grip, body.rotation);
		}
		if (r.Strap != null)
		{
			// The strap stays where the prefab puts it under the wheels and stretches down to the bar
			r.Strap.localScale = new Vector3(1f, Mathf.Max(0.05f, wheels.y - r.StrapDrop - grip.y), 1f);
		}
		if (zdo.GetBool(KeyHands, true) && r.Left != null && r.Right != null)
		{
			Vector3 right = body.right;
			Vector3 back = -body.forward;
			SolveArm(r.Left, grip - right * GripHalfWidth, -right + back * 0.35f + Vector3.down * 0.2f);
			SolveArm(r.Right, grip + right * GripHalfWidth, right + back * 0.35f + Vector3.down * 0.2f);
		}
	}

	private static void FindBones(Rider r, Vector3 feet)
	{
		r.BonesSearched = true;
		try
		{
			Animator animator = r.Player.GetComponentInChildren<Animator>();
			if (animator != null && animator.isHuman)
			{
				r.Left = Bones(animator.GetBoneTransform(HumanBodyBones.LeftUpperArm), animator.GetBoneTransform(HumanBodyBones.LeftLowerArm), animator.GetBoneTransform(HumanBodyBones.LeftHand));
				r.Right = Bones(animator.GetBoneTransform(HumanBodyBones.RightUpperArm), animator.GetBoneTransform(HumanBodyBones.RightLowerArm), animator.GetBoneTransform(HumanBodyBones.RightHand));
			}
			if (r.Left == null || r.Right == null)
			{
				Transform root = animator != null ? animator.transform : r.Player.transform;
				r.Left = Bones(Find(root, "LeftArm"), Find(root, "LeftForeArm"), Find(root, "LeftHand"));
				r.Right = Bones(Find(root, "RightArm"), Find(root, "RightForeArm"), Find(root, "RightHand"));
			}
			if (r.Left == null || r.Right == null)
			{
				if (!s_boneWarning)
				{
					s_boneWarning = true;
					ZiplineMessages.LogWarning("Could not find the arm bones; zipline riders hang without raised arms.");
				}
				return;
			}
			float shoulders = (r.Left[0].position.y + r.Right[0].position.y) * 0.5f - feet.y;
			r.Reach = shoulders + Length(r.Left) * 0.94f;
		}
		catch (Exception ex)
		{
			r.Left = r.Right = null;
			ZiplineMessages.LogWarning("Looking up the arm bones failed: " + ex.Message);
		}
	}

	private static Transform[] Bones(Transform upper, Transform lower, Transform hand)
	{
		if (upper == null || lower == null || hand == null || !lower.IsChildOf(upper) || !hand.IsChildOf(lower))
		{
			return null;
		}
		return new[] { upper, lower, hand };
	}

	private static float Length(Transform[] arm)
	{
		return (arm[1].position - arm[0].position).magnitude + (arm[2].position - arm[1].position).magnitude;
	}

	private static Transform Find(Transform root, string name)
	{
		foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
		{
			string childName = child.name;
			int colon = childName.LastIndexOf(':');
			if (colon >= 0)
			{
				childName = childName.Substring(colon + 1);
			}
			if (string.Equals(childName, name, StringComparison.OrdinalIgnoreCase))
			{
				return child;
			}
		}
		return null;
	}

	// Two-bone IK: bends the elbow so the hand reaches target, with the elbow turned towards pole
	private static void SolveArm(Transform[] arm, Vector3 target, Vector3 pole)
	{
		Transform upper = arm[0];
		Transform lower = arm[1];
		Transform hand = arm[2];
		Vector3 a = upper.position;
		Vector3 b = lower.position;
		Vector3 c = hand.position;
		float upperLength = (b - a).magnitude;
		float lowerLength = (c - b).magnitude;
		if (upperLength < 0.0001f || lowerLength < 0.0001f)
		{
			return;
		}
		float reach = Mathf.Clamp((target - a).magnitude, Mathf.Abs(upperLength - lowerLength) + 0.001f, upperLength + lowerLength - 0.001f);
		float shoulderNow = Angle(c - a, b - a);
		float elbowNow = Angle(a - b, c - b);
		float shoulderWanted = Mathf.Acos(Mathf.Clamp((lowerLength * lowerLength - upperLength * upperLength - reach * reach) / (-2f * upperLength * reach), -1f, 1f));
		float elbowWanted = Mathf.Acos(Mathf.Clamp((reach * reach - upperLength * upperLength - lowerLength * lowerLength) / (-2f * upperLength * lowerLength), -1f, 1f));
		Vector3 bendAxis = Vector3.Cross(c - a, b - a);
		if (bendAxis.sqrMagnitude < 1E-08f)
		{
			bendAxis = Vector3.Cross(c - a, pole);
		}
		if (bendAxis.sqrMagnitude < 1E-08f)
		{
			return;
		}
		bendAxis.Normalize();
		lower.rotation = Quaternion.AngleAxis((elbowWanted - elbowNow) * Mathf.Rad2Deg, bendAxis) * lower.rotation;
		upper.rotation = Quaternion.AngleAxis((shoulderWanted - shoulderNow) * Mathf.Rad2Deg, bendAxis) * upper.rotation;
		upper.rotation = Quaternion.FromToRotation(hand.position - a, target - a) * upper.rotation;
		Vector3 toTarget = target - a;
		if (toTarget.sqrMagnitude < 1E-08f)
		{
			return;
		}
		toTarget.Normalize();
		Vector3 elbow = Vector3.ProjectOnPlane(lower.position - a, toTarget);
		Vector3 wanted = Vector3.ProjectOnPlane(pole, toTarget);
		if (elbow.sqrMagnitude >= 1E-08f && wanted.sqrMagnitude >= 1E-08f)
		{
			upper.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(elbow, wanted, toTarget), toTarget) * upper.rotation;
		}
	}

	private static float Angle(Vector3 u, Vector3 v)
	{
		float lengths = u.magnitude * v.magnitude;
		return lengths < 1E-08f ? 0f : Mathf.Acos(Mathf.Clamp(Vector3.Dot(u, v) / lengths, -1f, 1f));
	}

	private void OnDestroy()
	{
		foreach (Rider rider in s_riders.Values)
		{
			if (rider != null && rider.Trolley != null)
			{
				Destroy(rider.Trolley);
			}
		}
		s_riders.Clear();
	}
}
