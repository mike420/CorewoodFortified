using UnityEngine;

namespace CorewoodFortified.LadderAnimation;

internal sealed class NativeIKLease
{
	private struct Goal
	{
		internal bool Owned;

		internal Vector3 BeforePosition;

		internal Vector3 LastPosition;

		internal Quaternion BeforeRotation;

		internal Quaternion LastRotation;

		internal float BeforeWeight;

		internal float BeforeRotationWeight;

		internal float LastWeight;

		internal float LastRotationWeight;
	}

	private struct Hint
	{
		internal bool Owned;

		internal Vector3 BeforePosition;

		internal Vector3 LastPosition;

		internal float BeforeWeight;

		internal float LastWeight;
	}

	internal static readonly AvatarIKGoal[] Goals = new AvatarIKGoal[4]
	{
		AvatarIKGoal.LeftHand,
		AvatarIKGoal.RightHand,
		AvatarIKGoal.LeftFoot,
		AvatarIKGoal.RightFoot
	};

	internal static readonly AvatarIKHint[] Hints = new AvatarIKHint[4]
	{
		AvatarIKHint.LeftElbow,
		AvatarIKHint.RightElbow,
		AvatarIKHint.LeftKnee,
		AvatarIKHint.RightKnee
	};

	private readonly Goal[] _goals = new Goal[4];

	private readonly Hint[] _hints = new Hint[4];

	private bool _bodyOwned;

	private Vector3 _bodyBefore;

	private Vector3 _bodyLast;

	private Quaternion _rotationBefore;

	private Quaternion _rotationLast;

	internal bool Pending { get; private set; }

	internal void RestoreInIK(Animator animator)
	{
		for (int i = 0; i < 4; i++)
		{
			Goal goal = _goals[i];
			if (goal.Owned)
			{
				AvatarIKGoal goal2 = Goals[i];
				if (Same(animator.GetIKPosition(goal2), goal.LastPosition))
				{
					animator.SetIKPosition(goal2, goal.BeforePosition);
				}
				if (Same(animator.GetIKRotation(goal2), goal.LastRotation))
				{
					animator.SetIKRotation(goal2, goal.BeforeRotation);
				}
				if (Mathf.Abs(animator.GetIKPositionWeight(goal2) - goal.LastWeight) < 1E-05f)
				{
					animator.SetIKPositionWeight(goal2, goal.BeforeWeight);
				}
				if (Mathf.Abs(animator.GetIKRotationWeight(goal2) - goal.LastRotationWeight) < 1E-05f)
				{
					animator.SetIKRotationWeight(goal2, goal.BeforeRotationWeight);
				}
			}
			_goals[i] = default(Goal);
			Hint hint = _hints[i];
			if (hint.Owned)
			{
				AvatarIKHint hint2 = Hints[i];
				if (Same(animator.GetIKHintPosition(hint2), hint.LastPosition))
				{
					animator.SetIKHintPosition(hint2, hint.BeforePosition);
				}
				if (Mathf.Abs(animator.GetIKHintPositionWeight(hint2) - hint.LastWeight) < 1E-05f)
				{
					animator.SetIKHintPositionWeight(hint2, hint.BeforeWeight);
				}
			}
			_hints[i] = default(Hint);
		}
		if (_bodyOwned)
		{
			if (Same(animator.bodyPosition, _bodyLast))
			{
				animator.bodyPosition = _bodyBefore;
			}
			if (Same(animator.bodyRotation, _rotationLast))
			{
				animator.bodyRotation = _rotationBefore;
			}
		}
		_bodyOwned = false;
		Pending = false;
	}

	internal void ReassertInIK(Animator animator)
	{
		for (int i = 0; i < 4; i++)
		{
			Goal goal = _goals[i];
			if (goal.Owned)
			{
				AvatarIKGoal goal2 = Goals[i];
				animator.SetIKPosition(goal2, goal.LastPosition);
				animator.SetIKRotation(goal2, goal.LastRotation);
				animator.SetIKPositionWeight(goal2, goal.LastWeight);
				animator.SetIKRotationWeight(goal2, goal.LastRotationWeight);
			}
			Hint hint = _hints[i];
			if (hint.Owned)
			{
				animator.SetIKHintPosition(Hints[i], hint.LastPosition);
				animator.SetIKHintPositionWeight(Hints[i], hint.LastWeight);
			}
		}
	}

	internal void GoalInIK(Animator animator, int limb, Vector3 position, Quaternion rotation, float weight)
	{
		if (!(weight <= 0f))
		{
			AvatarIKGoal goal = Goals[limb];
			_goals[limb] = new Goal
			{
				Owned = true,
				BeforePosition = animator.GetIKPosition(goal),
				BeforeRotation = animator.GetIKRotation(goal),
				BeforeWeight = animator.GetIKPositionWeight(goal),
				BeforeRotationWeight = animator.GetIKRotationWeight(goal),
				LastPosition = position,
				LastRotation = rotation,
				LastWeight = weight,
				LastRotationWeight = weight
			};
			animator.SetIKPosition(goal, position);
			animator.SetIKRotation(goal, rotation);
			animator.SetIKPositionWeight(goal, weight);
			animator.SetIKRotationWeight(goal, weight);
			Pending = true;
		}
	}

	internal void HintInIK(Animator animator, int limb, Vector3 position, float weight)
	{
		if (!(weight <= 0f))
		{
			AvatarIKHint hint = Hints[limb];
			_hints[limb] = new Hint
			{
				Owned = true,
				BeforePosition = animator.GetIKHintPosition(hint),
				BeforeWeight = animator.GetIKHintPositionWeight(hint),
				LastPosition = position,
				LastWeight = weight
			};
			animator.SetIKHintPosition(hint, position);
			animator.SetIKHintPositionWeight(hint, weight);
			Pending = true;
		}
	}

	internal void BodyInIK(Animator animator, Vector3 position, Quaternion rotation, float weight)
	{
		if (!(weight <= 0f))
		{
			_bodyBefore = animator.bodyPosition;
			_rotationBefore = animator.bodyRotation;
			_bodyLast = Vector3.Lerp(_bodyBefore, position, weight);
			_rotationLast = Quaternion.Slerp(_rotationBefore, rotation, weight);
			animator.bodyPosition = _bodyLast;
			animator.bodyRotation = _rotationLast;
			_bodyOwned = true;
			Pending = true;
		}
	}

	private static bool Same(Vector3 a, Vector3 b)
	{
		return (a - b).sqrMagnitude < 1E-08f;
	}

	private static bool Same(Quaternion a, Quaternion b)
	{
		return Mathf.Abs(Quaternion.Dot(a, b)) > 0.999999f;
	}
}
