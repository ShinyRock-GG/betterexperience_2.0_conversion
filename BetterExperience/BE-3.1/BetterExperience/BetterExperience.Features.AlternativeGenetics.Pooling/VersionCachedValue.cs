using System;

namespace BetterExperience.Features.AlternativeGenetics.Pooling;

internal class VersionCachedValue<T> where T : class
{
	private Func<T> producer;

	private volatile T value;

	private volatile int valueVersion = -1;

	private Func<int> version;

	public T Value
	{
		get
		{
			int num = version();
			if (value == null || valueVersion != num)
			{
				value = producer();
				valueVersion = num;
			}
			return value;
		}
	}

	public VersionCachedValue(Func<int> version, Func<T> func)
	{
		producer = func;
		this.version = version;
	}
}
