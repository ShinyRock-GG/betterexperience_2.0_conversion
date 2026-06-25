using System;
using System.Collections.Generic;
using System.Linq;
using BetterExperience.Features.AlternativeGenetics.Pooling;
using BetterExperience.Utils;
using BetterExperience.Wrappers.Pools;
using UnityEngine;

namespace BetterExperience.Features.AlternativeGenetics;

internal class GenePool
{
	private Logger logger = new Logger();

	private Dictionary<string, int> geneIndexCache;

	private GeneSetFineTuningManager tuningManager;

	private PoolingStrategy strategy;

	private Dictionary<string, string> groupMapping;

	private bool geneOrderChecked;

	public GenePoolData Data { get; private set; }

	public PredictableRandom Random { get; private set; }

	public Logger Logger => logger;

	private float SimilarityThresold => Data.DiversitySimilarityThreshold;

	public int Level => (int)SimilarityThresold;

	public int Capacity => Data.InitialCapacity + Data.ExtendedCapacity;

	public int ForcedRecentCapacity { get; set; } = -1;

	public int RecentCapacity
	{
		get
		{
			if (ForcedRecentCapacity < 0)
			{
				return Capacity;
			}
			return ForcedRecentCapacity;
		}
	}

	public int GuaranteedRandoms
	{
		get
		{
			if (!Data.Active)
			{
				return 0;
			}
			return Data.GuaranteedRandoms;
		}
	}

	public int IterationLength => 10;

	public int GenerationCapacity => IterationLength - GuaranteedRandoms;

	private bool Empty => (from x in GetGeneration(GeneGeneration.Young)
		where x.MappedGuestId == null
		select x).ToArray().Length == 0;

	public int Remaining => (from x in GetGeneration(GeneGeneration.Young)
		where x.MappedGuestId == null
		select x).ToArray().Length + Data.DiversityPenalty;

	public GeneFactoryInfo GeneFactory { get; private set; }

	public MultithreadingFeature Multithreading { get; }

	public GeneSetFineTuningManager FineTuning => tuningManager;

	public event Action<GenePool> PostCompact = delegate
	{
	};

	public event Action<GenePool, PoolStateSnapshot, PoolStateSnapshot> PostRefresh = delegate
	{
	};

	public event Action<GenePool, GeneSet> PostProcessGeneSet = delegate
	{
	};

	public event Action<GenePool> OnNewEpochStart = delegate
	{
	};

	internal float[] CreateMomentumLock(float[] gradient)
	{
		if (Data.Settings.UseMomentum)
		{
			return FineTuning.CreateMomentumLock(Data.GeneOrder, gradient);
		}
		return new float[Data.GeneOrder.Count];
	}

	public GenePool(GenePoolData data, GeneFactoryInfo geneFactory, AlternativeGeneMapping mapping)
	{
		groupMapping = mapping;
		GeneFactory = geneFactory;
		Data = data;
		if (Data.Seed == 0)
		{
			Data.Seed = Environment.TickCount;
		}
		if (data.GeneOrder == null)
		{
			data.GeneOrder = new List<string>();
		}
		if (data.Generations == null)
		{
			data.Generations = new Dictionary<GeneGeneration, List<GeneSet>>();
		}
		tuningManager = new GeneSetFineTuningManager(this);
		Random = new PredictableRandom(Data.Seed);
		logger.Prefix = "[ Pool " + Data.Settings.Name + "]";
	}

	internal void Clear()
	{
		Data.Generations.Clear();
		Data.DiversitySimilarityThreshold = 0f;
		Data.DiversityPenalty = 0;
		Data.ExtendedCapacity = 0;
		tuningManager = new GeneSetFineTuningManager(this);
		Data.Error = 0f;
		Data.Iteration = 0;
		Data.Epoch = 0;
		geneOrderChecked = false;
	}

