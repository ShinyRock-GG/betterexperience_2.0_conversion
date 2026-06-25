using System;

namespace BetterExperience.Wrappers.Pools;

public class GeneId : Tuple<string, int>
{
	public GeneId(string item1, int item2)
		: base(item1, item2)
	{
	}

	public GeneId(Tuple<string, int> value)
		: base(value.Item1, value.Item2)
	{
	}

	public GeneId(object unknown)
		: this(ParseObject(unknown))
	{
	}

	public static Tuple<string, int> ParseObject(object unknown)
	{
		if (unknown is Tuple<string, int>)
		{
			return unknown as Tuple<string, int>;
		}
		if (unknown is string)
		{
			string[] array = ((string)unknown).Split(new char[1] { '#' });
			return new Tuple<string, int>(array[0], int.Parse(array[1]));
		}
		throw new ArgumentException("Unknown obj " + unknown);
	}

	public override string ToString()
	{
		return base.Item1 + "#" + base.Item2;
	}
}
