using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BetterExperience.Features.AlternativeGenetics;

internal class PerceptionScoring
{
	public float PerceptionScale { get; }

	public GenePool Pool { get; }

	public PerceptionScoring(float peceptionScale, GenePool pool)
	{
		PerceptionScale = peceptionScale;
		Pool = pool;
	}

	public int[] Translate(float[] input)
	{
		return input.Select((float x) => Mathf.RoundToInt(x / PerceptionScale)).ToArray();
	}

	public float Score(GeneSet geneSet, GeneSet genes)
	{
		return Score(geneSet, new GeneSet[1] { genes })[0];
	}

	public float[] Score(GeneSet geneSet, IList<GeneSet> testSet)
	{
		int[] array = Translate(geneSet.Vector);
		float[] array2 = new float[testSet.Count];
		for (int i = 0; i < testSet.Count; i++)
		{
			int[] array3 = Translate(testSet[i].Vector);
			float num = 0f;
			float num2 = 0f;
			for (int j = 0; j < array.Length; j++)
			{
				float weight = GetWeight(j);
				if (array[j] - array3[j] == 0)
				{
					num += weight;
				}
				num2 += weight;
			}
			array2[i] = num / num2;
		}
		return array2;
	}

	public virtual float GetWeight(int i)
	{
		return Pool.FineTuning.GetSimilarityWeight(Pool.Data.GeneOrder[i]);
	}
}
