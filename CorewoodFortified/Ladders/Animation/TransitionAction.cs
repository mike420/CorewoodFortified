using System;

namespace CorewoodFortified.LadderAnimation;

internal sealed class TransitionAction
{
	internal sealed class Builder
	{
		private readonly TransitionAction _action;

		private readonly int _n;

		internal Builder(TransitionKind kind, float seconds, int samples = 41)
		{
			_n = samples;
			_action = new TransitionAction
			{
				Kind = kind,
				Seconds = seconds
			};
			for (int i = 0; i < 4; i++)
			{
				_action.Weight[i] = Fill(1f);
				_action.Reach[i] = Fill(0f);
				_action.Arc[i] = new float[samples][];
				for (int j = 0; j < samples; j++)
				{
					_action.Arc[i][j] = new float[3];
				}
			}
			_action.BodyWeight = Fill(1f);
			_action.BodyUp = Fill(0f);
			_action.BodyToward = Fill(0f);
			_action.Lean = Fill(0f);
		}

		internal Builder Weight(int limb, params float[] keys)
		{
			Keys(_action.Weight[limb], keys);
			return this;
		}

		internal Builder Reach(int limb, params float[] keys)
		{
			Keys(_action.Reach[limb], keys);
			return this;
		}

		internal Builder Body(params float[] keys)
		{
			Keys(_action.BodyWeight, keys);
			return this;
		}

		internal Builder BodyUp(params float[] keys)
		{
			Keys(_action.BodyUp, keys);
			return this;
		}

		internal Builder BodyToward(params float[] keys)
		{
			Keys(_action.BodyToward, keys);
			return this;
		}

		internal Builder Lean(params float[] keys)
		{
			Keys(_action.Lean, keys);
			return this;
		}

		internal Builder Arc(int limb, float from, float to, float lat, float outward, float up)
		{
			for (int i = 0; i < _n; i++)
			{
				float num = (float)i / (float)(_n - 1);
				if (!(num <= from) && !(num >= to))
				{
					float num2 = (float)Math.Sin(Math.PI * (double)(num - from) / (double)(to - from));
					num2 *= num2;
					_action.Arc[limb][i][0] += lat * num2;
					_action.Arc[limb][i][1] += outward * num2;
					_action.Arc[limb][i][2] += up * num2;
				}
			}
			return this;
		}

		internal TransitionAction Build()
		{
			return _action;
		}

		private float[] Fill(float value)
		{
			float[] array = new float[_n];
			for (int i = 0; i < _n; i++)
			{
				array[i] = value;
			}
			return array;
		}

		private void Keys(float[] table, float[] keys)
		{
			if (keys.Length < 2 || keys.Length % 2 != 0)
			{
				throw new ArgumentException("keys are progress/value pairs");
			}
			for (int i = 0; i < _n; i++)
			{
				float num = (float)i / (float)(_n - 1);
				float num2 = keys[1];
				for (int j = 0; j + 2 < keys.Length; j += 2)
				{
					if (num >= keys[j] && num <= keys[j + 2])
					{
						float num3 = ((keys[j + 2] > keys[j]) ? ((num - keys[j]) / (keys[j + 2] - keys[j])) : 1f);
						num3 = num3 * num3 * (3f - 2f * num3);
						num2 = keys[j + 1] + (keys[j + 3] - keys[j + 1]) * num3;
						break;
					}
					if (num > keys[j + 2])
					{
						num2 = keys[j + 3];
					}
				}
				table[i] = num2;
			}
		}
	}

	internal TransitionKind Kind;

	internal float Seconds = 0.6f;

	internal float[][] Weight = new float[4][];

	internal float[][] Reach = new float[4][];

	internal float[][][] Arc = new float[4][][];

	internal float[] BodyWeight = Array.Empty<float>();

	internal float[] BodyUp = Array.Empty<float>();

	internal float[] BodyToward = Array.Empty<float>();

	internal float[] Lean = Array.Empty<float>();

	internal int Samples => BodyWeight.Length;

	internal bool Valid
	{
		get
		{
			int num = BodyWeight.Length;
			if (num < 2 || num > 1024 || BodyUp.Length != num || BodyToward.Length != num || Lean.Length != num || !(Seconds > 0.05f) || !(Seconds < 5f))
			{
				return false;
			}
			if (Weight.Length != 4 || Reach.Length != 4 || Arc.Length != 4)
			{
				return false;
			}
			for (int i = 0; i < 4; i++)
			{
				if (Weight[i] == null || Reach[i] == null || Arc[i] == null || Weight[i].Length != num || Reach[i].Length != num || Arc[i].Length != num)
				{
					return false;
				}
				for (int j = 0; j < num; j++)
				{
					if (!Unit(Weight[i][j]) || !Unit(Reach[i][j]) || Arc[i][j] == null || Arc[i][j].Length != 3)
					{
						return false;
					}
					float[] array = Arc[i][j];
					for (int k = 0; k < array.Length; k++)
					{
						if (!Small(array[k]))
						{
							return false;
						}
					}
				}
			}
			for (int l = 0; l < num; l++)
			{
				if (!Unit(BodyWeight[l]) || !Small(BodyUp[l]) || !Small(BodyToward[l]) || !(Math.Abs(Lean[l]) <= 90f))
				{
					return false;
				}
			}
			return true;
		}
	}

	internal float WeightAt(int limb, float progress)
	{
		return ClimbLoop.Sample(Weight[limb], progress);
	}

	internal float ReachAt(int limb, float progress)
	{
		return ClimbLoop.Sample(Reach[limb], progress);
	}

	internal float ArcAt(int limb, float progress, int axis)
	{
		return ClimbLoop.Sample(Arc[limb], progress, axis);
	}

	internal float BodyWeightAt(float progress)
	{
		return ClimbLoop.Sample(BodyWeight, progress);
	}

	internal float BodyUpAt(float progress)
	{
		return ClimbLoop.Sample(BodyUp, progress);
	}

	internal float BodyTowardAt(float progress)
	{
		return ClimbLoop.Sample(BodyToward, progress);
	}

	internal float LeanAt(float progress)
	{
		return ClimbLoop.Sample(Lean, progress);
	}

	private static bool Unit(float v)
	{
		if (!float.IsNaN(v) && v >= -0.001f)
		{
			return v <= 1.001f;
		}
		return false;
	}

	private static bool Small(float v)
	{
		if (!float.IsNaN(v) && !float.IsInfinity(v))
		{
			return Math.Abs(v) <= 2f;
		}
		return false;
	}
}
