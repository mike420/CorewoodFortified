using System;
using UnityEngine;

namespace CorewoodFortified.LadderAnimation;

internal sealed class OriginalRigBinding
{
	internal readonly Player Player;

	internal readonly Animator Animator;

	internal readonly CharacterAnimEvent Events;

	internal readonly Transform Hips;

	internal readonly Transform[] Ends = new Transform[4];

	internal readonly Transform[] Joints = new Transform[4];

	internal readonly Transform[] Roots = new Transform[4];

	internal readonly float[] Reach = new float[4];

	internal readonly Transform[] Landmark = new Transform[4];

	internal readonly float PelvisHeight;

	internal readonly float ShoulderHeight;

	private readonly int _controller;

	private readonly int _avatar;

	private readonly Vector3 _rootScale;

	internal bool BodyBound { get; private set; }

	internal Vector3 BodyFromPelvis { get; private set; }

	internal Quaternion BodyInPlayer { get; private set; }

	internal float LiveBodyTwist { get; private set; }

	internal Vector3 NativePelvisInBody { get; private set; }

	internal bool Valid
	{
		get
		{
			if (Player != null && Animator != null && Animator.isHuman && Animator.avatar != null && Animator.avatar.isValid && Animator.avatar.GetInstanceID() == _avatar && Animator.runtimeAnimatorController != null && Animator.runtimeAnimatorController.GetInstanceID() == _controller)
			{
				return (Player.transform.lossyScale - _rootScale).sqrMagnitude < 1E-06f;
			}
			return false;
		}
	}

	private OriginalRigBinding(Player player, Animator animator, CharacterAnimEvent events)
	{
		Player = player;
		Animator = animator;
		Events = events;
		_controller = animator.runtimeAnimatorController.GetInstanceID();
		_avatar = animator.avatar.GetInstanceID();
		_rootScale = player.transform.lossyScale;
		Hips = Bone(HumanBodyBones.Hips);
		HumanBodyBones[] array = new HumanBodyBones[4]
		{
			HumanBodyBones.LeftUpperArm,
			HumanBodyBones.RightUpperArm,
			HumanBodyBones.LeftUpperLeg,
			HumanBodyBones.RightUpperLeg
		};
		HumanBodyBones[] array2 = new HumanBodyBones[4]
		{
			HumanBodyBones.LeftLowerArm,
			HumanBodyBones.RightLowerArm,
			HumanBodyBones.LeftLowerLeg,
			HumanBodyBones.RightLowerLeg
		};
		HumanBodyBones[] array3 = new HumanBodyBones[4]
		{
			HumanBodyBones.LeftHand,
			HumanBodyBones.RightHand,
			HumanBodyBones.LeftFoot,
			HumanBodyBones.RightFoot
		};
		for (int i = 0; i < 4; i++)
		{
			Roots[i] = Bone(array[i]);
			Joints[i] = Bone(array2[i]);
			Ends[i] = Bone(array3[i]);
			Reach[i] = Vector3.Distance(Roots[i].position, Joints[i].position) + Vector3.Distance(Joints[i].position, Ends[i].position);
			if (Reach[i] < 0.2f || Reach[i] > 2f)
			{
				throw new InvalidOperationException("Live humanoid limb dimensions are incompatible.");
			}
		}
		Landmark[0] = animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal) ?? Ends[0];
		Landmark[1] = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal) ?? Ends[1];
		Landmark[2] = animator.GetBoneTransform(HumanBodyBones.LeftToes) ?? Ends[2];
		Landmark[3] = animator.GetBoneTransform(HumanBodyBones.RightToes) ?? Ends[3];
		PelvisHeight = Math.Min(Reach[2], Reach[3]) * 0.93f;
		ShoulderHeight = PelvisHeight + Vector3.Distance(Hips.position, (Roots[0].position + Roots[1].position) * 0.5f);
	}

	internal static OriginalRigBinding Create(Player player)
	{
		if (player == null)
		{
			throw new ArgumentException("No current player to bind.");
		}
		Animator animator = null;
		CharacterAnimEvent characterAnimEvent = null;
		Animator[] componentsInChildren = player.GetComponentsInChildren<Animator>(includeInactive: true);
		foreach (Animator animator2 in componentsInChildren)
		{
			CharacterAnimEvent component = animator2.GetComponent<CharacterAnimEvent>();
			if (!(component == null) && animator2.isHuman && !(animator2.avatar == null) && animator2.avatar.isValid)
			{
				if (animator != null)
				{
					throw new InvalidOperationException("Multiple native humanoid Animators; explicit binding is required.");
				}
				animator = animator2;
				characterAnimEvent = component;
			}
		}
		if (animator == null || characterAnimEvent == null || animator.runtimeAnimatorController == null)
		{
			throw new InvalidOperationException("No valid native player humanoid/controller was found.");
		}
		return new OriginalRigBinding(player, animator, characterAnimEvent);
	}

	internal void BindBodyInIK()
	{
		if (!BodyBound)
		{
			Quaternion rotation = Player.transform.rotation;
			BodyInPlayer = Quaternion.identity;
			LiveBodyTwist = Quaternion.Angle(rotation, Animator.bodyRotation);
			BodyFromPelvis = Quaternion.Inverse(Animator.bodyRotation) * (Animator.bodyPosition - Hips.position);
			NativePelvisInBody = Quaternion.Inverse(Animator.bodyRotation) * (Hips.position - Animator.bodyPosition);
			BodyBound = true;
		}
	}

	internal Vector3 Patch(int limb)
	{
		Transform transform = Ends[limb];
		Transform transform2 = Landmark[limb];
		if (limb < 2)
		{
			Vector3 vector = transform2.position - transform.position;
			float magnitude = vector.magnitude;
			if (!(magnitude < 0.0001f))
			{
				return transform.position + vector * (Mathf.Min(ClimbTuning.HandPalmAlong, magnitude) / magnitude);
			}
			return transform.position;
		}
		return transform2.position - Vector3.up * ClimbTuning.SoleThickness;
	}

	private Transform Bone(HumanBodyBones bone)
	{
		Transform boneTransform = Animator.GetBoneTransform(bone);
		if (boneTransform == null)
		{
			throw new InvalidOperationException("Required humanoid bone missing: " + bone.ToString() + ".");
		}
		return boneTransform;
	}
}
