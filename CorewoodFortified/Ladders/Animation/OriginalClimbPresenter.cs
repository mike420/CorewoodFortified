using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace CorewoodFortified.LadderAnimation;

internal sealed class OriginalClimbPresenter : IClimbPresenter, IDisposable
{
	private sealed class Actor
	{
		internal readonly OriginalRigBinding Rig;

		internal readonly LimbGait Gait = new LimbGait();

		internal float BodyBlend;

		internal bool Caught;

		internal TransitionAction? Transition;

		internal float ChestTwist;

		internal readonly float[] ShownWeight = new float[4] { 1f, 1f, 1f, 1f };

		internal float ShownProgress;

		internal float ProgressRate;

		internal float ProgressAt;

		internal float TransitionProgress;

		internal bool EntryTimed;

		internal readonly FitCounters Fit = new FitCounters();

		internal readonly bool[] Asked = new bool[4];

		internal readonly ContactRecord[] Record = new ContactRecord[4];

		internal readonly Vector3[] AnchorTarget = new Vector3[4];

		internal readonly Vector3[] AnchorPatch = new Vector3[4];

		internal readonly bool[] Anchored = new bool[4];

		internal readonly Vector3[] LastPivot = new Vector3[4];

		internal int TuningVersion = -1;

		internal readonly NativeIKLease Lease = new NativeIKLease();

		internal ClimbPresentationFrame Frame;

		internal bool Active;

		internal float Seen;

		internal float Age;

		internal float SlideBlend;

		internal Actor(OriginalRigBinding rig)
		{
			Rig = rig;
		}

		internal float ProgressNow(float now, float renderDelta)
		{
			float num = Mathf.Clamp01(TransitionProgress + ProgressRate * Mathf.Clamp(now - ProgressAt, 0f, 0.05f));
			ShownProgress += (num - ShownProgress) * (1f - Mathf.Exp((0f - Mathf.Max(0f, renderDelta)) / ClimbTuning.ProgressSmoothing));
			return ShownProgress;
		}
	}

	private enum NativeKind
	{
		None,
		Attack,
		Minor
	}

	internal sealed class FitCounters
	{
		private readonly int[] _count = new int[4];

		private readonly int[] _skips = new int[4];

		private readonly float[] _worst = new float[4];

		private readonly float[] _sum = new float[4];

		private readonly float[] _rootUp = new float[4];

		private readonly float[] _rootOut = new float[4];

		private readonly float[] _error = new float[4];

		private readonly float[] _errorSum = new float[4];

		private readonly int[] _errorCount = new int[4];

		private readonly int[] _planted = new int[4];

		private readonly int[] _plantedSkips = new int[4];

		private readonly int[] _plantedFades = new int[4];

		private readonly float[] _patchWorst = new float[4];

		private readonly float[] _patchSum = new float[4];

		private readonly float[] _gapMin = new float[4];

		private readonly float[] _gapMax = new float[4];

		private readonly float[] _drift = new float[4];

		private PresentedMotion _motion;

		private float _since;

		private int _lines;

		private int Total => _count[0] + _count[1] + _count[2] + _count[3];

		internal void Reset()
		{
			Clear();
			_lines = 0;
		}

		internal void Begin(OriginalClimbPresenter owner, PresentedMotion motion, float now)
		{
			if (motion != _motion || !(_since > 0f))
			{
				if (Total > 0)
				{
					Report(owner, now, final: true);
				}
				_motion = motion;
				_since = now;
			}
		}

		internal void Sample(int limb, float ratio, Vector3 root, Vector3 feet, in ClimbPresentationFrame frame, Vector3 outward)
		{
			if (!float.IsNaN(ratio) && !float.IsInfinity(ratio))
			{
				_count[limb]++;
				_sum[limb] += ratio;
				_worst[limb] = Math.Max(_worst[limb], ratio);
				_rootUp[limb] = root.y - feet.y;
				Vector3 vector = Vector(OriginalRungSurface.At(frame.Run, frame.Face, frame.RungPlane, root.y, 0f, 0f));
				_rootOut[limb] = Vector3.Dot(root - vector, outward);
			}
		}

		internal void Skip(int limb, bool planted)
		{
			_skips[limb]++;
			if (planted)
			{
				_plantedSkips[limb]++;
			}
		}

		internal void Planted(int limb, float patchError, float gap, float drift, bool faded)
		{
			if (!float.IsNaN(patchError) && !float.IsNaN(gap) && !float.IsNaN(drift))
			{
				if (_planted[limb] == 0)
				{
					_gapMin[limb] = gap;
					_gapMax[limb] = gap;
				}
				_planted[limb]++;
				_patchSum[limb] += patchError;
				_patchWorst[limb] = Math.Max(_patchWorst[limb], patchError);
				_gapMin[limb] = Math.Min(_gapMin[limb], gap);
				_gapMax[limb] = Math.Max(_gapMax[limb], gap);
				_drift[limb] = Math.Max(_drift[limb], drift);
				if (faded)
				{
					_plantedFades[limb]++;
				}
			}
		}

		internal void Solved(int limb, float error)
		{
			if (!float.IsNaN(error) && !float.IsInfinity(error))
			{
				_errorCount[limb]++;
				_errorSum[limb] += error;
				_error[limb] = Math.Max(_error[limb], error);
			}
		}

