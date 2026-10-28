using System;

namespace CorewoodFortified.LadderAnimation;

internal sealed class ClimbLoop
{
	internal string Name = "climb_up";

	internal int StrideRungs = 2;

	internal float[] Start = new float[4] { 0.5f, 0f, 0.2f, 0.7f };

	internal float[] Length = new float[4] { 0.4f, 0.4f, 0.5f, 0.5f };

	internal float[] Swing = Smooth(33);

	internal float[][][] Arc = new float[4][][]
	{
		Arc3(33, 0f, 0.07f, 0.02f),
		Arc3(33, 0f, 0.07f, 0.02f),
		Arc3(33, 0.01f, 0.14f, 0.05f),
		Arc3(33, 0.01f, 0.14f, 0.05f)
	};

	internal float[][] Body = Dwell(61, 0.6f, new float[2] { 0.2f, 0.7f }, 0.5f, 0.3f);

	internal float[] Lean = Flat(61, 0f);

	internal bool Valid
	{
		get
		{
			if (StrideRungs < 1 || StrideRungs > 6 || !Four(Start) || !Four(Length) || !Table(Swing) || Arc == null || Arc.Length != 4 || Body == null || Body.Length < 2 || !Table(Lean))
			{
				return false;
			}
			for (int i = 0; i < 4; i++)
			{
				if (!Between(Start[i], 0f, 1f) || !Between(Length[i], 0.05f, 0.7f) || !Table3(Arc[i]))
				{
					return false;
				}
			}
			float[][] body = Body;
			foreach (float[] array in body)
			{
				if (array == null || array.Length != 2 || !Small(array[0]) || !Small(array[1]))
				{
					return false;
				}
			}
			return true;
		}
	}

	internal ClimbLoop Copy()
	{
		ClimbLoop climbLoop = (ClimbLoop)MemberwiseClone();
		climbLoop.Start = (float[])Start.Clone();
		climbLoop.Length = (float[])Length.Clone();
		climbLoop.Swing = (float[])Swing.Clone();
		climbLoop.Arc = new float[4][][];
		for (int i = 0; i < 4; i++)
		{
			climbLoop.Arc[i] = Clone2(Arc[i]);
		}
		climbLoop.Body = Clone2(Body);
		climbLoop.Lean = (float[])Lean.Clone();
		return climbLoop;
	}

	internal static float Sample(float[] table, float x)
	{
		float num = LimbGait.Clamp(x, 0f, 1f) * (float)(table.Length - 1);
		int num2 = Math.Min(table.Length - 2, (int)Math.Floor(num));
		return table[num2] + (table[num2 + 1] - table[num2]) * (num - (float)num2);
	}

	internal static float Sample(float[][] table, float x, int column)
	{
		float num = LimbGait.Clamp(x, 0f, 1f) * (float)(table.Length - 1);
		int num2 = Math.Min(table.Length - 2, (int)Math.Floor(num));
		return table[num2][column] + (table[num2 + 1][column] - table[num2][column]) * (num - (float)num2);
	}

	internal static float[][] Dwell(int samples, float amount, float[] footStarts, float footLength, float slowAt, float cycleMetres = 1f)
	{
		float[] array = new float[samples];
		float num = 1f / (float)(samples - 1);
		float num2 = 0f;
		float num3 = 0f;
		for (int i = 1; i < samples; i++)
		{
			float num4 = ((float)i - 0.5f) * num;
			float num5 = 0f;
			foreach (float num6 in footStarts)
			{
				float num7 = num4 - num6;
				num7 -= (float)Math.Floor(num7);
				if (num7 < footLength)
				{
					num5 -= (float)Math.Cos(Math.PI * 2.0 * (double)(num7 / footLength - slowAt));
				}
			}
			num2 = (array[i] = num2 + num5 * amount * cycleMetres * num);
			num3 += num2;
		}
		float num8 = num3 / (float)(samples - 1);
		float[][] array2 = new float[samples][];
		for (int k = 0; k < samples; k++)
		{
			array2[k] = new float[2]
			{
				array[k] - num8,
				0f
			};
		}
		array2[0][0] = array2[samples - 1][0];
		return array2;
	}

	internal static float[] Smooth(int count)
	{
		float[] array = new float[count];
		for (int i = 0; i < count; i++)
		{
			float num = (float)i / (float)(count - 1);
			array[i] = num * num * (3f - 2f * num);
		}
		return array;
	}

	internal static float[][] Arc3(int count, float lat, float outward, float up)
	{
		float[][] array = new float[count][];
		for (int i = 0; i < count; i++)
		{
			float num = (float)Math.Sin(Math.PI * (double)i / (double)(count - 1));
			array[i] = new float[3]
			{
				lat * num * num,
				outward * num * num,
				up * num * num
			};
		}
		return array;
	}

	internal static float[] Flat(int count, float value)
	{
		float[] array = new float[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = value;
		}
		return array;
	}

	private static float[][] Clone2(float[][] table)
	{
		float[][] array = new float[table.Length][];
		for (int i = 0; i < table.Length; i++)
		{
			array[i] = (float[])table[i].Clone();
		}
		return array;
	}

	private static bool Four(float[] values)
	{
		if (values != null)
		{
			return values.Length == 4;
		}
		return false;
	}

	private static bool Small(float value)
	{
		if (!float.IsNaN(value) && !float.IsInfinity(value))
		{
			return Math.Abs(value) <= 2f;
		}
		return false;
	}

	private static bool Table(float[] table)
	{
		if (table == null || table.Length < 2 || table.Length > 1024)
		{
			return false;
		}
		foreach (float value in table)
		{
			if (!Small(value) && !(Math.Abs(value) <= 90f))
			{
				return false;
			}
		}
		return true;
	}

	private static bool Table3(float[][] table)
	{
		if (table == null || table.Length < 2 || table.Length > 1024)
		{
			return false;
		}
		foreach (float[] array in table)
		{
			if (array == null || array.Length != 3 || !Small(array[0]) || !Small(array[1]) || !Small(array[2]))
			{
				return false;
			}
		}
		return true;
	}

	private static bool Between(float value, float min, float max)
	{
		if (!float.IsNaN(value) && value >= min)
		{
			return value <= max;
		}
		return false;
	}
}
