using System;
using System.Collections.Generic;
using System.Linq;
using BetterExperience.Utils;
using UnityEngine;

namespace BetterExperience.Features.AlternativeGenetics.Pooling;

internal class SelectionPooling : AbstractPoolingStrategy<SelectionConfiguration>
{
	private class SelectionOpportunities
	{
		public List<(GeneSet, GeneSet, float[])> GuidedEvolution { get; set; } = new List<(GeneSet, GeneSet, float[])>();

		public List<(GeneSet, GeneSet)> Crossover { get; set; } = new List<(GeneSet, GeneSet)>();
	}

	private VersionCachedValue<GeneSet> averageSet;

	private VersionCachedValue<SelectionOpportunities> selectionOps;

	private GeneSet Initializer { get; set; }

	public SelectionPooling(GenePool pool, SelectionConfiguration settings)
		: base(pool, settings)
	{
		SelectionPooling selectionPooling = this;
		averageSet = pool.EpochCachedValue(() => selectionPooling.CreateWeightedAverageSet(pool.GetGeneration(GeneGeneration.Old)));
		selectionOps = pool.EpochCachedValue(CreateOpportunities);
	}

	public override GeneSet[] GetSpecialDumpVectors()
	{
		GeneSet geneSet = base.Pool.CopyGeneSet(averageSet.Value);
		geneSet.Id = "wmean";
		List<GeneSet> list = new List<GeneSet>();
		list.Add(geneSet);
		int num = 0;
		foreach (var item2 in selectionOps.Value.GuidedEvolution)
		{
			float[] item = item2.Item3;
			GeneSet geneSet2 = base.Pool.CreateGeneSet();
			item.CopyTo(geneSet2.Vector, 0);
			geneSet2.Id = "grad_" + num++;
			list.Add(geneSet2);
		}
		return list.ToArray();
	}

	public override void Initialize()
	{
		Dictionary<BetterExperience.Wrappers.Pools.GeneId, BetterExperience.Wrappers.Pools.GeneInfoEx>.ValueCollection values = base.Pool.GeneFactory.Eve.Values;
		if (base.Settings.Initializer == VectorInitializer.eve || base.Settings.Initializer == VectorInitializer.center_eve)
		{
			Initializer = base.Pool.ExtractGeneSet(values, norate: true);
			if (base.Settings.Initializer == VectorInitializer.center_eve)
			{
				Initializer.Vector.InplaceMap((float x) => (0.5f + x) / 2f);
			}
		}
		else
		{
			Initializer = base.Pool.CreateGeneSet();
			Initializer.InitVector(GeneVector.Data, 0.5f);
		}
		for (int num = 0; num < base.Pool.Data.GeneOrder.Count; num++)
		{
			Initializer.Vector[num] = base.Pool.FineTuning.GetInitialValue(base.Pool.Data.GeneOrder[num], Initializer.Vector[num]);
		}
		Initializer.InitVector(GeneVector.Rating, 0.1f);
	}

	public override void Populate()
	{
		MoveYoungToMature();
		MoveMatureToOld();
		base.Data.DiversityPenalty = 10 - base.Pool.GetGeneration(GeneGeneration.Mature).Count;
	}

	private void MoveMatureToOld()
	{
		List<GeneSet> generation = base.Pool.GetGeneration(GeneGeneration.Mature);
		List<GeneSet> generation2 = base.Pool.GetGeneration(GeneGeneration.Old);
		bool flag = false;
		if (generation.Count >= base.Pool.Capacity)
		{
			generation2.AddRange(generation);
			generation.Clear();
			flag = true;
		}
		if (flag)
		{
			CompactOld();
			base.Data.Epoch++;
		}
	}