		internal void Report(OriginalClimbPresenter owner, float now, bool final)
		{
			if (Total == 0 || (!final && now - _since < 3f))
			{
				return;
			}
			if (_lines < 40 && ClimbTuning.LogFit)
			{
				_lines++;
				StringBuilder stringBuilder = new StringBuilder("Climb fit ").Append(_motion).Append(' ').Append((now - _since).ToString("0.0", CultureInfo.InvariantCulture))
					.Append('s');
				string[] array = new string[4] { "LH", "RH", "LF", "RF" };
				for (int i = 0; i < 4; i++)
				{
					float num = ((_count[i] == 0) ? 0f : (_sum[i] / (float)_count[i]));
					stringBuilder.Append(" | ").Append(array[i]).Append(" n=")
						.Append(_count[i])
						.Append(" skip=")
						.Append(_skips[i])
						.Append(" worst=")
						.Append(_worst[i].ToString("0.00", CultureInfo.InvariantCulture))
						.Append(" mean=")
						.Append(num.ToString("0.00", CultureInfo.InvariantCulture))
						.Append(" up=")
						.Append(_rootUp[i].ToString("0.00", CultureInfo.InvariantCulture))
						.Append(" out=")
						.Append(_rootOut[i].ToString("0.00", CultureInfo.InvariantCulture))
						.Append(" err=")
						.Append(_error[i].ToString("0.000", CultureInfo.InvariantCulture))
						.Append('/')
						.Append(((_errorCount[i] == 0) ? 0f : (_errorSum[i] / (float)_errorCount[i])).ToString("0.000", CultureInfo.InvariantCulture))
						.Append(" planted=")
						.Append(_planted[i])
						.Append(" patch=")
						.Append(_patchWorst[i].ToString("0.000", CultureInfo.InvariantCulture))
						.Append('/')
						.Append(((_planted[i] == 0) ? 0f : (_patchSum[i] / (float)_planted[i])).ToString("0.000", CultureInfo.InvariantCulture))
						.Append(" gap=")
						.Append(_gapMin[i].ToString("0.000", CultureInfo.InvariantCulture))
						.Append("..")
						.Append(_gapMax[i].ToString("0.000", CultureInfo.InvariantCulture))
						.Append(" drift=")
						.Append(_drift[i].ToString("0.000", CultureInfo.InvariantCulture))
						.Append(" pfade=")
						.Append(_plantedFades[i])
						.Append(" pskip=")
						.Append(_plantedSkips[i]);
				}
				owner.Log(stringBuilder.ToString());
			}
			Clear();
			_since = now;
		}

		private void Clear()
		{
			Array.Clear(_count, 0, 4);
			Array.Clear(_skips, 0, 4);
			Array.Clear(_worst, 0, 4);
			Array.Clear(_sum, 0, 4);
			Array.Clear(_error, 0, 4);
			Array.Clear(_errorSum, 0, 4);
			Array.Clear(_errorCount, 0, 4);
			Array.Clear(_planted, 0, 4);
			Array.Clear(_plantedSkips, 0, 4);
			Array.Clear(_plantedFades, 0, 4);
			Array.Clear(_patchWorst, 0, 4);
			Array.Clear(_patchSum, 0, 4);
			Array.Clear(_gapMin, 0, 4);
			Array.Clear(_gapMax, 0, 4);
			Array.Clear(_drift, 0, 4);
		}
	}

	internal struct ContactRecord
	{
		internal bool Has;

		internal bool Planted;

		internal bool Skipped;

		internal bool Native;

		internal bool Hand;

		internal Vector3 Target;

		internal Vector3 RungCentre;

		internal Vector3 Outward;

		internal Vector3 Root;

		internal RungProfile Profile;

		internal float Requested;

		internal float Effective;

		internal float Ratio;

		internal float Chain;

		internal float Progress;

		internal bool Descending;
	}

	private static OriginalClimbPresenter? _current;

	private readonly Action<string> _log;

	private readonly Dictionary<CharacterAnimEvent, Actor> _actors = new Dictionary<CharacterAnimEvent, Actor>();

	private readonly List<CharacterAnimEvent> _remove = new List<CharacterAnimEvent>();

	private Harmony? _harmony;

	private bool _ready;

	private bool _disposing;

	private string? _failure;

	private Player? _closedPlayer;

	private int _closedSession;

	internal OriginalClimbPresenter(Action<string> log)
	{
		_log = log;
	}

	public bool Install(string harmonyId)
	{
		if (_harmony != null || _disposing)
		{
			return false;
		}
		try
		{
			if (_current != null)
			{
				throw new InvalidOperationException("Another original presenter is still restoring its rig.");
			}
			MethodInfo methodInfo = AccessTools.Method(typeof(CharacterAnimEvent), "OnAnimatorIK", new Type[1] { typeof(int) });
			if (methodInfo == null)
			{
				throw new InvalidOperationException("Installed player has no supported native IK callback.");
			}
			Patches patchInfo = Harmony.GetPatchInfo(methodInfo);
			if (patchInfo != null)
			{
				foreach (string owner in patchInfo.Owners)
				{
					if (owner == "com.theconcernedcat.valheim.concernedladders.pose")
					{
						throw new InvalidOperationException("Retire the rejected ladder pose before installing the original presenter.");
					}
				}
			}
			_harmony = new Harmony(harmonyId);
			_harmony.Patch(methodInfo, new HarmonyMethod(typeof(OriginalClimbPresenter), "BeforeNativeIK"), new HarmonyMethod(typeof(OriginalClimbPresenter), "AfterNativeIK"));
			_current = this;
			_ready = true;
			return true;
		}
		catch (Exception exception)
		{
			Log("Original climbing presentation unavailable: " + SafeFailure.Brief(exception));
			Dispose();
			return false;
		}
	}