	internal (int, int) MigrateSettings(PoolSettings settings)
	{
		int num = 0;
		int item = 0;
		PoolSettings settings2 = Data.Settings;
		if (settings.EnforceSymmetry != settings2.EnforceSymmetry)
		{
			num++;
			settings2.EnforceSymmetry = settings.EnforceSymmetry;
			logger.Info("Pool migration: Changed EnforceSymmetry to {0}", settings2.EnforceSymmetry);
		}
		if (!object.Equals(settings.Name, settings2.Name))
		{
			settings2.Name = settings.Name;
			num++;
			logger.Info("Pool migration: Pool name changed to {0}", settings2.Name);
		}
		if (!settings.ForcedValue.ContentEquals(settings2.ForcedValue))
		{
			settings2.ForcedValue = settings.ForcedValue;
			num++;
			logger.Info("Pool migration: Changed forced value");
		}
		if (!object.Equals(settings.Hidden, settings2.Hidden))
		{
			settings2.Hidden = settings.Hidden;
			num++;
			logger.Info("Pool migration: Changed hidden flag");
		}
		if (!object.Equals(settings.FastPass, settings2.FastPass))
		{
			settings2.FastPass = settings.FastPass;
			num++;
			logger.Info("Pool migration: Changed fast pass flag");
		}
		if (!object.Equals(settings.UseMomentum, settings2.UseMomentum))
		{
			settings2.UseMomentum = settings.UseMomentum;
			num++;
			logger.Info("Pool migration: Changed use momentum flag");
		}
		if (!object.Equals(settings.Comparator, settings2.Comparator))
		{
			settings2.Comparator = settings.Comparator;
			num++;
			logger.Info("Pool migration: Changed comparator");
		}
		if (!object.Equals(settings.StandardizeGrupoGenes, settings2.StandardizeGrupoGenes))
		{
			settings2.StandardizeGrupoGenes = settings.StandardizeGrupoGenes;
			num++;
			logger.Info("Pool migration: Changed StandardizeGrupoGenes");
		}
		if (!settings.Groups.ContentEquals(settings2.Groups))
		{
			List<string> list = settings2.Groups.Except(settings.Groups).ToList();
			if (list.Count > 0)
			{
				logger.Error("Remove groups {0}", string.Join(";", list.ToArray()));
				List<string> list2 = new List<string>();
				foreach (string item2 in list)
				{
					List<string> list3 = GeneFactory.GroupToGenes[item2];
					foreach (string item3 in Data.GeneOrder)
					{
						if (list3.Contains(new GeneId(item3).Item1))
						{
							list2.Add(item3);
						}
					}
				}
				RemoveFromGeneOrder(list2);
			}
			settings2.Groups = settings.Groups;
			logger.Info("Pool migration: Changed groups");
		}
		settings2.FineTuning = settings.FineTuning;
		PoolingFactory poolingFactory = new PoolingFactory();
		PoolingStrategy poolingStrategy = poolingFactory.Create(this);
		settings2.Pooling = settings.Pooling;
		PoolingStrategy poolingStrategy2 = poolingFactory.Create(this);
		if (poolingStrategy.GetType() != poolingStrategy2.GetType())
		{
			logger.Error("Pool cleared due to strategy change");
			Clear();
		}
		tuningManager = new GeneSetFineTuningManager(this);
		return (num, item);
	}

	private void RemoveFromGeneOrder(List<string> deadgenes)
	{
		List<string> list = Data.GeneOrder.Where((string x) => !deadgenes.Contains(x)).ToList();
		int[] neworder = list.Select((string x) => Data.GeneOrder.IndexOf(x)).ToArray();
		foreach (List<GeneSet> value in Data.Generations.Values)
		{
			foreach (GeneSet item in value)
			{
				GeneVector[] array = item.Vectors.Keys.ToArray();
				foreach (GeneVector key in array)
				{
					float[] vector = item.Vectors[key];
					item.Vectors[key] = MigrateVector(vector, neworder);
				}
			}
		}
		Data.DiversitySimilarityThreshold = 0f;
		Data.GeneOrder = list;
	}

