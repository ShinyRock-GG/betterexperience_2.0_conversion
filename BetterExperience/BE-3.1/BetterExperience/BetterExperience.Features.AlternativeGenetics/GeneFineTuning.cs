using System;
using Newtonsoft.Json;

namespace BetterExperience.Features.AlternativeGenetics;

internal class GeneFineTuning
{
	[JsonIgnore]
	private (float, float)? _minmax;

	public float? DeviationFactor { get; set; }

	public float? Guidance { get; set; }

	public bool? NonDiverse { get; set; }

	public float? SimilarityWeight { get; set; }

	public string Value { get; set; }

	public float? EvolutionFactor { get; set; }

	public string MomentumGroup { get; set; }

	public float? InitialValue { get; set; }

	[JsonIgnore]
	public (float, float)? MinMax
	{
		get
		{
			if (Value != null)
			{
				if (!_minmax.HasValue)
				{
					_minmax = ParseTuple(Value);
				}
				return _minmax;
			}
			return null;
		}
	}

	private (float, float) ParseTuple(string repr)
	{
		string[] array = repr.Split(new char[1] { ':' });
		if (array.Length > 2)
		{
			throw new Exception("Illegal range value " + repr);
		}
		float num;
		float item;
		if (array.Length == 1)
		{
			num = float.Parse(array[0]);
			item = num;
		}
		else
		{
			num = float.Parse(array[0]);
			item = float.Parse(array[1]);
		}
		return (num, item);
	}
}