	public void Publish(Player player, in ClimbPresentationFrame frame)
	{
		if (_failure != null)
		{
			throw new InvalidOperationException("Original presentation disabled after an IK fault: " + _failure);
		}
		if (!_ready || _disposing || !LocalOwner(player) || ((object)player == _closedPlayer && frame.Session == _closedSession))
		{
			return;
		}
		try
		{
			Validate(in frame);
			Actor actor = null;
			foreach (Actor value in _actors.Values)
			{
				if ((object)value.Rig.Player == player)
				{
					actor = value;
					break;
				}
			}
			if (actor == null)
			{
				if (_actors.Count >= 2)
				{
					throw new InvalidOperationException("Unexpected local presentation actor count.");
				}
				OriginalRigBinding originalRigBinding = OriginalRigBinding.Create(player);
				actor = new Actor(originalRigBinding);
				_actors.Add(originalRigBinding.Events, actor);
			}
			if (!actor.Rig.Valid)
			{
				throw new InvalidOperationException("Player rig/controller/scale changed; a fresh binding is required.");
			}
			bool flag = !actor.Active || actor.Frame.Session != frame.Session || actor.Frame.Discontinuity != frame.Discontinuity || actor.TuningVersion != ClimbTuning.Version;
			if (!flag && frame.Sample <= actor.Frame.Sample)
			{
				return;
			}
			float num = (flag ? 0f : (frame.SampleTime - actor.Frame.SampleTime));
			if (num < 0f || num > 0.5f)
			{
				throw new InvalidOperationException("Accepted presentation clock changed or stalled.");
			}
			float f = (flag ? 0f : (frame.Travel - actor.Frame.Travel));
			bool flag2 = !flag && actor.Frame.Motion == PresentedMotion.Slide;
			bool flag3 = frame.Motion == PresentedMotion.Slide;
			if (flag || (flag2 && !flag3))
			{
				float feetY = ((frame.Motion == PresentedMotion.GroundMount || frame.Motion == PresentedMotion.AirCatch || frame.Motion == PresentedMotion.TopEntry) ? frame.TransitionTo.Y : frame.Feet.Y);
				if (!actor.Gait.Bind(ClimbTuning.Gait, frame.Run.Bottom.Y + frame.RungOrigin, frame.Run.RungPitch, 0, frame.LastRung, actor.Rig.ShoulderHeight - ClimbTuning.PelvisDrop, feetY))
				{
					throw new InvalidOperationException("No real rung span can bind the original contacts.");
				}
				if (flag)
				{
					if (actor.Frame.Session != frame.Session)
					{
						actor.Fit.Reset();
					}
					actor.Age = 0f;
					actor.SlideBlend = 0f;
					actor.TuningVersion = ClimbTuning.Version;
					actor.Caught = frame.Motion == PresentedMotion.AirCatch;
				}
			}
			else if (frame.GeometryRevision != actor.Frame.GeometryRevision && !actor.Gait.RefreshBounds(frame.Run.Bottom.Y + frame.RungOrigin, 0, frame.LastRung))
			{
				throw new InvalidOperationException("Run refresh removed a contact or changed rung alignment.");
			}
			if ((frame.Motion == PresentedMotion.Climb || frame.Motion == PresentedMotion.Fast || frame.Motion == PresentedMotion.Hold) && !actor.Gait.Update(frame.Feet.Y, num, (frame.MenuHold || frame.Motion == PresentedMotion.Hold) && Mathf.Abs(f) < 1E-06f))
			{
				throw new InvalidOperationException("Invalid completed-travel sample for original contacts.");
			}
			actor.SlideBlend = Mathf.MoveTowards(actor.SlideBlend, flag3 ? 1f : 0f, num / (flag3 ? 0.08f : 0.12f));
			AdvanceTransition(actor, in frame, flag, num);
			actor.Age += num;
			actor.Frame = frame;
			actor.Active = true;
			actor.Seen = Time.time;
		}
		catch (Exception exception)
		{
			End(player, ClimbEnd.Fault);
			throw new InvalidOperationException("Original animation binding/frame failed: " + SafeFailure.Brief(exception));
		}
	}

	private static void AdvanceTransition(Actor actor, in ClimbPresentationFrame frame, bool fresh, float dt)
	{
		TransitionKind? transitionKind = frame.Motion switch
		{
			PresentedMotion.TopFinish => TransitionKind.TopExit, 
			PresentedMotion.StepOff => TransitionKind.SideExit, 
			PresentedMotion.BottomStepOff => TransitionKind.BottomStepOff, 
			PresentedMotion.TopEntry => TransitionKind.TopEntry, 
			_ => null, 
		};
		if (transitionKind.HasValue)
		{
			float num = Mathf.Clamp01(frame.Transition);
			if (actor.Transition == null || actor.Transition.Kind != transitionKind.Value)
			{
				actor.Transition = ClimbTuning.Action(transitionKind.Value);
				actor.EntryTimed = false;
				actor.TransitionProgress = (actor.ShownProgress = num);
				actor.ProgressRate = 0f;
				SeedWeights(actor, num);
			}
			else if (dt > 0.0001f)
			{
				actor.ProgressRate = Mathf.Max(0f, (num - actor.TransitionProgress) / dt);
				actor.TransitionProgress = num;
			}
			actor.ProgressAt = Time.time;
		}
		else if (fresh && (frame.Motion == PresentedMotion.GroundMount || frame.Motion == PresentedMotion.AirCatch))
		{
			actor.Transition = ClimbTuning.Action((frame.Motion == PresentedMotion.AirCatch) ? TransitionKind.AirCatch : TransitionKind.GroundMount);
			actor.TransitionProgress = (actor.ShownProgress = 0f);
			actor.ProgressRate = 1f / Mathf.Max(0.05f, actor.Transition.Seconds);
			actor.ProgressAt = Time.time;
			actor.EntryTimed = true;
			SeedWeights(actor, 0f);
		}
		else if (actor.Transition != null && actor.EntryTimed)
		{
			actor.TransitionProgress = Mathf.Clamp01(actor.TransitionProgress + dt / Mathf.Max(0.05f, actor.Transition.Seconds));
			actor.ProgressAt = Time.time;
			if (actor.TransitionProgress >= 1f && actor.ShownProgress >= 0.999f)
			{
				actor.Transition = null;
			}
		}
		else
		{
			actor.Transition = null;
		}
	}

