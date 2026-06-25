using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BetterExperience.Utils;

public static class Extensions
{
	public static V GetValueOrAdd<K, V>(this IDictionary<K, V> dic, K key, Func<V> producer)
	{
		if (!dic.TryGetValue(key, out var value))
		{
			value = (dic[key] = producer());
		}
		return value;
	}

	public static V GetValueOrDefault<K, V>(this IDictionary<K, V> dic, K key, V def)
	{
		if (!dic.TryGetValue(key, out var value))
		{
			return def;
		}
		return value;
	}

	public static V GetValueOrDefault<K, V>(this IDictionary<K, V> dic, K key, Func<V> def)
	{
		if (!dic.TryGetValue(key, out var value))
		{
			return def();
		}
		return value;
	}

	public static bool ContentEquals<K, V>(this IDictionary<K, V> dic, IDictionary<K, V> other)
	{
		if (object.Equals(dic, other))
		{
			return true;
		}
		if (dic == null || other == null)
		{
			return false;
		}
		List<K> first = (from k in dic.Keys.ToList()
			orderby k
			select k).ToList();
		List<K> second = (from k in other.Keys.ToList()
			orderby k
			select k).ToList();
		if (!first.ContentEquals(second))
		{
			return false;
		}
		foreach (K key in dic.Keys)
		{
			dic.TryGetValue(key, out var value);
			other.TryGetValue(key, out var value2);
			if (!object.Equals(value, value2))
			{
				return false;
			}
		}
		return true;
	}

	public static void ForEach<K, V>(this IDictionary<K, V> dic, Action<K, V> callback)
	{
		foreach (KeyValuePair<K, V> item in dic)
		{
			callback(item.Key, item.Value);
		}
	}

	public static void AddOrReplace<K, V>(this IDictionary<K, V> dic, K key, V value)
	{
		dic.Remove(key);
		dic.Add(key, value);
	}

	public static T ElementAtOrDefault<T>(this IList<T> col, int index, T def)
	{
		if (col.Count > index)
		{
			return col[index];
		}
		return def;
	}

	public static bool ContentEquals<T>(this ICollection<T> first, ICollection<T> second)
	{
		if (object.Equals(first, second))
		{
			return true;
		}
		if (first == null || second == null)
		{
			return false;
		}
		if (first.Count != second.Count)
		{
			return false;
		}
		if (first.Count == 0)
		{
			return true;
		}
		return first.SequenceEqual(second);
	}

	public static void ForEach<T>(this ICollection<T> col, Action<T> callback)
	{
		foreach (T item in col)
		{
			callback(item);
		}
	}

	public static void RemoveIf<T>(this IList<T> col, Func<T, bool> test)
	{
		for (int i = 0; i < col.Count; i++)
		{
			if (test(col[i]))
			{
				col.RemoveAt(i);
				i--;
			}
		}
	}

	public static string Join(this string[] str, string separator)
	{
		return string.Join(separator, str);
	}

	public static T[] InplaceMap<T>(this T[] arr, Func<T, T> transformer)
	{
		for (int i = 0; i < arr.Length; i++)
		{
			arr[i] = transformer(arr[i]);
		}
		return arr;
	}

	public static T[] InplaceMap<T>(this T[] arr, Func<int, T, T> transformer)
	{
		for (int i = 0; i < arr.Length; i++)
		{
			arr[i] = transformer(i, arr[i]);
		}
		return arr;
	}

	public static T[] InplaceShuffle<T>(this T[] order, Func<int, int> randomizer)
	{
		for (int i = 0; i < order.Length; i++)
		{
			int arg = order.Length - i;
			int num = randomizer(arg);
			T val = order[i];
			T val2 = order[i + num];
			order[i] = val2;
			order[i + num] = val;
		}
		return order;
	}

