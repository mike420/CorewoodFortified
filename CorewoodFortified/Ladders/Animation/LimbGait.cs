using System;

namespace CorewoodFortified.LadderAnimation;

internal sealed class LimbGait
{
	internal const int LeftHand = 0;

	internal const int RightHand = 1;

	internal const int LeftFoot = 2;

	internal const int RightFoot = 3;

	private readonly float[] _shown = new float[4];

	private readonly float[] _swing = new float[4];

	private readonly float[] _settled = new float[4];

	private readonly bool[] _synced = new bool[4];

	private readonly bool[] _planted = new bool[4];

	private readonly float[] _shift = new float[2];

	private float _reverseUntil;

	private float _clock;

	private float _stillSince;

	private readonly int[][] _baseRung = new int[2][]
	{
		new int[4],
		new int[4]
	};

	private GaitShape _shape = new GaitShape();

	private float _base;

	private float _pitch;

	private float _cycle;

	private float _shoulderRel;

	private float _lastFeet;

	private int _first;

	private int _last;

	private bool _holding;

	private bool _descending;

	internal const float StallTravel = 0.002f;

	internal const float StallSeconds = 0.15f;

	internal bool Ready { get; private set; }

	internal GaitShape Shape => _shape;

	internal ClimbLoop Loop
	{
		get
		{
			if (!_descending)
			{
				return _shape.Up;
			}
			return _shape.Down;
		}
	}

	internal bool Descending => _descending;

	internal float Sway { get; private set; }

	internal float CycleMetres => _cycle;

	internal bool Bind(GaitShape shape, float rungBaseY, float pitch, int firstRung, int lastRung, float shoulderRel, float feetY)
	{
		Ready = false;
		if (shape == null || !shape.Valid || !Finite(rungBaseY) || !Finite(pitch) || pitch < 0.1f || pitch > 1f || firstRung < -4096 || lastRung > 4096 || lastRung - firstRung < 1 || !Finite(shoulderRel) || shoulderRel < 0.5f || shoulderRel > 3f || !Finite(feetY))
		{
			return false;
		}
		_shape = shape.Copy();
		_base = rungBaseY;
		_pitch = pitch;
		_first = firstRung;
		_last = lastRung;
		_shoulderRel = shoulderRel;
		_cycle = (float)_shape.Up.StrideRungs * pitch;
		BaseRungs(_shape.Up, _baseRung[0]);
		BaseRungs(_shape.Down, _baseRung[1]);
		_holding = false;
		_descending = false;
		_lastFeet = feetY;
		_shift[0] = (_shift[1] = 0f);
		_clock = 0f;
		_stillSince = 0f;
		_reverseUntil = -1f;
		Ready = true;
		Pose(feetY, planted: true);
		for (int i = 0; i < 4; i++)
		{
			_synced[i] = true;
		}
		return true;
	}

	private void BaseRungs(ClimbLoop loop, int[] rungs)
	{
		for (int i = 0; i < 4; i++)
		{
			bool num = i < 2;
			float num2 = (1f - loop.Length[i]) * _cycle;
			float num3 = (num ? (_shoulderRel + _shape.HandMidFromShoulder - num2 * 0.5f) : _shape.FootLowRel);
			float num4 = ((loop == _shape.Down) ? (loop.Start[i] + loop.Length[i]) : loop.Start[i]);
			float num5 = (_cycle * num4 + num3) / _pitch;
			bool flag = loop == _shape.Down;
			double num6 = (num ? Math.Floor((double)num5 + 0.0001) : (flag ? Math.Round(num5) : Math.Ceiling((double)num5 - 0.0001)));
			rungs[i] = (int)num6 - (flag ? loop.StrideRungs : 0);
		}
	}

	internal bool RefreshBounds(float rungBaseY, int firstRung, int lastRung)
	{
		if (!Ready || !Finite(rungBaseY) || lastRung - firstRung < 1)
		{
			Ready = false;
			return false;
		}
		float num = (rungBaseY - _base) / _pitch;
		int num2 = (int)Math.Round(num);
		if (Math.Abs(num - (float)num2) > 0.002f)
		{
			Ready = false;
			return false;
		}
		_first = firstRung + num2;
		_last = lastRung + num2;
		return true;
	}

