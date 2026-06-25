using System;

namespace BetterExperience.Utils;

public struct BitMask<T> : IEquatable<BitMask<T>> where T : Enum
{
	private readonly bool acceptAll;

	private readonly int bitmask;

	public bool Empty => bitmask == 0;

	public BitMask(params T[] args)
		: this(0, args)
	{
	}

	public BitMask(int bitmask, params T[] args)
	{
		foreach (T val in args)
		{
			bitmask |= 1 << Convert.ToInt32(val);
		}
		this.bitmask = bitmask;
		acceptAll = false;
	}

	public BitMask(bool acceptAll)
	{
		bitmask = 0;
		this.acceptAll = acceptAll;
	}

	public BitMask<T> Add(params T[] args)
	{
		return new BitMask<T>(bitmask, args);
	}

	public BitMask<T> Add(T value)
	{
		return new BitMask<T>(bitmask, value);
	}

	public BitMask<T> Remove(T value)
	{
		int num = 1 << Convert.ToInt32(value);
		return new BitMask<T>(bitmask & ~num);
	}

	public bool Contains(T value)
	{
		if (acceptAll)
		{
			return true;
		}
		return (bitmask & (1 << Convert.ToInt32(value))) != 0;
	}

	public bool ContainsAny(params T[] values)
	{
		foreach (T value in values)
		{
			if (Contains(value))
			{
				return true;
			}
		}
		return false;
	}

	public bool ContainsAny(BitMask<T> values)
	{
		return (values.bitmask & bitmask) != 0;
	}

	public bool IsNoFilter()
	{
		return acceptAll;
	}

	public static BitMask<T> Of(params T[] head)
	{
		return new BitMask<T>(head);
	}

	public static BitMask<T> AllOf()
	{
		return new BitMask<T>(acceptAll: true);
	}

	public override bool Equals(object obj)
	{
		if (obj == null || GetType() != obj.GetType())
		{
			return false;
		}
		BitMask<T> bitMask = (BitMask<T>)obj;
		if (bitmask == bitMask.bitmask)
		{
			return acceptAll == bitMask.acceptAll;
		}
		return false;
	}

	public bool Equals(BitMask<T> other)
	{
		if (acceptAll == other.acceptAll)
		{
			return bitmask == other.bitmask;
		}
		return false;
	}

	public override int GetHashCode()
	{
		int num = 662212440 * -1521134295;
		bool flag = acceptAll;
		int num2 = (num + flag.GetHashCode()) * -1521134295;
		int num3 = bitmask;
		return num2 + num3.GetHashCode();
	}
}