	private static void SeedWeights(Actor actor, float progress)
	{
		if (actor.Transition != null)
		{
			for (int i = 0; i < 4; i++)
			{
				actor.ShownWeight[i] = actor.Transition.WeightAt(i, progress);
			}
		}
	}

	public void End(Player? player, ClimbEnd reason)
	{
		foreach (Actor value in _actors.Values)
		{
			if ((object)player == null || (object)value.Rig.Player == player)
			{
				if (value.Active)
				{
					value.Fit.Report(this, Time.time, final: true);
					ContactTrace.Flush();
				}
				value.Active = false;
				_closedPlayer = value.Rig.Player;
				_closedSession = value.Frame.Session;
			}
		}
	}

	public void Update(float deltaTime)
	{
		_remove.Clear();
		foreach (KeyValuePair<CharacterAnimEvent, Actor> actor in _actors)
		{
			Actor value = actor.Value;
			if (value.Active && value.Rig.Valid)
			{
				for (int i = 0; i < 4; i++)
				{
					if (value.Asked[i])
					{
						value.Fit.Solved(i, Vector3.Distance(value.Rig.Ends[i].position, value.LastPivot[i]));
					}
				}
				if (ClimbTuning.LogFit || ClimbTuning.TraceContacts)
				{
					MeasureContacts(value);
				}
			}
			if (!value.Rig.Valid || !LocalOwner(value.Rig.Player) || Time.time - value.Seen > 1f)
			{
				value.Active = false;
			}
			if (actor.Key == null || value.Rig.Player == null || (!value.Active && !value.Lease.Pending))
			{
				_remove.Add(actor.Key);
			}
		}
		foreach (CharacterAnimEvent item in _remove)
		{
			_actors.Remove(item);
		}
		if (_closedPlayer == null)
		{
			_closedPlayer = null;
		}
	}

	private void MeasureContacts(Actor actor)
	{
		for (int i = 0; i < 4; i++)
		{
			ContactRecord record = actor.Record[i];
			if (!record.Has || record.Skipped || record.Effective <= 0.01f)
			{
				actor.Anchored[i] = false;
				if (ClimbTuning.TraceContacts && record.Has)
				{
					ContactTrace.Row(in actor.Frame, i, record, Vector3.zero, -1f, 0f);
				}
				continue;
			}
			Vector3 vector = actor.Rig.Patch(i);
			float num = Vector3.Distance(vector, record.Target);
			RungProfile profile = record.Profile;
			Vector3 lhs = vector - record.RungCentre;
			float y = lhs.y;
			float num2 = Vector3.Dot(lhs, record.Outward);
			float num3;
			if (profile.Round)
			{
				num3 = Mathf.Sqrt(y * y + num2 * num2) - profile.Radius;
			}
			else
			{
				float num4 = Mathf.Abs(y) - profile.HalfHeight;
				float num5 = Mathf.Abs(num2) - profile.HalfDepth;
				num3 = ((num4 > 0f && num5 > 0f) ? Mathf.Sqrt(num4 * num4 + num5 * num5) : Mathf.Max(num4, num5));
			}
			float gap = (record.Hand ? (num3 - ClimbTuning.PalmThickness) : (y - (profile.Round ? profile.Radius : profile.HalfHeight)));
			bool faded = record.Effective < record.Requested - 0.01f;
			if (record.Planted && actor.Anchored[i] && (record.Target - actor.AnchorTarget[i]).sqrMagnitude < 1E-06f)
			{
				actor.Fit.Planted(i, num, gap, Vector3.Distance(vector, actor.AnchorPatch[i]), faded);
			}
			else if (record.Planted)
			{
				actor.Anchored[i] = true;
				actor.AnchorTarget[i] = record.Target;
				actor.AnchorPatch[i] = vector;
				actor.Fit.Planted(i, num, gap, 0f, faded);
			}
			else
			{
				actor.Anchored[i] = false;
			}
			if (!ClimbTuning.TraceContacts)
			{
				continue;
			}
			float sideways = 0f;
			float toward = 0f;
			if (!record.Hand)
			{
				Knee(actor, i, record.Outward, out sideways, out toward);
			}
			else
			{
				Transform boneTransform = actor.Rig.Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
				Transform boneTransform2 = actor.Rig.Animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
				if (boneTransform != null && boneTransform2 != null)
				{
					Vector3 vector2 = boneTransform2.position - boneTransform.position;
					vector2.y = 0f;
					sideways = Vector3.SignedAngle(vector2, Vector3.Cross(Vector3.up, -record.Outward), Vector3.up);
				}
			}
			ContactTrace.Row(in actor.Frame, i, record, vector, num, gap, sideways, toward);
		}
	}

	private static void Knee(Actor actor, int limb, Vector3 outward, out float sideways, out float toward)
	{
		sideways = (toward = 0f);
		Animator animator = actor.Rig.Animator;
		bool flag = limb == 2;
		Transform boneTransform = animator.GetBoneTransform(flag ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg);
		Transform boneTransform2 = animator.GetBoneTransform(flag ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg);
		Transform boneTransform3 = animator.GetBoneTransform(flag ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot);
		if (!(boneTransform == null) && !(boneTransform2 == null) && !(boneTransform3 == null))
		{
			Vector3 vector = boneTransform3.position - boneTransform.position;
			float num = ((vector.sqrMagnitude > 1E-06f) ? Mathf.Clamp01(Vector3.Dot(boneTransform2.position - boneTransform.position, vector) / vector.sqrMagnitude) : 0f);
			Vector3 lhs = boneTransform2.position - (boneTransform.position + vector * num);
			Vector3 rhs = -outward;
			Vector3 rhs2 = Vector3.Cross(Vector3.up, rhs);
			sideways = Vector3.Dot(lhs, rhs2) * (flag ? (-1f) : 1f);
			toward = Vector3.Dot(lhs, rhs);
		}
	}

