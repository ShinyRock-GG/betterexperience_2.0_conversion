using System;
using System.Collections.Generic;
using System.Linq;
using BetterExperience.Utils;
using BetterExperience.Wrappers.Pools;
using UnityEngine;

namespace BetterExperience.Features.AlternativeGenetics.Pooling;

internal class RollingMeanPooling : PoolingStrategy
{
	private GenePool pool;

	private RollingMeanConfiguration settings;

	private VersionCachedValue<GeneSet> activeAverage;

	private VersionCachedValue<float[]> activeGradient;

	private PredictableRandom Random => pool.Random;

	private Logger logger => pool.Logger;

	private GenePoolData Data => pool.Data;

	public RollingMeanPooling(GenePool pool, RollingMeanConfiguration settings)
	{
		this.pool = pool;
		this.settings = settings;
		activeAverage = pool.EpochCachedValue(CreateAverage);
		activeGradient = pool.IterationCachedValue(() => CreateGradient(activeAverage.Value.Vector));
	}

	private GeneSet CreateAverage()
	{
		List<GeneSet> generation = pool.GetGeneration(GeneGeneration.Old);
		if (generation.Count <= 1)
		{
			return generation[0];
		}
		return CreateWeightedAverageSet(generation);
	}

	public void Populate()
	{
		List<GeneSet> generation = pool.GetGeneration(GeneGeneration.Mature);
		List<GeneSet> generation2 = pool.GetGeneration(GeneGeneration.Old);
		Data.DiversitySimilarityThreshold = 0f;
		MoveYoungToMature();
		if (generation.Count >= pool.Data.InitialCapacity)
		{
			List<GeneSet> generation3 = pool.GetGeneration(GeneGeneration.Buffer);
			MoveMatureToBuffer();
			GeneSet geneSet = CreateWeightedAverageSet(generation3, useRatingAsWeight: true);
			GeneSet geneSet2 = CreateWeightedAverageSet(generation2);
			geneSet.Vectors.Remove(GeneVector.Rating);
			generation2.Add(geneSet);
			while (generation2.Count > settings.Capacity)
			{
				GeneSet value = CreateWeightedAverageSet(generation2.GetRange(0, 2));
				generation2.RemoveAt(0);
				generation2[0] = value;
			}
			float num = Mathf.InverseLerp(0.1f, 0f, CreateAbsGradient(geneSet2.Vector).Select(Mathf.Abs).Max());
			pool.Data.Error = num * 10f;
			GenerateMovingWeightedStdDev(geneSet2, generation3, geneSet.StdDev);
			generation.Clear();
			pool.Data.Epoch++;
		}
		Data.DiversityPenalty = pool.Data.InitialCapacity - generation.Count;
	}

	private void MoveMatureToBuffer()
	{
		List<GeneSet> generation = pool.GetGeneration(GeneGeneration.Buffer);
		if (generation.Count > pool.Capacity)
		{
			List<GeneSet> list = (from gs in generation
				select (gs: gs, iter: gs.Iteration, score: gs.Rating.Average()) into x
				orderby x.score, x.iter
				select x.gs).ToList();
			while (generation.Count > pool.Capacity)
			{
				GeneSet item = list[0];
				list.RemoveAt(0);
				generation.Remove(item);
			}
		}
		List<GeneSet> generation2 = pool.GetGeneration(GeneGeneration.Mature);
		generation.AddRange(generation2);
		generation2.Clear();
	}

	private void MoveYoungToMature()
	{
		List<GeneSet> generation = pool.GetGeneration(GeneGeneration.Young);
		List<GeneSet> generation2 = pool.GetGeneration(GeneGeneration.Mature);
		List<GeneSet> generation3 = pool.GetGeneration(GeneGeneration.Old);
		float[] vector = CreateWeightedAverageSet(generation3).Vector;
		foreach (GeneSet item in generation)
		{
			if (!Data.Survivors.Contains(item.Id) || item.Rating == null)
			{
				continue;
			}
			for (int i = 0; i < item.Vector.Length; i++)
			{
				if (item.Rating[i] == 0f && vector.Length > i)
				{
					item.Vector[i] = vector[i];
				}
			}
			generation2.Add(item);
		}
		generation.Clear();
	}