	internal bool Update(float feetY, float seconds, bool hold)
	{
		if (!Ready || !Finite(feetY) || !Finite(seconds) || seconds < 0f || seconds > 0.5f)
		{
			return false;
		}
		_clock += seconds;
		float num = feetY - _lastFeet;
		_lastFeet = feetY;
		if (Math.Abs(num) > 0.002f * Math.Max(seconds, 0.001f) / 0.02f)
		{
			_stillSince = _clock;
		}
		if (!hold && _clock - _stillSince > 0.15f)
		{
			hold = true;
		}
		if (!hold && Math.Abs(num) > 0.0005f)
		{
			bool flag = num < 0f;
			if (flag != _descending)
			{
				_descending = flag;
				MatchPhase(feetY);
				_reverseUntil = _clock + _shape.ReverseSeconds;
				for (int i = 0; i < 4; i++)
				{
					_synced[i] = false;
				}
			}
		}
		if (hold && !_holding)
		{
			for (int j = 0; j < 4; j++)
			{
				bool flag2 = j < 2;
				float low = feetY + (flag2 ? (_shoulderRel - _shape.HandSettleBelow) : (_shape.FootLowRel - _shape.FootSettleBelow));
				float high = feetY + (flag2 ? (_shoulderRel + _shape.HandSettleAbove) : (_shape.FootLowRel + _shape.FootSettleAbove));
				_settled[j] = NearestRung(_shown[j], low, high);
			}
		}
		_holding = hold;
		float num2 = 0f;
		for (int k = 0; k < 4; k++)
		{
			float swing = 0f;
			float num3 = (hold ? _settled[k] : Target(k, feetY, planted: false, out swing));
			num2 += ((k == 1 || k == 3) ? 0.5f : (-0.5f)) * (float)Math.Sin(Math.PI * (double)swing);
			float num4 = (hold ? _shape.SettleSeconds : ((_clock < _reverseUntil) ? _shape.ReverseSeconds : _shape.RejoinSeconds)) / 3f;
			if (_synced[k] && !hold && Math.Abs(_shown[k] - num3) < _pitch * 0.5f)
			{
				_shown[k] = num3;
			}
			else
			{
				float num5 = ((seconds <= 0f) ? 0f : (1f - (float)Math.Exp((0f - seconds) / num4)));
				_shown[k] += (num3 - _shown[k]) * num5;
				_synced[k] = !hold && Math.Abs(_shown[k] - num3) < 0.002f;
				if (_synced[k])
				{
					_shown[k] = num3;
				}
			}
			_swing[k] = (_synced[k] ? swing : 0f);
			_planted[k] = (_synced[k] && swing <= 0f) || (hold && Math.Abs(_shown[k] - num3) < 0.002f);
		}
		Sway = Clamp(num2, -1f, 1f);
		return true;
	}

	internal void Pose(float feetY, bool planted)
	{
		for (int i = 0; i < 4; i++)
		{
			_shown[i] = Target(i, feetY, planted, out var swing);
			_swing[i] = (planted ? 0f : swing);
			_planted[i] = planted || swing <= 0f;
		}
		Sway = 0f;
	}

	internal OriginalLimbContact Contact(int limb)
	{
		if (!Ready || limb < 0 || limb > 3)
		{
			return default(OriginalLimbContact);
		}
		float num = _swing[limb];
		return new OriginalLimbContact(_shown[limb], (num > 0f) ? ((float)Math.Sin(Math.PI * (double)num)) : 0f, _planted[limb], num);
	}

	internal void Arc(int limb, float swing, out float lat, out float outward, out float upward)
	{
		float[][] table = Loop.Arc[limb];
		lat = ClimbLoop.Sample(table, swing, 0);
		outward = ClimbLoop.Sample(table, swing, 1);
		upward = ClimbLoop.Sample(table, swing, 2);
	}