	private void CompactOld()
	{
		List<GeneSet> generation = base.Pool.GetGeneration(GeneGeneration.Old);
		GeneSet[] collection;
		if (base.Settings.FixedSize == 0)
		{
			collection = (from x in generation.Select(delegate(GeneSet x)
				{
					float num2 = x.Rating.Average();
					return (score: (float)x.Epoch + num2 * base.Settings.RateToLifetimeFactor, gs: x);
				})
				where x.score >= (float)base.Pool.Data.Epoch
				select x.gs).ToArray();
		}
		else
		{
			base.Pool.Logger.Info("Generating fixed set");
			List<GeneSet> tmp = (from x in generation
				select (score: x.Rating.Average(), gs: x) into x
				orderby (score: x.score, Epoch: x.gs.Epoch) descending
				select x.gs).ToList();
			List<GeneSet> list = new List<GeneSet>();
			for (int num = 0; num < base.Settings.FixedSize && num < tmp.Count; num++)
			{
				list.Add(tmp[num]);
			}
			base.Pool.Logger.Info("{0} survivors", tmp.Count);
			generation.Where((GeneSet x) => x.Epoch == base.Pool.Data.Epoch).ForEach(delegate(GeneSet x)
			{
				if (!tmp.Contains(x))
				{
					tmp.Add(x);
				}
			});
			collection = tmp.ToArray();
			base.Pool.Logger.Info("{0} final generation", tmp.Count);
		}
		generation.Clear();
		generation.AddRange(collection);
		if (generation.Count > 0)
		{
			base.Pool.Data.Error = generation.Select((GeneSet x) => x.Rating.Average()).Average() * 10f;
		}
		else
		{
			base.Pool.Data.Error = 0f;
		}
	}

	public override GenerationInfo ProduceRandomGeneSet(GeneSet gs, int step)
	{
		List<GeneSet> generation = base.Pool.GetGeneration(GeneGeneration.Old);
		SelectionTechinque selectionTechinque = SelectTechinque(generation);
		float weight = 1f + (float)(step / base.Settings.StepsToUpdateWeight) * base.Settings.WeightUpdateScale;
		float weight2 = (float)(step / base.Settings.StepsToUpdateWeight) * base.Settings.WeightUpdateScale;
		switch (selectionTechinque)
		{
		case SelectionTechinque.Random:
			if (base.Settings.Distribution == VectorDistribution.normal)
			{
				RandomInitializer(gs, weight);
			}
			else
			{
				RandomUniform(gs);
			}
			break;
		case SelectionTechinque.AverageRandom:
			RandomAverage(gs, weight);
			break;
		case SelectionTechinque.Morph:
			Morph(gs, weight);
			break;
		case SelectionTechinque.Crossover:
			Crossover(gs, weight2);
			break;
		case SelectionTechinque.MorphCrossover:
			Crossover(gs, weight);
			break;
		case SelectionTechinque.GuidedEvolution:
			GuidedEvolution(gs, weight);
			break;
		default:
			throw new NotImplementedException();
		}
		return new GenerationInfo
		{
			Text = "Technique " + selectionTechinque.ToString() + " weight " + weight
		};
	}

	private void Crossover(GeneSet target, float weight)
	{
		(GeneSet, GeneSet) tuple = base.Pool.Random.ListChoice(selectionOps.Value.Crossover);
		Crossover(target, tuple.Item1, tuple.Item2);
		Randomize(target, target, weight);
	}

	private void GuidedEvolution(GeneSet target, float weight)
	{
		(GeneSet, GeneSet, float[]) tuple = base.Pool.Random.ListChoice(selectionOps.Value.GuidedEvolution);
		Randomize(target, tuple.Item1, weight, tuple.Item3);
	}

	private void Crossover(GeneSet target, GeneSet a, GeneSet b)
	{
		int count = base.Pool.Data.GeneOrder.Count;
		int num = count / 4;
		int num2 = base.Pool.Random.NextInt(count / 2);
		int num3 = num + num2;
		target.InitVector(GeneVector.Rating, 0f);
		int[] array = Enumerable.Range(0, count).ToArray();
		array.InplaceShuffle(base.Pool.Random.NextInt);
		for (int i = 0; i < target.Vector.Length; i++)
		{
			int num4 = array[i];
			float value = ((num4 < num3) ? a.Vector[num4] : b.Vector[num4]);
			float num5 = ((num4 < num3) ? a.Rating[num4] : b.Rating[num4]);
			base.Pool.SetGeneValue(target, base.Pool.Data.GeneOrder[num4], value, nosymmetry: false, guidance: true, base.Settings.Step);
			target.Rating[num4] = num5;
		}
	}

	private void Morph(GeneSet gs, float weight)
	{
		Randomize(gs, base.Pool.Random.ListChoice(base.Pool.GetGeneration(GeneGeneration.Old)), weight);
	}

	private void RandomInitializer(GeneSet gs, float weight)
	{
		Randomize(gs, Initializer, weight);
	}

	private void RandomAverage(GeneSet gs, float weight)
	{
		Randomize(gs, averageSet.Value, weight);
	}

