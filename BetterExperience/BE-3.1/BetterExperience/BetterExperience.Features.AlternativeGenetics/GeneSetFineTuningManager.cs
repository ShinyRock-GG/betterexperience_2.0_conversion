using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BetterExperience.Utils;
using BetterExperience.Wrappers.Pools;
using UnityEngine;

namespace BetterExperience.Features.AlternativeGenetics;

internal class GeneSetFineTuningManager
{
	private GenePool pool;

	private Dictionary<Regex, GeneFineTuning> patterns = new Dictionary<Regex, GeneFineTuning>();

	private Dictionary<string, GeneFineTuning> groupTunings = new Dictionary<string, GeneFineTuning>();

	private Dictionary<string, GeneFineTuning> tunings = new Dictionary<string, GeneFineTuning>();

	private ISet<string> checkedSet = new HashSet<string>();

	private bool fastGuidance = true;

	private Dictionary<string, string> groupToGGmap = new Dictionary<string, string>();

	public int GuidedCount { get; private set; }

	public int TuningCount => tunings.Count;

	internal float ApplyGeneDeviationLimit(string geneId, float value)
	{
		if (tunings.TryGetValue(geneId, out var value2) && value2.Guidance.HasValue)
		{
			return 1f;
		}
		return value;
	}

	public GeneSetFineTuningManager(GenePool pool)
	{
		this.pool = pool;
		foreach (KeyValuePair<string, GeneFineTuning> item in pool.Data.Settings.FineTuning)
		{
			if (item.Key.StartsWith("@"))
			{
				groupTunings[item.Key.Substring(1)] = item.Value;
				continue;
			}
			Regex key = new Regex(item.Key.Split(new char[1] { '*' }).InplaceMap(Regex.Escape).Join(".*"));
			patterns[key] = item.Value;
		}
		pool.Data.GeneOrder.ForEach(RegisterNewGene);
	}

	internal void RegisterNewGene(string geneId)
	{
		if (!checkedSet.Add(geneId))
		{
			return;
		}
		List<GeneFineTuning> list = new List<GeneFineTuning>();
		foreach (KeyValuePair<Regex, GeneFineTuning> pattern in patterns)
		{
			if (pattern.Key.IsMatch(geneId))
			{
				list.Add(pattern.Value);
			}
		}
		string key = pool.GeneFactory.GeneToGroup[new GeneId(geneId).Item1];
		GeneFineTuning valueOrDefault = groupTunings.GetValueOrDefault(key, () => (GeneFineTuning)null);
		if (valueOrDefault != null)
		{
			list.Add(valueOrDefault);
		}
		if (list.Count != 0)
		{
			if (list.Count > 1)
			{
				throw new Exception("Gene " + geneId + " matches multiple tuning groups");
			}
			tunings.Add(geneId, list[0]);
			if (HasGuidance(geneId))
			{
				GuidedCount++;
			}
		}
	}

	private T? ReadTuning<T>(string geneId, Func<GeneFineTuning, T?> extractor) where T : struct
	{
		if (tunings.TryGetValue(geneId, out var value))
		{
			return extractor(value);
		}
		return null;
	}

	private T ReadTuning<T>(string geneId, Func<GeneFineTuning, T> extractor) where T : class
	{
		if (tunings.TryGetValue(geneId, out var value))
		{
			return extractor(value);
		}
		return null;
	}

	public float ApplyValueLimits(string geneId, float current)
	{
		if (pool.Data.GuidanceDisabled)
		{
			return current;
		}
		(float, float)? tuple = ReadTuning<(float, float)>(geneId, (Func<GeneFineTuning, (float, float)?>)((GeneFineTuning tuning) => tuning.MinMax));
		if (tuple.HasValue)
		{
			return Mathf.Clamp(current, tuple.Value.Item1, tuple.Value.Item2);
		}
		return current;
	}

	public float ApplyValueGuidance(string geneId, float current, float proposed)
	{
		if (float.IsNaN(current))
		{
			return proposed;
		}
		if (float.IsNaN(proposed))
		{
			return current;
		}
		if (pool.Data.GuidanceDisabled)
		{
			return proposed;
		}
		float num = ReadTuning(geneId, (GeneFineTuning t) => t.Guidance) ?? float.NaN;
		if (float.IsNaN(num))
		{
			return proposed;
		}
		if (fastGuidance)
		{
			return (current + num) / 2f;
		}
		if (Mathf.Abs(num - current) < Mathf.Abs(num - proposed))
		{
			return current;
		}
		return proposed;
	}

	public bool HasGuidance(string gene)
	{
		if (pool.Data.GuidanceDisabled)
		{
			return false;
		}
		if (ReadTuning(gene, (GeneFineTuning t) => t.Guidance).HasValue)
		{
			return true;
		}
		if (ReadTuning<(float, float)>(gene, (Func<GeneFineTuning, (float, float)?>)((GeneFineTuning t) => t.MinMax)).HasValue)
		{
			return true;
		}
		return false;
	}

	internal float GetDeviationFactorTuning(string strid, float v)
	{
		return ReadTuning(strid, (GeneFineTuning t) => t.DeviationFactor) ?? v;
	}

	internal float GetEvolutionFactorTuning(string strid, float v)
	{
		return ReadTuning(strid, (GeneFineTuning t) => t.EvolutionFactor) ?? v;
	}

	public float GetSimilarityWeight(string geneId)
	{
		return ReadTuning(geneId, (GeneFineTuning t) => t.SimilarityWeight) ?? 1f;
	}

	public float GetInitialValue(string geneId, float value)
	{
		return ReadTuning(geneId, (GeneFineTuning t) => t.InitialValue) ?? value;
	}

	public IEnumerable<string> GetSymmetricGenes(string gene)
	{
		string text = null;
		if (gene.Contains("_L#"))
		{
			text = gene.Replace("_L#", "_R#");
		}
		else if (gene.Contains("_R#"))
		{
			text = gene.Replace("_R#", "_L#");
		}
		if (text == null)
		{
			return Enumerable.Empty<string>();
		}
		return Enumerable.Repeat(text, 1);
	}

	internal float[] CreateMomentumLock(List<string> geneOrder, float[] gradient)
	{
		Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
		foreach (string item in geneOrder)
		{
			string text = ReadTuning(item, (GeneFineTuning t) => t.MomentumGroup);
			if (text != null)
			{
				dictionary.GetValueOrAdd(text, () => new List<string>()).Add(item);
			}
		}
		float[] array = new float[geneOrder.Count];
		for (int num = 0; num < geneOrder.Count; num++)
		{
			array[num] = float.NaN;
		}
		if (dictionary.Count != 0)
		{
			foreach (KeyValuePair<string, List<string>> item2 in dictionary)
			{
				float num2 = (pool.Random.NextBool() ? (-1f) : 1f);
				if (gradient != null)
				{
					num2 = 0f;
					foreach (string item3 in item2.Value)
					{
						float num3 = gradient[geneOrder.IndexOf(item3)];
						if (Mathf.Abs(num3) > Mathf.Abs(num2))
						{
							num2 = num3;
						}
					}
					num2 = Math.Sign(num2);
				}
				foreach (string item4 in item2.Value)
				{
					array[geneOrder.IndexOf(item4)] = num2;
				}
			}
		}
		return array;
	}
}