	internal GeneSet CreateGeneSet()
	{
		GeneSet geneSet = new GeneSet();
		geneSet.Id = Guid.NewGuid().ToString();
		geneSet.Vectors = new Dictionary<GeneVector, float[]>();
		geneSet.Vectors[GeneVector.Data] = new float[Data.GeneOrder.Count].Fill(float.NaN);
		geneSet.Iteration = Data.Iteration;
		geneSet.Epoch = Data.Epoch;
		return geneSet;
	}

	private float[] MigrateVector(float[] vector, int[] neworder)
	{
		if (vector == null)
		{
			return null;
		}
		float[] array = new float[neworder.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = vector[neworder[i]];
		}
		return array;
	}

	public string GetMappedGeneset(string guestId)
	{
		foreach (List<GeneSet> value in Data.Generations.Values)
		{
			foreach (GeneSet item in value)
			{
				if (item.MappedGuestId == guestId)
				{
					return item.Id;
				}
			}
		}
		return null;
	}

	internal void MarkSurvivor(string mappedset)
	{
		Data.Survivors.Add(mappedset);
	}

	internal void AddSurvivor(string id, GeneSet geneset)
	{
		Data.Survivors.Add(geneset.Id);
		GetGeneration(GeneGeneration.Young).Add(geneset);
		geneset.MappedGuestId = id;
	}

	public List<GeneSet> GetGeneration(GeneGeneration generation)
	{
		return Data.Generations.GetValueOrAdd(generation, () => new List<GeneSet>());
	}

	internal bool Matches(GeneInfoEx gene)
	{
		string valueOrDefault = Extensions.GetValueOrDefault(groupMapping, gene.Id.Item1, gene.Group);
		return Data.Settings.Groups.Contains(valueOrDefault);
	}

	public int GeneIndex(string geneId)
	{
		if (geneIndexCache == null)
		{
			geneIndexCache = new Dictionary<string, int>();
			for (int i = 0; i < Data.GeneOrder.Count; i++)
			{
				geneIndexCache[Data.GeneOrder[i]] = i;
			}
		}
		if (!geneIndexCache.TryGetValue(geneId, out var value))
		{
			value = Data.GeneOrder.IndexOf(geneId);
			if (value < 0)
			{
				value = Data.GeneOrder.Count;
				Data.GeneOrder.Add(geneId);
				tuningManager.RegisterNewGene(geneId);
				ExpandVectors();
			}
			geneIndexCache[geneId] = value;
		}
		return value;
	}

	internal void SetGeneValue(GeneSet result, string geneId, float value, bool nosymmetry = false, bool guidance = true, float step = 0f)
	{
		if (!float.IsNaN(value))
		{
			value = Mathf.Clamp(Mathf.Abs(value), 0f, 1f);
		}
		if (step > 0f)
		{
			value = Mathf.Round(value / step) * step;
		}
		int num = GeneIndex(geneId);
		if (result.Vector == null)
		{
			result.Vectors[GeneVector.Data] = new float[Data.GeneOrder.Count];
			result.Vector.Fill(float.NaN);
		}
		else if (result.Vector.Length < Data.GeneOrder.Count)
		{
			float[] array = new float[Data.GeneOrder.Count];
			array.Fill(float.NaN);
			Array.Copy(result.Vector, array, result.Vector.Length);
			result.Vectors[GeneVector.Data] = array;
		}
		if (guidance)
		{
			value = tuningManager.ApplyValueGuidance(geneId, result.Vector[num], value);
		}
		value = tuningManager.ApplyValueLimits(geneId, value);
		if (result.Vector[num] == value)
		{
			return;
		}
		if (Data.Settings.EnforceSymmetry && !nosymmetry)
		{
			foreach (string symmetricGene in tuningManager.GetSymmetricGenes(geneId))
			{
				SetGeneValue(result, symmetricGene, value, nosymmetry: true);
			}
		}
		result.Vector[num] = value;
	}

