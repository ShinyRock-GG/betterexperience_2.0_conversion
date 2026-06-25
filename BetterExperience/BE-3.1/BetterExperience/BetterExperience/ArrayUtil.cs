using System;
using System.Collections.Generic;

namespace BetterExperience;

internal class ArrayUtil
{
	public static float[] TransformVectors(float[] a, float[] b, Func<float, float, float> transform)
	{
		float[] array = new float[Math.Min(a.Length, b.Length)];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = transform(a[i], b[i]);
		}
		return array;
	}

	public static int[] SelectIndex(float[] diff, Func<float, bool> predicate)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < diff.Length; i++)
		{
			if (predicate(diff[i]))
			{
				list.Add(i);
			}
		}
		return list.ToArray();
	}
}