	internal float Phase(float feetY)
	{
		float num = (feetY + _shift[_descending ? 1u : 0u] - _base) / _cycle;
		return num - (float)Math.Floor(num);
	}

	internal void Body(float feetY, out float up, out float toward, out float lean)
	{
		up = (toward = (lean = 0f));
		if (Ready && Finite(feetY))
		{
			float x = Phase(feetY);
			ClimbLoop loop = Loop;
			up = ClimbLoop.Sample(loop.Body, x, 0);
			toward = ClimbLoop.Sample(loop.Body, x, 1);
			lean = ClimbLoop.Sample(loop.Lean, x);
		}
	}

	internal float Target(int limb, float feetY, bool planted, out float swing)
	{
		ClimbLoop loop = Loop;
		int[] obj = _baseRung[_descending ? 1u : 0u];
		float num = (feetY + _shift[_descending ? 1u : 0u] - _base) / _cycle - loop.Start[limb];
		float num2 = (float)Math.Floor(num);
		float num3 = Clamp((num - num2) / loop.Length[limb], 0f, 1f);
		if (planted)
		{
			num3 = ((num3 < 0.5f) ? 0f : 1f);
		}
		long num4 = obj[limb] + (long)num2 * loop.StrideRungs;
		int num5 = ClampRung(num4);
		int num6 = ClampRung(num4 + loop.StrideRungs);
		bool flag = num5 != num6 && num3 > 0f && num3 < 1f;
		swing = (flag ? num3 : 0f);
		return _base + ((float)num5 + (float)(num6 - num5) * ClimbLoop.Sample(loop.Swing, num3)) * _pitch;
	}

	private void MatchPhase(float feetY)
	{
		int num = (_descending ? 1 : 0);
		float num2 = float.MaxValue;
		float num3 = 0f;
		int strideRungs = Loop.StrideRungs;
		for (int i = -strideRungs; i < strideRungs; i++)
		{
			_shift[num] = (float)i * _pitch;
			float num4 = 0f;
			for (int j = 0; j < 4; j++)
			{
				num4 += Math.Abs(Target(j, feetY, planted: false, out var _) - _shown[j]);
			}
			if (num4 < num2 - 0.0001f)
			{
				num2 = num4;
				num3 = _shift[num];
			}
		}
		_shift[num] = num3;
	}

	internal static void Recovery(ClimbLoop loop, int limb, float pitch, float speed, out float seconds, out float mean, out float peak)
	{
		float num = (float)loop.StrideRungs * pitch / speed;
		seconds = loop.Length[limb] * num;
		float num2 = (float)loop.StrideRungs * pitch;
		mean = num2 / seconds;
		peak = 0f;
		for (int i = 0; i < loop.Swing.Length - 1; i++)
		{
			float num3 = (loop.Swing[i + 1] - loop.Swing[i]) * (float)(loop.Swing.Length - 1);
			peak = Math.Max(peak, num3 * mean);
		}
	}

	private float NearestRung(float height, float low, float high)
	{
		long num = (long)Math.Round((height - _base) / _pitch);
		long num2 = (long)Math.Ceiling((double)((low - _base) / _pitch) - 0.0001);
		long num3 = (long)Math.Floor((double)((high - _base) / _pitch) + 0.0001);
		if (num2 <= num3)
		{
			num = Math.Max(num2, Math.Min(num3, num));
		}
		return _base + (float)ClampRung(num) * _pitch;
	}

	private int ClampRung(long rung)
	{
		return (int)Math.Max(_first, Math.Min(_last, rung));
	}

	internal static bool Finite(float value)
	{
		if (!float.IsNaN(value))
		{
			return !float.IsInfinity(value);
		}
		return false;
	}

	internal static float Clamp(float value, float min, float max)
	{
		return Math.Max(min, Math.Min(max, value));
	}

	internal static float Ease(float value)
	{
		float num = Clamp(value, 0f, 1f);
		return num * num * num * (10f + num * (-15f + 6f * num));
	}
}