	private static void BeforeNativeIK(CharacterAnimEvent __instance, int layerIndex)
	{
		OriginalClimbPresenter current = _current;
		if (current == null || layerIndex != 0 || !current._actors.TryGetValue(__instance, out Actor value))
		{
			return;
		}
		try
		{
			value.Lease.RestoreInIK(value.Rig.Animator);
		}
		catch (Exception error)
		{
			current.Fail("Original IK restoration failed", error);
		}
	}

	private static void AfterNativeIK(CharacterAnimEvent __instance, int layerIndex)
	{
		OriginalClimbPresenter current = _current;
		if (current == null || !current._actors.TryGetValue(__instance, out Actor value))
		{
			return;
		}
		if (layerIndex != 0)
		{
			if (!value.Active || !current._ready || current._disposing)
			{
				return;
			}
			try
			{
				value.Lease.ReassertInIK(value.Rig.Animator);
				return;
			}
			catch (Exception error)
			{
				current.Fail("Original climbing pose stopped", error);
				return;
			}
		}
		if (value.Active && current._ready && !current._disposing && LocalOwner(value.Rig.Player))
		{
			try
			{
				current.PoseInIK(value);
			}
			catch (Exception error2)
			{
				current.Fail("Original climbing pose stopped", error2);
				try
				{
					value.Lease.RestoreInIK(value.Rig.Animator);
				}
				catch
				{
				}
			}
		}
		if (!value.Active && !value.Lease.Pending)
		{
			current._actors.Remove(__instance);
		}
	}

	private static bool LocalOwner(Player player)
	{
		if (player == null || player != Player.m_localPlayer)
		{
			return false;
		}
		ZNetView component = player.GetComponent<ZNetView>();
		if (component != null && component.IsValid())
		{
			return component.IsOwner();
		}
		return false;
	}

	private static void Validate(in ClimbPresentationFrame frame)
	{
		if (frame.Sample < 0 || !LimbGait.Finite(frame.SampleTime) || !LimbGait.Finite(frame.Travel) || !LimbGait.Finite(frame.StepTravel) || !frame.Feet.IsFinite || !frame.Run.IsClimbable || !frame.Run.Bottom.IsFinite || !frame.Run.Top.IsFinite || !frame.TransitionFrom.IsFinite || !frame.TransitionTo.IsFinite || !LimbGait.Finite(frame.RungPlane) || !LimbGait.Finite(frame.RungOrigin) || !LimbGait.Finite(frame.Transition) || frame.LastRung < 1 || frame.LastRung > 4096 || (frame.Face != LadderFace.Front && frame.Face != LadderFace.Rear) || frame.Motion < PresentedMotion.Hold || frame.Motion > PresentedMotion.TopEntry || (frame.NativeAction & ~(NativeActionParts.LeftArm | NativeActionParts.RightArm | NativeActionParts.UpperBody | NativeActionParts.Head)) != NativeActionParts.None)
		{
			throw new InvalidOperationException("Non-finite or unsupported accepted presentation frame.");
		}
	}

	public void Dispose()
	{
		if (_disposing)
		{
			return;
		}
		_ready = false;
		_disposing = true;
		End(null, ClimbEnd.Disabled);
		foreach (Actor value in _actors.Values)
		{
			if (!(value.Rig.Animator == null) && value.Lease.Pending)
			{
				try
				{
					OriginalIKCleanup.Schedule(value.Rig.Animator, value.Lease);
				}
				catch (Exception exception)
				{
					Log("Original rig cleanup could not be scheduled: " + SafeFailure.Brief(exception));
				}
			}
		}
		_actors.Clear();
		_remove.Clear();
		_closedPlayer = null;
		try
		{
			_harmony?.UnpatchSelf();
		}
		catch
		{
		}
		_harmony = null;
		if (_current == this)
		{
			_current = null;
		}
	}

	private void Fail(string context, Exception error)
	{
		if (_failure == null)
		{
			_failure = SafeFailure.Brief(error);
			_ready = false;
			End(null, ClimbEnd.Fault);
			Log(context + ": " + _failure);
		}
	}

	private void Log(string message)
	{
		try
		{
			_log(message);
		}
		catch
		{
		}
	}