	internal void Refresh()
	{
		logger.Info("Refreshing pool");
		if (!Empty)
		{
			logger.Info("Refresh cancelled. Not empty");
			return;
		}
		PoolStateSnapshot arg = TakeStateSnapshot();
		int epoch = Data.Epoch;
		strategy.Populate();
		if (Data.Epoch > epoch)
		{
			this.OnNewEpochStart(this);
		}
		Data.Iteration++;
		this.PostRefresh(this, arg, TakeStateSnapshot());
		Data.Survivors.Clear();
		Data.Statistics.NonDeviantGenes = 0;
	}

	public void InvokePostProcessor(GeneSet gs)
	{
		this.PostProcessGeneSet(this, gs);
	}

	public List<Tuple<GeneSet, float>> FindInSet(List<GeneSet> set, GeneSet value, bool ignoreThreshold = false, float thresholdFactor = 1f, bool ignoreTuning = false, float forcedThreshold = -1f)
	{
		_ = Data.GeneOrder.Count;
		List<Tuple<GeneSet, float>> list = new List<Tuple<GeneSet, float>>();
		float num = ((forcedThreshold > 0f) ? forcedThreshold : SimilarityThresold);
		num *= thresholdFactor;
		foreach (GeneSet item in set)
		{
			float num2 = SimilarityDistance(value, item, ignoreTuning);
			if (num2 <= num || ignoreThreshold)
			{
				list.Add(new Tuple<GeneSet, float>(item, num2));
			}
			else if (SimilarityThresold == 0f && value.Ancestors.Contains(item.Id))
			{
				list.Add(new Tuple<GeneSet, float>(item, num2));
			}
		}
		list.Sort((Tuple<GeneSet, float> a, Tuple<GeneSet, float> b) => a.Item2.CompareTo(b.Item2));
		return list;
	}

	public float SimilarityDistance(GeneSet value, GeneSet x, bool ignoreTuning = false)
	{
		if (Data.Settings.Comparator == "cos")
		{
			return CosineDistance(value, x);
		}
		if (Data.Settings.Comparator == "eucos")
		{
			return EuclideanDistance(value, x) * CosineDistance(value, x);
		}
		return EuclideanDistance(value, x, ignoreTuning);
	}

	public float CosineDistance(GeneSet a, GeneSet b)
	{
		float num = ArrayUtil.TransformVectors(a.Vector, b.Vector, (float i, float j) => i * j).Sum();
		float f = a.Vector.Select((float i) => i * i).Sum();
		f = Mathf.Sqrt(f);
		float f2 = b.Vector.Select((float i) => i * i).Sum();
		f2 = Mathf.Sqrt(f2);
		float num2 = 1f - num / (f * f2);
		if ((double)num2 < 1E-05)
		{
			return 0f;
		}
		return num2;
	}

	public float EuclideanDistance(GeneSet value, GeneSet x, bool ignoreTuning = false)
	{
		float[] array = ArrayUtil.TransformVectors(value.Vector, x.Vector, (float a, float b) => Mathf.Pow(a - b, 2f));
		if (!ignoreTuning)
		{
			array.InplaceMap((int index, float val) => tuningManager.GetSimilarityWeight(Data.GeneOrder[index]) * val);
		}
		return Mathf.Sqrt(array.Sum());
	}

	internal GeneSet FindOneInSet(List<GeneSet> set, GeneSet value, Predicate<GeneSet> matchPredicate = null)
	{
		List<Tuple<GeneSet, float>> list = FindInSet(set, value);
		if (list.Count > 0)
		{
			foreach (Tuple<GeneSet, float> item in list)
			{
				if (matchPredicate == null || matchPredicate(item.Item1))
				{
					return item.Item1;
				}
			}
		}
		return null;
	}