	private void GenerateMovingWeightedStdDev(GeneSet average, List<GeneSet> samples, float[] stdDev)
	{
		for (int i = 0; i < stdDev.Length; i++)
		{
			float num = 0f;
			float num2 = 0f;
			foreach (GeneSet sample in samples)
			{
				float num3 = average.Vector[i] - sample.Vector[i];
				float num4 = sample.Rating[i];
				num += num4 * num3 * num3;
				num2 += num4;
			}
			int count = samples.Count;
			float num5 = num;
			float num6 = (float)(count - 1) * num2 / (float)count;
			stdDev[i] = Mathf.Sqrt(num5 / num6);
		}
	}

	private GeneSet CreateWeightedAverageSet(List<GeneSet> subset, bool useRatingAsWeight = false)
	{
		float[] array = new float[pool.Data.GeneOrder.Count];
		float[] array2 = new float[pool.Data.GeneOrder.Count];
		array.Fill(float.NaN);
		array2.Fill(float.NaN);
		for (int i = 0; i < array.Length; i++)
		{
			float num = 0f;
			float num2 = 0f;
			int num3 = 0;
			int num4 = 0;
			foreach (GeneSet item in subset)
			{
				if (item.Vector.Length > i && !float.IsNaN(item.Vector[i]))
				{
					float num5 = ((!useRatingAsWeight) ? (1f + (float)num4 * settings.GenerationWeight) : ((item.Rating != null) ? item.Rating[i] : 0.1f));
					num += item.Vector[i] * num5;
					num2 += num5;
					num3++;
				}
				num4++;
			}
			if (num > 0f)
			{
				array[i] = num / num2;
				array2[i] = num2 / (float)num3;
			}
		}
		GeneSet geneSet = new GeneSet();
		geneSet.Iteration = pool.Data.Iteration;
		for (int j = 0; j < array.Length; j++)
		{
			pool.SetGeneValue(geneSet, pool.Data.GeneOrder[j], array[j], nosymmetry: false, guidance: true, settings.Step);
		}
		geneSet.Vectors[GeneVector.Data] = array;
		geneSet.Vectors[GeneVector.Rating] = array2;
		float[] array3 = new float[pool.Data.GeneOrder.Count];
		for (int k = 0; k < array3.Length; k++)
		{
			float num6 = 0f;
			float num7 = 0f;
			int num8 = 0;
			int num9 = 0;
			foreach (GeneSet item2 in subset)
			{
				if (!float.IsNaN(item2.Vector[k]) && !float.IsNaN(array[k]))
				{
					float num10 = ((!useRatingAsWeight) ? (1f + (float)num9 * settings.GenerationWeight) : ((item2.Rating != null) ? item2.Rating[k] : 0.1f));
					float num11 = array[k] - item2.Vector[k];
					num6 += num10 * num11 * num11;
					num7 += num10;
					num8++;
				}
				num9++;
			}
			switch (num8)
			{
			case 0:
				array3[k] = float.NaN;
				continue;
			case 1:
				array3[k] = Mathf.Sqrt(num6 / num7);
				continue;
			}
			float num12 = num6;
			float num13 = (float)(num8 - 1) * num7 / (float)num8;
			array3[k] = Mathf.Sqrt(num12 / num13);
		}
		geneSet.Vectors[GeneVector.StdDev] = array3;
		return geneSet;
	}

	private float[] CreateGradient(float[] averageSample)
	{
		return CreateAbsGradient(averageSample).InplaceMap((float x) => (settings.GradLockThreshold > 0f && Mathf.Abs(x) >= settings.GradLockThreshold) ? Math.Sign(x) : 0);
	}

	private float[] CreateAbsGradient(float[] averageSample)
	{
		List<GeneSet> generation = pool.GetGeneration(GeneGeneration.Old);
		float[] array = new float[Data.GeneOrder.Count];
		if (generation.Count > 1)
		{
			GeneSet geneSet = generation[generation.Count - 1];
			for (int i = 0; i < Data.GeneOrder.Count; i++)
			{
				float num = geneSet.Vector[i] - averageSample[i];
				if (!float.IsNaN(num))
				{
					array[i] = num;
				}
			}
		}
		return array;
	}