	private void Randomize(GeneSet target, GeneSet source, float weight, float[] direction = null)
	{
		float[] array = base.Pool.CreateMomentumLock(direction);
		for (int i = 0; i < target.Vector.Length; i++)
		{
			float deviationFactorTuning = base.Pool.FineTuning.GetDeviationFactorTuning(base.Pool.Data.GeneOrder[i], 1f);
			float t = source.Rating[i] - 0.1f;
			float num = Mathf.Lerp(base.Settings.Sigma, base.Settings.FragileSigma, t);
			float num2 = base.Pool.Random.NextNormalStdDev(source.Vector[i], num * weight * deviationFactorTuning);
			if (direction != null && direction[i] != 0f && !float.IsNaN(direction[i]))
			{
				float num3 = Mathf.Abs(num2 - source.Vector[i]) * (float)Math.Sign(direction[i]);
				num2 = source.Vector[i] + num3;
			}
			if (array[i] != 0f && !float.IsNaN(array[i]))
			{
				num2 = source.Vector[i] + Mathf.Abs(num2 - source.Vector[i]) * array[i];
			}
			base.Pool.SetGeneValue(target, base.Pool.Data.GeneOrder[i], num2, nosymmetry: false, guidance: true, base.Settings.Step);
		}
	}

	private void RandomUniform(GeneSet target)
	{
		for (int i = 0; i < target.Vector.Length; i++)
		{
			float value = base.Pool.Random.NextFloat(1f);
			base.Pool.SetGeneValue(target, base.Pool.Data.GeneOrder[i], value, nosymmetry: false, guidance: true, base.Settings.Step);
		}
	}

	private SelectionTechinque SelectTechinque(List<GeneSet> ogen)
	{
		if (ogen.Count > 1)
		{
			if (selectionOps.Value.Crossover.Count > 0 && selectionOps.Value.GuidedEvolution.Count > 0)
			{
				if (base.Settings.AlwaysUseMorph)
				{
					return base.Pool.Random.Choice<SelectionTechinque>(SelectionTechinque.Morph, SelectionTechinque.Crossover, SelectionTechinque.MorphCrossover, SelectionTechinque.GuidedEvolution);
				}
				return base.Pool.Random.Choice<SelectionTechinque>(SelectionTechinque.Crossover, SelectionTechinque.MorphCrossover, SelectionTechinque.GuidedEvolution);
			}
			if (selectionOps.Value.Crossover.Count > 0)
			{
				if (base.Settings.AlwaysUseMorph)
				{
					return base.Pool.Random.Choice<SelectionTechinque>(SelectionTechinque.Morph, SelectionTechinque.Crossover, SelectionTechinque.MorphCrossover, SelectionTechinque.AverageRandom);
				}
				return base.Pool.Random.Choice<SelectionTechinque>(SelectionTechinque.Crossover, SelectionTechinque.MorphCrossover, SelectionTechinque.AverageRandom);
			}
			if (selectionOps.Value.GuidedEvolution.Count > 0)
			{
				if (base.Settings.AlwaysUseMorph)
				{
					return base.Pool.Random.Choice<SelectionTechinque>(SelectionTechinque.Morph, SelectionTechinque.GuidedEvolution, SelectionTechinque.GuidedEvolution);
				}
				return base.Pool.Random.Choice<SelectionTechinque>(SelectionTechinque.GuidedEvolution);
			}
			return base.Pool.Random.Choice<SelectionTechinque>(SelectionTechinque.Morph, SelectionTechinque.AverageRandom);
		}
		if (ogen.Count == 1)
		{
			if (base.Settings.Distribution == VectorDistribution.normal)
			{
				return base.Pool.Random.Choice<SelectionTechinque>(SelectionTechinque.AverageRandom, SelectionTechinque.Morph);
			}
			return base.Pool.Random.Choice<SelectionTechinque>(SelectionTechinque.Random, SelectionTechinque.Morph);
		}
		return SelectionTechinque.Random;
	}

	private SelectionOpportunities CreateOpportunities()
	{
		SelectionOpportunities selectionOpportunities = new SelectionOpportunities();
		List<(float, GeneSet)> rateAndGene = (from x in base.Pool.GetGeneration(GeneGeneration.Old)
			select (rating: x.Rating.Average(), gs: x)).ToList();
		if (base.Settings.CumulativeGradient)
		{
			selectionOpportunities.GuidedEvolution = CreateCumulativeGradient(rateAndGene);
		}
		else
		{
			selectionOpportunities.GuidedEvolution = CreateIndividualGradient(rateAndGene);
		}
		selectionOpportunities.Crossover = CreateCrossoverOpportunities(rateAndGene);
		return selectionOpportunities;
	}