	public static T[] Fill<T>(this T[] array, T value)
	{
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = value;
		}
		return array;
	}

	public static T[] Fill<T>(this T[] array, Func<T> value)
	{
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = value();
		}
		return array;
	}

	public static T[,] Fill<T>(this T[,] array, T value)
	{
		for (int i = 0; i < array.GetLength(0); i++)
		{
			for (int j = 0; j < array.GetLength(1); j++)
			{
				array[i, j] = value;
			}
		}
		return array;
	}

	public static IList<T> InplaceShuffle<T>(this IList<T> order, Func<int, int> randomizer)
	{
		for (int i = 0; i < order.Count; i++)
		{
			int arg = order.Count - i;
			int num = randomizer(arg);
			T value = order[i];
			T value2 = order[i + num];
			order[i] = value2;
			order[i + num] = value;
		}
		return order;
	}

	public static IList<T> BucketShuffle<T, K>(this IList<T> list, Func<int, int> randomizer, Func<T, K> bucket) where K : IComparable
	{
		IList<T> order = list.ToList().InplaceShuffle(randomizer);
		return list.OrderBy(bucket).ThenBy((T x) => order.IndexOf(x)).ToList();
	}

	public static float[] Subtract(this float[] a, float[] b)
	{
		float[] array = new float[Math.Min(a.Length, b.Length)];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = a[i] - b[i];
		}
		return array;
	}

	public static float[] Add(this float[] a, float[] b)
	{
		float[] array = new float[Math.Min(a.Length, b.Length)];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = a[i] + b[i];
		}
		return array;
	}

	public static float[][] Add(this float[][] a, float[][] b)
	{
		float[][] array = new float[Math.Min(a.Length, b.Length)][];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = a[i].Add(b[i]);
		}
		return array;
	}

	public static float[][] IAdd(this float[][] a, float[][] b)
	{
		for (int i = 0; i < a.Length; i++)
		{
			for (int j = 0; j < a[i].Length; j++)
			{
				a[i][j] += b[i][j];
			}
		}
		return a;
	}

	public static float[][] IDiv(this float[][] a, float b)
	{
		for (int i = 0; i < a.Length; i++)
		{
			for (int j = 0; j < a[i].Length; j++)
			{
				a[i][j] /= b;
			}
		}
		return a;
	}

	public static float[][,] IAdd(this float[][,] a, float[][,] b)
	{
		for (int i = 0; i < a.Length; i++)
		{
			for (int j = 0; j < a[i].GetLength(0); j++)
			{
				for (int k = 0; k < a[i].GetLength(1); k++)
				{
					a[i][j, k] += b[i][j, k];
				}
			}
		}
		return a;
	}

	public static float[][,] IDiv(this float[][,] a, float b)
	{
		for (int i = 0; i < a.Length; i++)
		{
			for (int j = 0; j < a[i].GetLength(0); j++)
			{
				for (int k = 0; k < a[i].GetLength(1); k++)
				{
					a[i][j, k] /= b;
				}
			}
		}
		return a;
	}

	public static float[] Normalized(this float[] a)
	{
		if (a.Length == 0)
		{
			return new float[0];
		}
		float num = 0f;
		for (int i = 0; i < a.Length; i++)
		{
			num += a[i] * a[i];
		}
		float num2 = Mathf.Sqrt(num);
		float[] array = new float[a.Length];
		for (int j = 0; j < array.Length; j++)
		{
			array[j] = a[j] / num2;
		}
		return array;
	}

	public static float[] Mul(this float[] a, float scalar)
	{
		float[] array = new float[a.Length];
		for (int i = 0; i < a.Length; i++)
		{
			array[i] = a[i] * scalar;
		}
		return array;
	}

	public static float[] Mul(this float[] a, float[] b)
	{
		float[] array = new float[Math.Min(a.Length, b.Length)];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = a[i] * b[i];
		}
		return array;
	}

	public static float[] IMul(this float[] a, float[] b)
	{
		int num = Math.Min(a.Length, b.Length);
		for (int i = 0; i < num; i++)
		{
			a[i] *= b[i];
		}
		return a;
	}

	public static float SquareMagnitude(this float[] a)
	{
		if (a.Length == 0)
		{
			return 0f;
		}
		float num = 0f;
		for (int i = 0; i < a.Length; i++)
		{
			num += a[i] * a[i];
		}
		return num;
	}

	public static float[] AsFloatArray(this Vector3 a)
	{
		return new float[3] { a.x, a.y, a.z };
	}

	public static Vector3 AsVector3(this float[] a)
	{
		if (a.Length == 3)
		{
			return new Vector3(a[0], a[1], a[2]);
		}
		return Vector3.zero;
	}

	public static float[] AsFloatArray(this Quaternion a)
	{
		return new float[4] { a.x, a.y, a.z, a.w };
	}

	public static Quaternion AsQuaternion(this float[] a)
	{
		if (a.Length == 4)
		{
			return new Quaternion(a[0], a[1], a[2], a[3]);
		}
		return Quaternion.identity;
	}

	public static void AddRange<K, T>(this IDictionary<K, T> dict, IEnumerable<KeyValuePair<K, T>> other)
	{
		other.ForEach(delegate(KeyValuePair<K, T> kv)
		{
			dict.Add(kv.Key, kv.Value);
		});
	}

	public static string TrimToNull(this string value)
	{
		if (value == null)
		{
			return null;
		}
		value = value.Trim();
		if (!(value == ""))
		{
			return value;
		}
		return null;
	}
}