	private GeneSet NextUnmapped(List<GeneSet> set)
	{
		List<GeneSet> list = set.FindAll((GeneSet gs) => gs.MappedGuestId == null).ToList();
		if (Data.DiversityPenalty > 0 && Random.NextInt(list.Count + Data.DiversityPenalty) > list.Count)
		{
			return null;
		}
		if (list.Count > 1)
		{
			return list.InplaceShuffle(Random.NextInt).FirstOrDefault();
		}
		return list.FirstOrDefault();
	}

	public void TryFixGeneOrder(ICollection<GeneInfoEx> genes)
	{
		if (geneOrderChecked)
		{
			return;
		}
		geneOrderChecked = true;
		HashSet<string> hashSet = new HashSet<string>();
		foreach (GeneInfoEx gene in genes)
		{
			if (Matches(gene))
			{
				hashSet.Add(gene.Id.ToString());
				if (float.IsNaN(gene.Value))
				{
					logger.Error("Gene {0} is nan", gene.Id);
				}
			}
		}
		if (hashSet.Count != Data.GeneOrder.Count)
		{
			UpdateToNewGeneOrder(hashSet);
		}
		strategy.Initialize();
	}

	private void UpdateToNewGeneOrder(ICollection<string> actualGenes)
	{
		int count = Data.GeneOrder.Count;
		List<string> list = Data.GeneOrder.Except(actualGenes).ToList();
		RemoveFromGeneOrder(list);
		if (list.Count > 0)
		{
			logger.Error("Removed {0} genes. {1}->{2}", list.Count, count, Data.GeneOrder.Count);
		}
		int count2 = Data.GeneOrder.Count;
		foreach (string actualGene in actualGenes)
		{
			GeneIndex(actualGene);
		}
		if (count2 < Data.GeneOrder.Count)
		{
			ExpandVectors();
		}
	}

	private void ExpandVectors()
	{
		foreach (List<GeneSet> value in Data.Generations.Values)
		{
			foreach (GeneSet item in value)
			{
				GeneVector[] array = item.Vectors.Keys.ToArray();
				foreach (GeneVector key in array)
				{
					float[] array2 = new float[Data.GeneOrder.Count].Fill(float.NaN);
					item.Vectors[key].CopyTo(array2, 0);
					item.Vectors[key] = array2;
				}
			}
		}
	}

	private void AddToRecentSet(GeneSet gs)
	{
		List<GeneSet> generation = GetGeneration(GeneGeneration.Recent);
		if (generation.Count >= Capacity)
		{
			generation.RemoveAt(0);
		}
		generation.Add(gs);
	}

	private void ApplySetTo(GeneSet gs, GuestInstance guest)
	{
		if (GetGeneration(GeneGeneration.Young).Contains(gs))
		{
			gs.MappedGuestId = guest.Id;
		}
		List<GeneInfo> list = new List<GeneInfo>();
		for (int i = 0; i < gs.Vector.Length; i++)
		{
			if (Data.Settings.ForcedValue != null && Data.Settings.ForcedValue.Count > 0)
			{
				gs.Vector[i] = Data.Settings.ForcedValue[0];
			}
			float num = gs.Vector[i];
			if (!float.IsNaN(num))
			{
				GeneInfo item = new GeneInfo
				{
					Id = new GeneId(Data.GeneOrder[i]),
					Value = num
				};
				list.Add(item);
			}
		}
		guest.UpdateAll(list);
	}

	internal void Flush()
	{
		Data.Seed = Random.Seed;
	}

