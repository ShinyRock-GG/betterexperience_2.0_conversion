using System;
using System.Collections.Generic;
using System.IO;
using BetterExperience.GameScopes;
using BetterExperience.Properties;
using BetterExperience.Utils;
using BetterExperience.Wrappers.Pools;

namespace BetterExperience.Features.AlternativeGenetics;

internal class GeneticsFactory
{
	private PersistenceService persistence;

	private PoolManager poolManager;

	private MultithreadingFeature multithreading;

	private AlternativeGeneticsActiveProfiles activeProfiles;

	private AlternativeGeneMapping Mapping { get; set; }

	public Dictionary<string, PoolingGroup> Groups { get; private set; }

	public Observable<GenePool, GeneSet> PostProcessGeneSet { get; } = new Observable<GenePool, GeneSet>();

	public Dictionary<GenePool, (int, int)> Migrations { get; } = new Dictionary<GenePool, (int, int)>();

	public GeneFactoryInfo GeneFactory { get; set; }

	public Dictionary<string, string> RatingSwap { get; set; } = new Dictionary<string, string>();

	public event Action<GenePool> PostCompact = delegate
	{
	};

	public event Action<GenePool, PoolStateSnapshot, PoolStateSnapshot> PostRefresh = delegate
	{
	};

	public event Action PoolsCreated = delegate
	{
	};

	public event Action<GenePool> OnNewEpochStart = delegate
	{
	};

	public GeneticsFactory(PersistenceService persistence, PoolManager poolManager, MultithreadingFeature multithreading)
	{
		GeneticsFactory geneticsFactory = this;
		this.persistence = persistence;
		this.poolManager = poolManager;
		this.multithreading = multithreading;
		if (poolManager.Count == 0)
		{
			ScopeSupport tmpScope = new ScopeSupport();
			tmpScope.Name = "DeferredPoolLoader";
			poolManager.Scope.AddChild(tmpScope);
			poolManager.OnPoolCreated.Add(delegate
			{
				geneticsFactory.GeneFactory = poolManager.GeneFactory;
				geneticsFactory.Load();
				tmpScope.Dispose();
			}, tmpScope);
		}
		else
		{
			GeneFactory = poolManager.GeneFactory;
			Load();
		}
	}

	public Dictionary<string, GenePool> GetPools()
	{
		Dictionary<string, GenePool> dictionary = new Dictionary<string, GenePool>();
		foreach (PoolingGroup value in Groups.Values)
		{
			foreach (KeyValuePair<string, GenePool> pool in value.Pools)
			{
				dictionary[pool.Key] = pool.Value;
			}
		}
		return dictionary;
	}