	private List<(GeneSet, GeneSet)> CreateCrossoverOpportunities(List<(float rating, GeneSet gs)> rateAndGene)
	{
		List<(GeneSet, GeneSet)> list = new List<(GeneSet, GeneSet)>();
		for (int i = 0; i < rateAndGene.Count; i++)
		{
			(float, GeneSet) tuple = rateAndGene[i];
			float item = tuple.Item1;
			for (int j = i + 1; j < rateAndGene.Count; j++)
			{
				(float, GeneSet) tuple2 = rateAndGene[j];
				var (num, _) = tuple2;
				if (item >= base.Settings.CrossoverThreshold && num >= base.Settings.CrossoverThreshold)
				{
					list.Add((tuple.Item2, tuple2.Item2));
				}
			}
		}
		return list;
	}

	private List<(GeneSet, GeneSet, float[])> CreateIndividualGradient(List<(float rating, GeneSet gs)> rateAndGene)
	{
		List<(GeneSet, GeneSet, float[])> list = new List<(GeneSet, GeneSet, float[])>();
		foreach (var item2 in rateAndGene)
		{
			float[] averageGradient = averageSet.Value.Vector.Subtract(item2.gs.Vector).InplaceMap((int i, float x) => (!TestPerception(x, averageSet.Value.Vector[i])) ? 0f : x);
			var (num, _) = item2;
			foreach (var gs2 in rateAndGene)
			{
				float item = gs2.rating;
				if (!(item > num) || base.Settings.CumulativeGradient)
				{
					continue;
				}
				float[] array = gs2.gs.Vector.Subtract(item2.gs.Vector);
				array.InplaceMap((int index, float x) => (Math.Sign(x) != Math.Sign(averageGradient[index])) ? ((averageGradient[index] + x) / 2f) : averageGradient[index]);
				array.InplaceMap((int i, float x) => (!TestPerception(x, gs2.gs.Vector[i])) ? 0f : x);
				if (array.Where((float x) => x != 0f).Count() > 0)
				{
					list.Add((item2.gs, gs2.gs, array));
					if (item < 1f)
					{
						list.Add((gs2.gs, item2.gs, array));
					}
				}
			}
		}
		FixGradientEstimation(list);
		return FilterGradientsByThreshold(list);
	}

	private List<(GeneSet, GeneSet, float[])> CreateCumulativeGradient(List<(float rating, GeneSet gs)> rateAndGene)
	{
		List<(GeneSet, GeneSet, float[])> list = new List<(GeneSet, GeneSet, float[])>();
		foreach (var item in rateAndGene)
		{
			(GeneSet, float[]) tuple = CreateCumulativeGradient(item, rateAndGene);
			if (tuple.Item2 != null)
			{
				list.Add((tuple.Item1, tuple.Item1, tuple.Item2));
			}
		}
		return list;
	}

	private (GeneSet, float[]) CreateCumulativeGradient((float rating, GeneSet gs) gsAndRating, List<(float rating, GeneSet gs)> ratedPool)
	{
		GeneSet gs = gsAndRating.gs;
		float rating = gsAndRating.rating;
		List<GeneSet> list = (from x in ratedPool
			where x.rating < rating
			select x.gs).ToList();
		List<GeneSet> list2 = (from x in ratedPool
			where x.rating > rating
			select x.gs).ToList();
		if (list.Count == 0 && list2.Count == 0)
		{
			return (gs, null);
		}
		if (list.Count == 0)
		{
			GeneSet betterset = CreateWeightedAverageSet(list2);
			float[] array = betterset.Vector.Subtract(gs.Vector);
			array.InplaceMap((int i, float x) => (!TestPerception(x, betterset.Vector[i])) ? 0f : x);
			return (gs, array);
		}
		if (list2.Count == 0)
		{
			GeneSet geneSet = CreateWeightedAverageSet(list);
			float[] array2 = gs.Vector.Subtract(geneSet.Vector);
			array2.InplaceMap((int i, float x) => (!TestPerception(x, gs.Vector[i])) ? 0f : x);
			return (gs, array2);
		}
		GeneSet betterset2 = CreateWeightedAverageSet(list2);
		GeneSet geneSet2 = CreateWeightedAverageSet(list);
		float[] array3 = betterset2.Vector.Subtract(gs.Vector);
		array3.InplaceMap((int i, float x) => (!TestPerception(x, betterset2.Vector[i])) ? 0f : x);
		float[] array4 = gs.Vector.Subtract(geneSet2.Vector);
		array4.InplaceMap((int i, float x) => (!TestPerception(x, gs.Vector[i])) ? 0f : x);
		float[] array5 = new float[array4.Length];
		for (int num = 0; num < array5.Length; num++)
		{
			float num2 = gs.Rating[num] - geneSet2.Rating[num];
			float num3 = betterset2.Rating[num];
			array5[num] = (array4[num] * num2 + array3[num] * num3) / (num2 + num3);
			if (float.IsNaN(array5[num]))
			{
				base.Pool.Logger.Error("NAN!! {0} {1} {2} {3}", array4[num], array3[num], num2, num3);
			}
		}
		return (gs, array5);
	}

