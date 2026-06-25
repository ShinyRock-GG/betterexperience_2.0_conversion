using System;
using System.Collections.Generic;
using UnityEngine;

namespace BetterExperience.Utils;

internal class PredictableRandom
{
	private System.Random rnd = new System.Random();

	public int Seed { get; private set; }

	public PredictableRandom(int seed)
	{
		Seed = seed;
	}

	private System.Random NextRandom()
	{
		return rnd;
	}

	public float NextFloat(float max)
	{
		return (float)NextRandom().NextDouble() * max;
	}

	public int NextInt(int max)
	{
		return NextRandom().Next(max);
	}

	public bool NextBool()
	{
		return NextRandom().Next(2) == 0;
	}

	public T ListChoice<T>(IList<T> set)
	{
		return set[NextInt(set.Count)];
	}

	public T Choice<T>(params T[] set)
	{
		return set[NextInt(set.Length)];
	}

	public float NextNormalStdDev(float mean, float stddev)
	{
		return BoxMuller32(mean, stddev);
	}

	private double BoxMuller64(double mean, double stddev)
	{
		double d = 1.0 - NextRandom().NextDouble();
		double num = 1.0 - NextRandom().NextDouble();
		double num2 = Math.Sqrt(-2.0 * Math.Log(d)) * Math.Sin(Math.PI * 2.0 * num);
		return (float)(mean + stddev * num2);
	}

	private float BoxMuller32(float mean, float stddev)
	{
		float f = 1f - NextFloat(1f);
		float num = 1f - NextFloat(1f);
		float num2 = Mathf.Sqrt(-2f * Mathf.Log(f)) * Mathf.Sin(MathF.PI * 2f * num);
		return mean + stddev * num2;
	}

	public float NextNormalRange(float a, float b)
	{
		float mean = (a + b) / 2f;
		float stddev = Mathf.Abs(b - a) / 4f;
		return NextNormalStdDev(mean, stddev);
	}
}