	private void PoseInIK(Actor actor)
	{
		OriginalRigBinding rig = actor.Rig;
		if (!rig.Valid || !actor.Gait.Ready)
		{
			actor.Active = false;
			return;
		}
		rig.BindBodyInIK();
		Animator animator = rig.Animator;
		ClimbPresentationFrame frame = actor.Frame;
		Vector3 vector = new Vector3(frame.Run.StandingSide.X, 0f, frame.Run.StandingSide.Z) * (float)frame.Face;
		Vector3 vector2 = -vector;
		Vector3 vector3 = Vector3.Cross(Vector3.up, vector2);
		Vector3 position = rig.Player.transform.position;
		NativeActionParts nativeActionParts = frame.NativeAction;
		NativeKind nativeKind = NativeActionNow(animator);
		if (nativeKind == NativeKind.Attack && frame.HammerEquipped)
		{
			nativeActionParts |= NativeActionParts.RightArm;
		}
		else if (nativeKind != NativeKind.None)
		{
			nativeActionParts |= NativeActionParts.LeftArm | NativeActionParts.RightArm | NativeActionParts.UpperBody | NativeActionParts.Head;
		}
		actor.Fit.Begin(this, frame.Motion, Time.time);
		TransitionAction transition = actor.Transition;
		float num = ((transition != null) ? actor.ProgressNow(Time.time, Time.deltaTime) : 0f);
		bool num2 = transition != null && (transition.Kind == TransitionKind.TopExit || transition.Kind == TransitionKind.SideExit || transition.Kind == TransitionKind.BottomStepOff);
		float num3 = transition?.BodyWeightAt(num) ?? 1f;
		if ((nativeActionParts & NativeActionParts.UpperBody) != NativeActionParts.None)
		{
			num3 = 0f;
		}
		float num4 = actor.Gait.Sway * (1f - actor.SlideBlend);
		float num5 = Mathf.Lerp(ClimbTuning.Lean, ClimbTuning.SlideLean, actor.SlideBlend);
		if (transition != null)
		{
			num5 += transition.LeanAt(num);
		}
		Vector3 vector4 = Vector(frame.TransitionTo);
		Vector3 vector5 = Vector(frame.TransitionFrom);
		Vector3 vector6 = vector4 - vector5;
		vector6.y = 0f;
		Vector3 vector7 = ((vector6.sqrMagnitude > 0.0004f) ? vector6.normalized : vector2);
		bool flag = frame.Motion == PresentedMotion.Climb || frame.Motion == PresentedMotion.Fast;
		actor.Gait.Body(frame.Feet.Y, out var up, out var toward, out var lean);
		actor.BodyBlend = Mathf.MoveTowards(actor.BodyBlend, flag ? 1f : 0f, Time.deltaTime / 0.25f);
		float num6 = LimbGait.Ease(actor.BodyBlend);
		num5 += lean * num6;
		float num7 = transition?.BodyUpAt(num) ?? 0f;
		float num8 = transition?.BodyTowardAt(num) ?? 0f;
		float num9 = Smooth01((position.y - (frame.Run.Top.Y - ClimbTuning.TopApproachFrom)) / ClimbTuning.TopApproachSpan);
		if (transition != null && transition.Kind == TransitionKind.TopExit)
		{
			num9 *= 1f - Smooth01((num - 0.05f) / 0.3f);
		}
		if (transition != null && transition.Kind == TransitionKind.TopEntry)
		{
			num9 *= Smooth01((num - 0.55f) / 0.3f);
		}
		num7 -= ClimbTuning.TopApproachDrop * num9;
		num8 += ClimbTuning.TopApproachToward * num9;
		num5 += ClimbTuning.TopApproachLean * num9;
		if (num2 && vector4.y < position.y)
		{
			float num10 = 0f;
			for (int i = 2; i < 4; i++)
			{
				num10 = Mathf.Max(num10, transition.ReachAt(i, num));
			}
			num7 += (vector4.y - position.y) * num10;
		}
		Vector3 vector8 = position + Vector3.up * (rig.PelvisHeight - ClimbTuning.PelvisDrop + up * num6 + num7) + vector2 * (ClimbTuning.PelvisToward + toward * num6 + num8) + vector3 * (ClimbTuning.SwayShift * num4);
		Quaternion quaternion = Quaternion.LookRotation(vector2, Vector3.up) * Quaternion.AngleAxis(num5, Vector3.right) * Quaternion.AngleAxis((0f - num4) * ClimbTuning.SwayTilt, Vector3.forward) * rig.BodyInPlayer;
		Vector3 bodyPosition = animator.bodyPosition;
		Quaternion bodyRotation = animator.bodyRotation;
		Vector3 vector9 = Quaternion.Inverse(bodyRotation) * (rig.Roots[3].position - rig.Roots[2].position);
		Vector3 vector10 = quaternion * vector9;
		vector10.y = 0f;
		if (vector10.sqrMagnitude > 0.0001f)
		{
			quaternion = Quaternion.AngleAxis(Mathf.Clamp(Vector3.SignedAngle(vector10, vector3, Vector3.up), -40f, 40f) * ClimbTuning.SquareHips, Vector3.up) * quaternion;
		}
		Vector3 position2 = vector8 + quaternion * rig.BodyFromPelvis;
		actor.Lease.BodyInIK(animator, position2, quaternion, num3);
		Vector3 bodyPosition2 = animator.bodyPosition;
		Quaternion quaternion2 = animator.bodyRotation * Quaternion.Inverse(bodyRotation);
		float num11 = 0f;
		Vector3 vector11 = bodyPosition2;
		if (ClimbTuning.SquareChest > 0f && num3 > 0f)
		{
			Transform boneTransform = animator.GetBoneTransform(HumanBodyBones.Chest);
			Vector3 vector12 = quaternion2 * (rig.Roots[1].position - rig.Roots[0].position);
			vector12.y = 0f;
			if (boneTransform != null && boneTransform.parent != null && vector12.sqrMagnitude > 0.0001f)
			{
				float num12 = Mathf.Clamp(Vector3.SignedAngle(vector12, vector3, Vector3.up), -40f, 40f) * ClimbTuning.SquareChest * num3;
				Quaternion quaternion3 = Quaternion.AngleAxis(num12, Vector3.up) * boneTransform.rotation;
				animator.SetBoneLocalRotation(HumanBodyBones.Chest, Quaternion.Inverse(boneTransform.parent.rotation) * quaternion3);
				actor.ChestTwist = num12;
				num11 = num12;
				vector11 = bodyPosition2 + quaternion2 * (boneTransform.position - bodyPosition);
			}
		}
		for (int j = 0; j < 4; j++)
		{
			actor.Asked[j] = false;
			actor.Record[j] = default(ContactRecord);
		}
		for (int k = 0; k < 4; k++)
		{
			bool flag2 = k < 2;
			if ((k == 0 && (nativeActionParts & (NativeActionParts.LeftArm | NativeActionParts.UpperBody)) != NativeActionParts.None) || (k == 1 && (nativeActionParts & (NativeActionParts.RightArm | NativeActionParts.UpperBody)) != NativeActionParts.None))
			{
				actor.Record[k] = new ContactRecord
				{
					Native = true
				};
				continue;
			}
			OriginalLimbContact originalLimbContact = actor.Gait.Contact(k);
			float num13 = ((k == 0 || k == 2) ? (-1f) : 1f);
			Vector3 vector13 = bodyPosition2 + quaternion2 * (rig.Roots[k].position - bodyPosition);
			if (flag2 && num11 != 0f)
			{
				vector13 = vector11 + Quaternion.AngleAxis(num11, Vector3.up) * (vector13 - vector11);
			}
			float num14 = Math.Min(ClimbPresentationFrame.HalfWidth - 0.04f, flag2 ? ClimbTuning.HandSpan : ClimbTuning.FootSpan);
			RungProfile profile = frame.ProfileAt(originalLimbContact.Height);
			float num15;
			float faceOffset;
			if (!profile.Round)
			{
				num15 = (flag2 ? (ClimbTuning.HandAbove + profile.HalfHeight - ClimbPresentationFrame.HalfHeight) : (ClimbTuning.FootAbove + profile.HalfHeight - ClimbPresentationFrame.HalfHeight));
				faceOffset = (flag2 ? ClimbTuning.HandOut : ClimbTuning.FootOut);
			}
			else if (flag2)
			{
				float f = ClimbTuning.HandWrapDegrees * ((float)Math.PI / 180f);
				float num16 = profile.Radius + ClimbTuning.PalmThickness;
				num15 = num16 * Mathf.Cos(f);
				faceOffset = num16 * Mathf.Sin(f);
			}
			else
			{
				num15 = profile.Radius + ClimbTuning.FootAbove - ClimbPresentationFrame.HalfHeight;
				faceOffset = ClimbTuning.FootOut;
			}
			Vector3 vector14 = RungSurface(in frame, originalLimbContact.Height + num15, faceOffset, num14 * num13);
			if (originalLimbContact.Swing > 0f)
			{
				actor.Gait.Arc(k, originalLimbContact.Swing, out var lat, out var outward, out var upward);
				vector14 += vector3 * (num13 * lat) + vector * outward + Vector3.up * upward;
			}
			float num17 = 0f;
			if (flag2)
			{
				float num18 = Smooth01((vector13.y - originalLimbContact.Height - ClimbTuning.RailFromBelow) / ClimbTuning.RailBlend);
				if (num18 > 0f)
				{
					float worldY = Mathf.Min(frame.Run.Top.Y - 0.04f, vector13.y - ClimbTuning.RailBelowShoulder);
					Vector3 b = RungSurface(in frame, worldY, ClimbTuning.RailOut, num13 * ClimbTuning.RailSide);
					vector14 = Vector3.Lerp(vector14, b, num18);
					num17 = num18;
				}
			}
			if (actor.SlideBlend > 0f)
			{
				float worldY2 = position.y + (flag2 ? (rig.ShoulderHeight - 0.16f) : 0.24f);
				Vector3 b2 = RungSurface(in frame, worldY2, ClimbPresentationFrame.HalfDepth + (flag2 ? 0.02f : 0.11f), num13 * (flag2 ? 0.32f : 0.25f));
				vector14 = Vector3.Lerp(vector14, b2, LimbGait.Ease(actor.SlideBlend));
			}
			float num19 = 1f;
			float num20 = 0f;
			if (transition != null)
			{
				num19 = transition.WeightAt(k, num);
				num20 = transition.ReachAt(k, num);
				if (num20 > 0f)
				{
					Vector3 b3;
					if (flag2)
					{
						float num21 = ((transition.Kind == TransitionKind.TopEntry) ? vector5.y : vector4.y);
						b3 = RungSurface(in frame, num21 + ClimbTuning.PalmThickness, 0f - ClimbTuning.LipOnto, num13 * ClimbTuning.HandSpan);
					}
					else if (transition.Kind == TransitionKind.TopEntry)
					{
						b3 = new Vector3(position.x, vector5.y, position.z) + vector3 * (num13 * ClimbTuning.FootSpan);
					}
					else if (transition.Kind == TransitionKind.TopExit)
					{
						float worldY3 = ((transition.Kind == TransitionKind.TopEntry) ? vector5.y : vector4.y);
						b3 = RungSurface(in frame, worldY3, 0f - (ClimbTuning.LipOnto + ClimbTuning.FootOnto), num13 * ClimbTuning.FootSpan);
					}
					else
					{
						b3 = ((transition.Kind == TransitionKind.TopEntry) ? vector5 : vector4) + Vector3.Cross(Vector3.up, vector7) * (num13 * (0f - ClimbTuning.FootSpan));
					}
					vector14 = Vector3.Lerp(vector14, b3, num20);
				}
				vector14 += vector3 * (num13 * transition.ArcAt(k, num, 0)) + vector * transition.ArcAt(k, num, 1) + Vector3.up * transition.ArcAt(k, num, 2);
			}
			else if (flag2)
			{
				num19 *= 1f - Smooth01((position.y - (frame.Run.Top.Y - ClimbTuning.TopHandsFree)) / ClimbTuning.TopHandsFreeSpan);
			}
			Vector3 vector15;
			Quaternion rotation;
			if (flag2)
			{
				vector15 = GripGoal(vector14, Vector3.Slerp(vector2, vector7, num20), Mathf.Lerp(ClimbTuning.HandFingerPitch, 90f, num20), out rotation);
			}
			else
			{
				Vector3 vector16 = Vector3.Slerp(vector2, vector7, num20);
				rotation = Quaternion.LookRotation(vector16, Vector3.up);
				vector15 = vector14 - vector16 * ClimbTuning.FootBallForward + Vector3.up * ClimbTuning.FootAnkleHeight;
			}
			float num22 = Vector3.Distance(vector13, vector15);
			bool planted = originalLimbContact.Planted && transition == null && num17 <= 0f && actor.SlideBlend <= 0f && num19 >= 0.999f;
			ContactRecord contactRecord = new ContactRecord
			{
				Has = true,
				Target = vector14,
				Planted = planted,
				Requested = num19,
				Ratio = num22 / rig.Reach[k],
				Root = vector13,
				Chain = rig.Reach[k],
				Progress = ((transition != null) ? num : (-1f)),
				Descending = actor.Gait.Descending,
				RungCentre = RungSurface(in frame, originalLimbContact.Height, 0f, num14 * num13),
				Hand = flag2,
				Outward = vector,
				Profile = profile
			};
			if (transition != null || actor.ShownWeight[k] < 0.999f)
			{
				num19 = Mathf.MoveTowards(actor.ShownWeight[k], num19, Time.deltaTime / ClimbTuning.WeightRiseSeconds);
			}
			actor.ShownWeight[k] = num19;
			contactRecord.Requested = num19;
			if (num19 <= 0.01f)
			{
				actor.Record[k] = contactRecord;
				continue;
			}
			actor.Fit.Sample(k, num22 / rig.Reach[k], vector13, position, in frame, vector);
			if (num22 > rig.Reach[k] * 1.01f)
			{
				actor.Fit.Skip(k, planted);
				contactRecord.Skipped = true;
				actor.Record[k] = contactRecord;
				continue;
			}
			num19 = (contactRecord.Effective = num19 * (1f - Smooth01((num22 / rig.Reach[k] - ClimbTuning.FadeFrom) / (1.01f - ClimbTuning.FadeFrom))));
			actor.Record[k] = contactRecord;
			actor.Lease.GoalInIK(animator, k, vector15, rotation, num19);
			if (num19 >= 0.999f)
			{
				actor.Asked[k] = true;
				actor.LastPivot[k] = vector15;
			}
			Vector3 vector17 = (flag2 ? (vector13 + vector * ClimbTuning.ElbowOut + vector3 * (num13 * ClimbTuning.ElbowSide) - Vector3.up * ClimbTuning.ElbowDown) : (vector13 + vector2 * ClimbTuning.KneeToward + vector3 * (num13 * ClimbTuning.KneeSide) - Vector3.up * ClimbTuning.KneeDown));
			if (!flag2 && !actor.Gait.Descending && ClimbTuning.KneeMidAscent > 0f)
			{
				Vector3 b4 = (vector13 + vector15) * 0.5f + vector2 * ClimbTuning.KneeAhead + vector3 * (num13 * ClimbTuning.KneeSide);
				vector17 = Vector3.Lerp(vector17, b4, ClimbTuning.KneeMidAscent);
			}
			actor.Lease.HintInIK(animator, k, vector17, num19 * 0.75f);
		}
		actor.Fit.Report(this, Time.time, final: false);
	}