	private List<(GeneSet, GeneSet, float[])> FilterGradientsByThreshold(List<(GeneSet, GeneSet, float[])> input)
	{
		List<(GeneSet, GeneSet, float[])> list = new List<(GeneSet, GeneSet, float[])>();
		foreach (var item3 in input)
		{
			bool flag = true;
			foreach (var item4 in list)
			{
				if (item4.Item1 == item3.Item1)
				{
					float[] item = item4.Item3;
					float[] item2 = item3.Item3;
					if (item.Subtract(item2).InplaceMap((float x) => (!TestPerception(x, 0.5f)) ? 0f : x).InplaceMap(Mathf.Abs)
						.Sum() == 0f)
					{
						flag = false;
						break;
					}
				}
			}
			if (flag)
			{
				list.Add(item3);
			}
		}
		return list;
	}

	private void FixGradientEstimation(List<(GeneSet, GeneSet, float[])> input)
	{
		Dictionary<GeneSet, float[]> dictionary = new Dictionary<GeneSet, float[]>();
		foreach (var item4 in input)
		{
			GeneSet item = item4.Item1;
			float[] gradient = item4.Item3;
			float[] valueOrAdd = dictionary.GetValueOrAdd(item, () => new float[gradient.Length]);
			for (int num = 0; num < gradient.Length; num++)
			{
				if (gradient[num] != 0f)
				{
					valueOrAdd[num] += 1f;
				}
			}
		}
		foreach (float[] value in dictionary.Values)
		{
			float avg = value.Average();
			value.InplaceMap((float x) => (x >= avg) ? 1 : 0);
		}
		foreach (var item5 in input)
		{
			GeneSet item2 = item5.Item1;
			float[] item3 = item5.Item3;
			float[] b = dictionary[item2];
			item3.IMul(b);
		}
	}

	private bool TestPerception(float x, float target)
	{
		if (!(Mathf.Abs(x) >= base.Settings.PerceptionThreshold))
		{
			return Mathf.Min(target, 1f - target) <= base.Settings.PerceptionThreshold;
		}
		return true;
	}

	protected void MoveYoungToMature()
	{
		List<GeneSet> generation = base.Pool.GetGeneration(GeneGeneration.Young);
		List<GeneSet> generation2 = base.Pool.GetGeneration(GeneGeneration.Mature);
		foreach (GeneSet item in generation)
		{
			if (base.Data.Survivors.Contains(item.Id))
			{
				generation2.Add(item);
			}
		}
		generation.Clear();
	}

	private GeneSet CreateWeightedAverageSet(List<GeneSet> subset)
	{
		float[] array = new float[base.Pool.Data.GeneOrder.Count];
		float[] array2 = new float[base.Pool.Data.GeneOrder.Count];
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
					float num5 = item.Rating[i];
					num += item.Vector[i] * num5;
					num2 += num5;
					num3++;
				}
				num4++;
			}
			if (num2 > 0f)
			{
				array[i] = num / num2;
				array2[i] = num2 / (float)num3;
			}
		}
		GeneSet geneSet = base.Pool.CreateGeneSet();
		for (int j = 0; j < array.Length; j++)
		{
			base.Pool.SetGeneValue(geneSet, base.Pool.Data.GeneOrder[j], array[j], nosymmetry: false, guidance: true, base.Settings.Step);
		}
		geneSet.Vectors[GeneVector.Data] = array;
		geneSet.Vectors[GeneVector.Rating] = array2;
		return geneSet;
	}
}