	public GenerationInfo ProduceRandomGeneSet(GeneSet gs, int attempt)
	{
		float num = 1f + (float)(attempt / settings.StepsToUpdateWeight) * settings.WeightUpdateScale;
		float[] array = ((!pool.Data.Settings.UseMomentum) ? new float[pool.Data.GeneOrder.Count].Fill(float.NaN) : pool.FineTuning.CreateMomentumLock(pool.Data.GeneOrder, null));
		GeneSet value = activeAverage.Value;
		float[] vector = value.Vector;
		float[] value2 = activeGradient.Value;
		List<GeneSet> generation = pool.GetGeneration(GeneGeneration.Old);
		if (generation.Count > 0 && (Data.Iteration - generation[generation.Count - 1].Iteration) % 2 == 0)
		{
			value2.Fill(0f);
		}
		for (int i = 0; i < gs.Vector.Length; i++)
		{
			string geneId = pool.Data.GeneOrder[i];
			float num2 = Mathf.Min(vector[i], 1f - vector[i]);
			float num3 = ((value2[i] == 0f) ? settings.SigmaStdDevWeight : 0f);
			float num4 = Mathf.Max(1E-05f, num3 + settings.SigmaDeltaWeight);
			float b = (value.StdDev[i] * num3 + num2 * settings.DeltaToStdDev * settings.SigmaDeltaWeight) / num4;
			float num5 = float.NaN;
			if (settings.CollapseThreshold > 0f && num2 < settings.CollapseThreshold)
			{
				if (pool.Random.NextFloat(1f) <= settings.CollapseRetainProb)
				{
					num5 = ((!(vector[i] < 0.5f)) ? (vector[i] + num2 / 2f) : (vector[i] - num2 / 2f));
					if (value2[i] != 0f && Mathf.Sign(num5 - 0.5f) != value2[i])
					{
						value2[i] = 0f;
					}
				}
				else
				{
					num5 = ((!(vector[i] < 0.5f)) ? (1f - settings.CollapseThreshold) : settings.CollapseThreshold);
					num2 = settings.CollapseThreshold;
					b = num2;
				}
			}
			if (settings.Step > 0f)
			{
				num2 = Mathf.Max(settings.Step, num2, 0.05f);
			}
			b = Mathf.Min(num2, b);
			float num6 = vector[i];
			float num7 = b * settings.SigmaScale * num;
			float num8 = ((!float.IsNaN(num5)) ? (Random.NextNormalStdDev(num5, num7 * settings.CollapsedSigmaScale) - num6) : (Random.NextNormalStdDev(num6, num7) - num6));
			if (!float.IsNaN(array[i]))
			{
				num8 = Mathf.Abs(num8) * array[i];
			}
			if (settings.BiasFactor > 0f && num2 <= settings.BiasFactor && Mathf.Sign(vector[i] - 0.5f) != Mathf.Sign(num8))
			{
				num8 *= settings.BiasFactor;
			}
			num6 += num8;
			if (value2[i] != 0f)
			{
				if (Mathf.Sign(value2[i]) != Mathf.Sign(num6 - vector[i]))
				{
					num6 = vector[i] + value2[i] * Mathf.Abs(num6 - vector[i]) * settings.GradientScale;
				}
				else if (settings.Step > 0f)
				{
					num6 = vector[i] + value2[i] * settings.Step;
				}
			}
			pool.SetGeneValue(gs, geneId, num6, nosymmetry: false, guidance: true, settings.Step);
		}
		pool.FirePostProcessGeneSet(gs);
		pool.CompleteGeneSet(gs);
		return new GenerationInfo();
	}

	private void InitializePool()
	{
		GeneSet geneSet = pool.CreateGeneSet();
		geneSet.InitVector(GeneVector.Data, 0.5f);
		geneSet.InitVector(GeneVector.StdDev, settings.InitializerStdDev);
		if (settings.Initializer == VectorInitializer.eve || settings.Initializer == VectorInitializer.center_eve)
		{
			Dictionary<GeneId, GeneInfoEx> eve = pool.GeneFactory.Eve;
			GeneSet geneSet2 = pool.ExtractGeneSet(eve.Values, norate: true);
			for (int i = 0; i < geneSet.Vector.Length; i++)
			{
				if (settings.Initializer == VectorInitializer.eve)
				{
					geneSet.Vector[i] = geneSet2.Vector[i];
				}
				else
				{
					geneSet.Vector[i] = (0.5f + geneSet2.Vector[i]) / 2f;
				}
			}
		}
		pool.GetGeneration(GeneGeneration.Old).Add(geneSet);
	}

	public GeneSet[] GetSpecialDumpVectors()
	{
		GeneSet geneSet = CreateWeightedAverageSet(pool.GetGeneration(GeneGeneration.Old));
		geneSet.Id = "wmean";
		GeneSet geneSet2 = pool.CreateGeneSet();
		geneSet2.Vectors[GeneVector.Data] = CreateGradient(geneSet.Vector);
		geneSet2.Id = "grad";
		return new GeneSet[2] { geneSet, geneSet2 };
	}

	public void Initialize()
	{
		if (pool.GetGeneration(GeneGeneration.Old).Count == 0)
		{
			InitializePool();
		}
	}
}