	private static Vector3 GripGoal(Vector3 contact, Vector3 toward, float fingerPitch, out Quaternion rotation)
	{
		float f = fingerPitch * ((float)Math.PI / 180f);
		Vector3 vector = Vector3.up * Mathf.Cos(f) + toward * Mathf.Sin(f);
		Vector3 vector2 = toward * Mathf.Cos(f) - Vector3.up * Mathf.Sin(f);
		rotation = Quaternion.LookRotation(vector, -vector2 * ClimbTuning.HandNormalSign);
		return contact - vector * ClimbTuning.HandPalmAlong - vector2 * ClimbTuning.HandPalmDepth;
	}

	private static Vector3 RungSurface(in ClimbPresentationFrame frame, float worldY, float faceOffset, float lateral)
	{
		return Vector(OriginalRungSurface.At(frame.Run, frame.Face, frame.RungPlane, worldY, faceOffset, lateral));
	}

	private static float Smooth01(float value)
	{
		float num = Mathf.Clamp01(value);
		return num * num * (3f - 2f * num);
	}

	private static float Window(float progress, float from, float to)
	{
		return LimbGait.Ease((progress - from) / (to - from));
	}

	private static Vector3 Vector(ClimbPoint point)
	{
		return new Vector3(point.X, point.Y, point.Z);
	}