	private void Load()
	{
		Mapping = persistence.Persisted(() => new AlternativeGeneMapping());
		activeProfiles = persistence.Persisted(() => new AlternativeGeneticsActiveProfiles());
		AlternativeGeneticsInit alternativeGeneticsInit = persistence.Persisted(delegate
		{
			File.WriteAllBytes(Path.Combine(persistence.Dir, "AlternativeGeneticsInit.json"), Resources.AlternativeGeneticsInit);
			return persistence.Persisted(() => (AlternativeGeneticsInit)null);
		});
		Groups = new Dictionary<string, PoolingGroup>();
		foreach (KeyValuePair<string, PoolSettingsInit> item in alternativeGeneticsInit)
		{
			PoolingGroupData poolingGroupData = LoadGroupProfile(item.Key, activeProfiles.GetValueOrAdd(item.Key, () => "default"));
			if (poolingGroupData == null)
			{
				poolingGroupData = new PoolingGroupData
				{
					Settings = item.Value.Settings
				};
			}
			PoolingGroup poolingGroup = (Groups[item.Key] = new PoolingGroup(poolingGroupData, multithreading));
			PoolingGroup poolingGroup3 = poolingGroup;
			if (poolingGroup3.Data.Settings.Profile == "default")
			{
				foreach (KeyValuePair<string, PoolSettings> pooli in item.Value.Pools)
				{
					poolingGroupData.Pools.GetValueOrAdd(pooli.Key, () => new GenePoolData
					{
						Settings = pooli.Value
					});
				}
			}
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, GenePoolData> pool in poolingGroupData.Pools)
			{
				GenePool genePool = new GenePool(pool.Value, GeneFactory, Mapping);
				poolingGroup3.Pools[pool.Key] = genePool;
				if (poolingGroupData.Settings.Profile == "default")
				{
					if (item.Value.Pools.ContainsKey(pool.Key))
					{
						genePool.MigrateSettings(item.Value.Pools[pool.Key]);
					}
					else
					{
						list.Add(pool.Key);
					}
				}
				if (!list.Contains(pool.Key))
				{
					genePool.OnNewEpochStart += delegate(GenePool sender)
					{
						this.OnNewEpochStart(sender);
					};
				}
			}
			foreach (string item2 in list)
			{
				poolingGroupData.Pools.Remove(item2);
				poolingGroup3.Pools.Remove(item2);
			}
			poolingGroup3.MigrateSettings(item.Value.Settings);
			poolingGroup3.Create();
		}
		this.PoolsCreated();
	}

	internal void SetProfile(string groupId, string profileId)
	{
		activeProfiles[groupId] = profileId;
	}

	public PoolingGroupData LoadGroupProfile(string groupId, string profileId)
	{
		string fileName = GetFileName(groupId, profileId);
		return persistence.Persisted(() => (PoolingGroupData)null, fileName);
	}

	private static string GetFileName(string groupId, string profileId)
	{
		return Path.Combine("ag_profiles", groupId + "_" + profileId);
	}

	public void Save()
	{
		foreach (KeyValuePair<string, PoolingGroup> group in Groups)
		{
			string key = group.Key;
			PoolingGroup value = group.Value;
			persistence.Persist(value.Data, GetFileName(key, value.Data.Settings.Profile));
		}
		persistence.Persist(activeProfiles);
	}

	internal void SaveProfile(string groupId, PoolingGroupData Data)
	{
		persistence.Persist(Data, GetFileName(groupId, Data.Settings.Profile));
	}

	public void UpdateStatistics(GuestInstance guest)
	{
		Dictionary<GeneId, GeneInfoEx> dictionary = ExtractGenes(guest);
		SwapRatings(guest, dictionary);
		UpdateStatistics(guest.Id, dictionary);
	}

	private void SwapRatings(GuestInstance guest, Dictionary<GeneId, GeneInfoEx> genes1)
	{
		if (RatingSwap.Count == 0)
		{
			return;
		}
		Dictionary<string, float> dictionary = new Dictionary<string, float>(guest.Rating);
		foreach (KeyValuePair<string, string> item in RatingSwap)
		{
			string key = item.Key;
			string value = item.Value;
			if (dictionary.TryGetValue(key, out var value2) && dictionary.TryGetValue(value, out var value3))
			{
				dictionary[key] = value3;
				dictionary[value] = value2;
			}
			else
			{
				new Logger().Error("Unable to swap group ratings: {0}<>{1}", key, value);
			}
		}
		foreach (GeneInfoEx value5 in genes1.Values)
		{
			if (dictionary.TryGetValue(value5.Group, out var value4))
			{
				value5.Rating = value4;
				continue;
			}
			new Logger().Error("Unable to find group rating: {0}", value5.Group);
		}
	}

	private void UpdateStatistics(string id, Dictionary<GeneId, GeneInfoEx> genes)
	{
		foreach (PoolingGroup value in Groups.Values)
		{
			if (value.Data.Enabled && value.Data.Active)
			{
				value.UpdateStatistics(id, genes);
			}
		}
	}

	internal Dictionary<PoolingGroup, PoolingGroup.GeneSetGroup> Apply(GuestInstance guest)
	{
		foreach (string key in guest.Rating.Keys)
		{
			bool flag = false;
			foreach (PoolingGroup value2 in Groups.Values)
			{
				foreach (GenePool value3 in value2.Pools.Values)
				{
					if (value3.Data.Settings.Groups.Contains(key))
					{
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				new Logger().Error("Gene group {0} is not mapped", key);
			}
		}
		Dictionary<GeneId, GeneInfoEx> dictionary = ExtractGenes(guest);
		Dictionary<PoolingGroup, PoolingGroup.GeneSetGroup> dictionary2 = new Dictionary<PoolingGroup, PoolingGroup.GeneSetGroup>();
		foreach (PoolingGroup value4 in Groups.Values)
		{
			PoolingGroup.GeneSetGroup value = value4.Apply(guest, dictionary.Values);
			dictionary2[value4] = value;
		}
		return dictionary2;
	}

	private Dictionary<GeneId, GeneInfoEx> ExtractGenes(GuestInstance guest)
	{
		Dictionary<GeneId, GeneInfoEx> dictionary = guest.ExtractAll();
		if (Mapping.Count > 0)
		{
			foreach (GeneInfoEx value2 in dictionary.Values)
			{
				if (Mapping.TryGetValue(value2.Id.Item1, out var value))
				{
					value2.Rating = guest.Rating[value];
					value2.Group = value;
				}
			}
		}
		return dictionary;
	}

	internal List<GenePool> ListAppliedPools(GuestInstance guestInstance)
	{
		List<GenePool> list = new List<GenePool>();
		foreach (PoolingGroup value in Groups.Values)
		{
			foreach (GenePool value2 in value.Pools.Values)
			{
				string genesetid = value2.GetMappedGeneset(guestInstance.Id);
				if (genesetid != null)
				{
					GeneSet geneSet = value2.GetGeneration(GeneGeneration.Young).Find((GeneSet gs) => genesetid == gs.Id);
					if (geneSet != null && geneSet.Ancestors.Count > 0)
					{
						list.Add(value2);
					}
				}
			}
		}
		return list;
	}

	internal List<(GenePool, GeneSet)> ListAppliedSets(GuestInstance guestInstance)
	{
		List<(GenePool, GeneSet)> list = new List<(GenePool, GeneSet)>();
		foreach (PoolingGroup value in Groups.Values)
		{
			foreach (GenePool value2 in value.Pools.Values)
			{
				string genesetid = value2.GetMappedGeneset(guestInstance.Id);
				if (genesetid != null)
				{
					GeneSet geneSet = value2.GetGeneration(GeneGeneration.Young).Find((GeneSet gs) => genesetid == gs.Id);
					if (geneSet != null && geneSet.Ancestors.Count > 0)
					{
						list.Add((value2, geneSet));
					}
				}
			}
		}
		return list;
	}
}
