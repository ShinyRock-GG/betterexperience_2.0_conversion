using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BetterExperience.Features.AlternativeGenetics.Pooling;
using BetterExperience.Utils;
using BetterExperience.Wrappers.Pools;

namespace BetterExperience.Features.AlternativeGenetics;

internal class PoolingGroup
{
	public class GeneSetGroup
	{
		public int Step { get; set; }

		public int MaxSteps { get; set; }

		public float Score { get; set; }

		public Dictionary<string, GeneSet> Samples { get; internal set; }

		public Dictionary<string, GenerationInfo> Infos { get; internal set; }
	}

	private MultithreadingFeature parallel;

	private Task<GeneSetGroup> aotGroup;

	public Dictionary<string, GenePool> Pools { get; private set; } = new Dictionary<string, GenePool>();

	public PoolingGroupData Data { get; private set; }

	public PoolingGroup(PoolingGroupData groupdata, MultithreadingFeature multithreading)
	{
		Data = groupdata;
		parallel = multithreading;
	}

	public void MigrateSettings(PoolingGroupSettings settings)
	{
		if (settings == null)
		{
			settings = new PoolingGroupSettings();
		}
		if (Data.Settings.Profile != settings.Profile)
		{
			return;
		}
		Data.Settings = settings;
		foreach (GenePool value in Pools.Values)
		{
			value.ForcedRecentCapacity = Data.Settings.RecentCapacity;
		}
	}

	public void Create()
	{
		foreach (GenePool value in Pools.Values)
		{
			value.CreateStrategy();
		}
	}

	internal void UpdateStatistics(string id, Dictionary<GeneId, GeneInfoEx> genes)
	{
		foreach (GenePool value in Pools.Values)
		{
			UpdateStatistics(id, genes, value);
		}
	}

	private void UpdateStatistics(string id, Dictionary<GeneId, GeneInfoEx> genes, GenePool pool)
	{
		GeneSet geneSet = pool.ExtractGeneSet(genes.Values);
		if (geneSet == null)
		{
			return;
		}
		List<GeneSet> generation = pool.GetGeneration(GeneGeneration.Young);
		Predicate<GeneSet> matchPredicate = (GeneSet set) => !pool.Data.Survivors.Contains(set.Id);
		GeneSet geneSet2 = pool.FindOneInSet(generation, geneSet, matchPredicate);
		if (geneSet2 == null && pool.Data.DiversitySimilarityThreshold == 0f)
		{
			string gsid = pool.GetMappedGeneset(id);
			List<GeneSet> list = generation.Where((GeneSet x) => x.Id == gsid).ToList();
			if (list.Count > 0)
			{
				geneSet2 = list[0];
			}
		}
		if (geneSet2 != null)
		{
			geneSet2.Vectors[GeneVector.Rating] = geneSet.Rating;
		}
		if (geneSet2 != null && !geneSet.IsIncomplete() && !geneSet2.IsIncomplete())
		{
			pool.MarkSurvivor(geneSet2.Id);
		}
		else
		{
			pool.AddSurvivor(id, geneSet);
		}
	}

	internal GeneSetGroup Apply(GuestInstance guest, Dictionary<GeneId, GeneInfoEx>.ValueCollection genes)
	{
		foreach (GenePool value2 in Pools.Values)
		{
			value2.TryFixGeneOrder(genes);
			value2.CheckState();
		}
		GeneSetGroup nextGroup = GetNextGroup();
		foreach (KeyValuePair<string, GeneSet> sample in nextGroup.Samples)
		{
			GenePool genePool = Pools[sample.Key];
			if (!genePool.Data.Enabled)
			{
				continue;
			}
			if (genePool.Data.Settings.StandardizeGrupoGenes)
			{
				for (int i = 0; i < genePool.Data.GeneOrder.Count; i++)
				{
					string text = genePool.Data.GeneOrder[i];
					if (text.Contains("_Grupo_") && genePool.GeneFactory.Eve.TryGetValue(new GeneId(text), out var value))
					{
						sample.Value.Vector[i] = value.Value;
					}
				}
			}
			Pools[sample.Key].ApplySet(sample.Value, guest);
		}
		aotGroup = parallel.TrySubmit(ProduceGroup);
		return nextGroup;
	}

	private GeneSetGroup GetNextGroup()
	{
		try
		{
			if (aotGroup == null)
			{
				return ProduceGroup();
			}
			GeneSetGroup result = aotGroup.Result;
			aotGroup = null;
			foreach (KeyValuePair<string, GeneSet> sample in result.Samples)
			{
				GenePool genePool = Pools[sample.Key];
				GeneSet value = sample.Value;
				if (genePool.Data.Epoch != value.Epoch)
				{
					return ProduceGroup();
				}
			}
			return result;
		}
		catch (Exception innerException)
		{
			throw new Exception("Unable to produce GS for " + Data.Settings.Name, innerException);
		}
	}