	private static NativeKind NativeActionNow(Animator animator)
	{
		NativeKind nativeKind = NativeKind.None;
		for (int i = 0; i < animator.layerCount; i++)
		{
			nativeKind = Strongest(nativeKind, Tag(animator.GetCurrentAnimatorStateInfo(i)));
			if (animator.IsInTransition(i))
			{
				nativeKind = Strongest(nativeKind, Tag(animator.GetNextAnimatorStateInfo(i)));
			}
		}
		return nativeKind;
	}

	private static NativeKind Tag(AnimatorStateInfo state)
	{
		if (!state.IsTag("minoraction") && !state.IsTag("minoraction_fast"))
		{
			if (!state.IsTag("attack"))
			{
				return NativeKind.None;
			}
			return NativeKind.Attack;
		}
		return NativeKind.Minor;
	}

	private static NativeKind Strongest(NativeKind a, NativeKind b)
	{
		if (a != NativeKind.Minor && b != NativeKind.Minor)
		{
			if (a != NativeKind.Attack && b != NativeKind.Attack)
			{
				return NativeKind.None;
			}
			return NativeKind.Attack;
		}
		return NativeKind.Minor;
	}

	void IClimbPresenter.Publish(Player player, in ClimbPresentationFrame frame)
	{
		Publish(player, in frame);
	}
}