	public GeneSet ExtractGeneSet(ICollection<GeneInfoEx> set, bool norate = false)
	{
		GeneSet geneSet = CreateGeneSet();
		bool flag = false;
		foreach (GeneInfoEx item in set)
		{
			if (!Matches(item))
			{
				continue;
			}
			float rating = item.Rating;
			if (rating > 0f || norate)
			{
				SetGeneValue(geneSet, item.Id.ToString(), item.Value, nosymmetry: false, guidance: false);
				flag = flag || rating > 0f;
				if (!norate)
				{
					if (geneSet.Rating == null)
					{
						geneSet.Vectors[GeneVector.Rating] = new float[Data.GeneOrder.Count];
					}
					int num = Data.GeneOrder.IndexOf(item.Id.ToString());
					if (num >= geneSet.Rating.Length)
					{
						float[] array = new float[Data.GeneOrder.Count];
						geneSet.Rating.CopyTo(array, 0);
						geneSet.Vectors[GeneVector.Rating] = array;
					}
					geneSet.Rating[num] = rating;
				}
			}
			else
			{
				SetGeneValue(geneSet, item.Id.ToString(), float.NaN, nosymmetry: false, guidance: false);
			}
		}
		if (flag || norate)
		{
			return geneSet;
		}
		return null;
	}

	private PoolStateSnapshot TakeStateSnapshot()
	{
		return new PoolStateSnapshot
		{
			OldCount = GetGeneration(GeneGeneration.Old).Count,
			MatureCount = GetGeneration(GeneGeneration.Mature).Count,
			YoungCount = GetGeneration(GeneGeneration.Young).Count,
			Level = Level,
			Threshold = SimilarityThresold
		};
	}

	internal void CompleteGeneSet(GeneSet target)
	{
	}

	internal void Fit()
	{
	}

	internal void Restart()
	{
		Data.DiversityPenalty = 0;
		GetGeneration(GeneGeneration.Young).Clear();
		GetGeneration(GeneGeneration.Mature).Clear();
	}

	internal void FirePostProcessGeneSet(GeneSet gs)
	{
		this.PostProcessGeneSet(this, gs);
	}

	internal void FirePostCompact()
	{
		this.PostCompact(this);
	}

	internal void CreateStrategy()
	{
		strategy = new PoolingFactory().Create(this);
	}

	public GeneSet NextFixedSet()
	{
		if (Data.Enabled)
		{
			return NextUnmapped(GetGeneration(GeneGeneration.Young));
		}
		return null;
	}

	public GenerationInfo NextRandomSet(GeneSet target, int step)
	{
		return strategy.ProduceRandomGeneSet(target, step);
	}

	public void ApplySet(GeneSet gs, GuestInstance guest)
	{
		gs.MappedGuestId = guest.Id;
		ApplySetTo(gs, guest);
		AddToRecentSet(gs);
		if (!GetGeneration(GeneGeneration.Young).Contains(gs) && Data.DiversityPenalty > 0)
		{
			Data.DiversityPenalty--;
		}
	}

	internal Dictionary<string, float> ScoreRecentGuests(GeneSet value)
	{
		Dictionary<string, float> dictionary = new Dictionary<string, float>();
		foreach (GeneSet item in GetGeneration(GeneGeneration.Recent))
		{
			dictionary[item.MappedGuestId] = SimilarityDistance(item, value);
		}
		return dictionary;
	}

	public GeneSet[] GetSpecialDumpVectors()
	{
		return strategy.GetSpecialDumpVectors();
	}

	internal void NotifyPoolUpgraded()
	{
	}

	internal GeneSet CopyGeneSet(GeneSet g)
	{
		GeneSet geneSet = CreateGeneSet();
		g.Vector.CopyTo(geneSet.Vector, 0);
		return geneSet;
	}

	public VersionCachedValue<T> EpochCachedValue<T>(Func<T> producer) where T : class
	{
		return new VersionCachedValue<T>(() => Data.Epoch, producer);
	}

	public VersionCachedValue<T> IterationCachedValue<T>(Func<T> producer) where T : class
	{
		return new VersionCachedValue<T>(() => Data.Iteration, producer);
	}

	internal void CheckState()
	{
		if (Data.Enabled && Empty && Data.DiversityPenalty <= 0)
		{
			Refresh();
		}
	}

	internal int UnguidedGeneCount()
	{
		if (Data.GuidanceDisabled)
		{
			return Data.GeneOrder.Count;
		}
		return Data.GeneOrder.Count - FineTuning.GuidedCount;
	}
}
