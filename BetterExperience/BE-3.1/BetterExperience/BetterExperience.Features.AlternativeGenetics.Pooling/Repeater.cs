using System.Collections.Generic;

namespace BetterExperience.Features.AlternativeGenetics.Pooling;

internal class Repeater : PoolingStrategy
{
	private GenePool pool;

	private RepeaterPoolConfiguration settings;

	public Repeater(GenePool pool, RepeaterPoolConfiguration settings)
	{
		this.pool = pool;
		this.settings = settings;
	}

	public GeneSet[] GetSpecialDumpVectors()
	{
		return new GeneSet[0];
	}

	public void Initialize()
	{
	}

	public void Populate()
	{
		List<GeneSet> generation = pool.GetGeneration(GeneGeneration.Old);
		List<GeneSet> generation2 = pool.GetGeneration(GeneGeneration.Young);
		foreach (GeneSet item in generation2)
		{
			if (pool.Data.Survivors.Contains(item.Id) && item.Rating != null && item.Rating[0] > 0.4f)
			{
				generation.Add(item);
			}
		}
		generation2.Clear();
		while (generation.Count > pool.Capacity)
		{
			generation.RemoveAt(0);
		}
		foreach (GeneSet item2 in generation)
		{
			generation2.Add(pool.CopyGeneSet(item2));
		}
		pool.Data.DiversityPenalty = pool.Data.InitialCapacity - generation2.Count;
	}

	public GenerationInfo ProduceRandomGeneSet(GeneSet gs, int step)
	{
		for (int i = 0; i < gs.Vector.Length; i++)
		{
			gs.Vector[i] = pool.Random.NextFloat(1f);
		}
		return new GenerationInfo();
	}
}