	private GeneSetGroup ProduceGroup()
	{
		Dictionary<string, GeneSet> dictionary = new Dictionary<string, GeneSet>();
		Dictionary<string, GeneSet> randomSets = new Dictionary<string, GeneSet>();
		MultithreadingFeature.ParallelFork parallelFork = parallel.CreateFork();
		Dictionary<string, GenerationInfo> infos = new Dictionary<string, GenerationInfo>();
		foreach (KeyValuePair<string, GenePool> pool2 in Pools)
		{
			string poolid = pool2.Key;
			GenePool pool = pool2.Value;
			GeneSet geneSet = pool.NextFixedSet();
			if (geneSet != null)
			{
				dictionary[poolid] = geneSet;
				continue;
			}
			randomSets[poolid] = pool.CreateGeneSet();
			parallelFork.Run(() => pool.NextRandomSet(randomSets[poolid], 0), delegate(GenerationInfo r)
			{
				infos[poolid] = r;
			});
		}
		parallelFork.Join();
		int step = 0;
		GenerationScore generationScore = new GenerationScore();
		if (dictionary.Count == 0 && Data.Settings.Threshold > 0f)
		{
			for (step = 0; step < Data.Settings.GenerationAttempts; step++)
			{
				generationScore = ComputeMergedScore(randomSets);
				if (generationScore.IsAcceptable(Data.Settings.Threshold))
				{
					break;
				}
				parallelFork = parallel.Fork(Pools, (KeyValuePair<string, GenePool> kv) => kv.Value.NextRandomSet(randomSets[kv.Key], step), delegate(KeyValuePair<string, GenePool> kv, GenerationInfo r)
				{
					infos[kv.Key] = r;
				});
				parallelFork.Join();
			}
		}
		Dictionary<string, GeneSet> merged = new Dictionary<string, GeneSet>();
		dictionary.ForEach(delegate(KeyValuePair<string, GeneSet> kv)
		{
			merged.Add(kv.Key, kv.Value);
		});
		randomSets.ForEach(delegate(KeyValuePair<string, GeneSet> kv)
		{
			merged.Add(kv.Key, kv.Value);
		});
		return new GeneSetGroup
		{
			Step = step,
			MaxSteps = Data.Settings.GenerationAttempts,
			Score = generationScore.TotalScore,
			Samples = merged,
			Infos = infos
		};
	}

	private GenerationScore ComputeMergedScore(Dictionary<string, GeneSet> input)
	{
		Dictionary<string, float> scores = new Dictionary<string, float>();
		float separateMinimalScore = 0f;
		parallel.Fork(input, (KeyValuePair<string, GeneSet> kv) => ComputeScore(Pools[kv.Key], kv.Value), delegate(Dictionary<string, float> r)
		{
			HandlePartialScore(r, scores, ref separateMinimalScore);
		}).Join();
		int unguidedPoolCount = Pools.Values.Where((GenePool x) => x.Data.GeneOrder.Count == x.UnguidedGeneCount()).Count();
		float[] array = scores.Values.ToArray();
		if (Data.Settings.PerceptionThreshold > 0f && unguidedPoolCount > 0)
		{
			array.InplaceMap((float x) => x / (float)unguidedPoolCount);
			separateMinimalScore /= unguidedPoolCount;
		}
		float num = ((array.Length != 0 && array.Where(float.IsNaN).Count() <= 0) ? array.Min() : float.NaN);
		return new GenerationScore
		{
			TotalScore = (float.IsNaN(separateMinimalScore) ? num : ((num + separateMinimalScore) / 2f)),
			MinimalScore = separateMinimalScore,
			ExactScore = num
		};
	}

	private Dictionary<string, float> ComputeScore(GenePool pool, GeneSet set)
	{
		if (pool.UnguidedGeneCount() != pool.Data.GeneOrder.Count)
		{
			return new Dictionary<string, float>();
		}
		if (Data.Settings.PerceptionThreshold > 0f)
		{
			PerceptionScoring perceptionScoring = new PerceptionScoring(Data.Settings.PerceptionThreshold, pool);
			Dictionary<string, float> dictionary = new Dictionary<string, float>();
			{
				foreach (GeneSet item in pool.GetGeneration(GeneGeneration.Recent))
				{
					dictionary[item.Id] = 1f - perceptionScoring.Score(set, item);
				}
				return dictionary;
			}
		}
		return pool.ScoreRecentGuests(set);
	}

	private void HandlePartialScore(Dictionary<string, float> partialScores, Dictionary<string, float> scores, ref float separateMinimalScore)
	{
		if (partialScores.Count > 0)
		{
			float[] array = partialScores.Values.Where((float x) => !float.IsNaN(x)).ToArray();
			if (array.Length != 0)
			{
				separateMinimalScore += array.Min();
			}
		}
		if (scores.Count == 0)
		{
			foreach (KeyValuePair<string, float> partialScore in partialScores)
			{
				scores[partialScore.Key] = partialScore.Value;
			}
			return;
		}
		HashSet<string> hashSet = new HashSet<string>();
		foreach (string key in scores.Keys)
		{
			hashSet.Add(key);
		}
		foreach (string key2 in partialScores.Keys)
		{
			hashSet.Add(key2);
		}
		foreach (string item in hashSet)
		{
			float value = float.NaN;
			scores.TryGetValue(item, out value);
			float value2 = float.NaN;
			partialScores.TryGetValue(item, out value2);
			scores[item] = value + value2;
		}
	}
}
