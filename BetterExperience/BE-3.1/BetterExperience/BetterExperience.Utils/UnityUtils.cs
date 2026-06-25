using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.SingletonesAndSystemasGlobales.AbstractLayer.Globales;
using BetterExperience.GameScopes;
using UnityEngine;

namespace BetterExperience.Utils;

public class UnityUtils
{
	public static Transform NewTransform(string name, Transform parent = null, ScopeSupport scope = null)
	{
		GameObject go = new GameObject();
		go.name = name;
		go.transform.parent = parent;
		if (scope != null)
		{
			scope.OnDispose += delegate
			{
				UnityEngine.Object.DestroyImmediate(go);
			};
		}
		if (parent == null)
		{
			go.hideFlags |= HideFlags.HideAndDontSave;
			UnityEngine.Object.DontDestroyOnLoad(go);
		}
		return go.transform;
	}

	public static T[] FindAndInitializeSingletonsOfType<T>()
	{
		Singleton[] array = UnityEngine.Object.FindObjectsOfType<Singleton>();
		List<T> list = new List<T>();
		Singleton[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			Type type = array2[i].GetType();
			Type type2 = FindGenericSuperClass(type, typeof(Singleton<>));
			if (type2 == null)
			{
				continue;
			}
			Type c = type2.GenericTypeArguments[0];
			if (!typeof(T).IsAssignableFrom(c))
			{
				continue;
			}
			PropertyInfo property = type2.GetProperty("instance");
			if (!(property == null))
			{
				if (property.GetValue(null) is T item)
				{
					list.Add(item);
					continue;
				}
				new Logger().Error("Unable to get instance of singleton {0}", type.Name);
			}
		}
		return list.ToArray();
	}

	internal static Vector3 Abs(Vector3 result)
	{
		return new Vector3(Mathf.Abs(result.x), Mathf.Abs(result.y), Mathf.Abs(result.z));
	}

	internal static Vector3 Truncate(Vector3 result, float prec)
	{
		return new Vector3((result.x > prec) ? result.x : 0f, (result.y > prec) ? result.y : 0f, (result.z > prec) ? result.z : 0f);
	}

	private static Type FindGenericSuperClass(Type t, Type genericBase)
	{
		if (t == typeof(object))
		{
			return null;
		}
		if (t.IsGenericType && t.GetGenericTypeDefinition() == genericBase)
		{
			return t;
		}
		return FindGenericSuperClass(t.BaseType, genericBase);
	}

	public static Vector3 ToEuler(Quaternion quaternion)
	{
		Vector3 rEuler = quaternion.eulerAngles;
		NormalizeEuler(ref rEuler);
		return rEuler;
	}

	public static void NormalizeEuler(ref Vector3 rEuler)
	{
		if (rEuler.x < -180f)
		{
			rEuler.x += 360f;
		}
		else if (rEuler.x > 180f)
		{
			rEuler.x -= 360f;
		}
		if (rEuler.y < -180f)
		{
			rEuler.y += 360f;
		}
		else if (rEuler.y > 180f)
		{
			rEuler.y -= 360f;
		}
		if (rEuler.z < -180f)
		{
			rEuler.z += 360f;
		}
		else if (rEuler.z > 180f)
		{
			rEuler.z -= 360f;
		}
	}

	public static float FromToAxisAngle(Vector3 from, Vector3 to, Vector3 normal)
	{
		Vector3 vector = Vector3.ProjectOnPlane(from, normal);
		Vector3 to2 = Vector3.ProjectOnPlane(to, normal);
		return Vector3.SignedAngle(vector, to2, normal);
	}

	public static bool AddNeighbourComponent<TFind, TAdd>(Transform transform, Action<TAdd> tuning) where TFind : Component where TAdd : Component
	{
		TFind[] componentsInChildren = transform.GetComponentsInChildren<TFind>();
		TFind[] array = componentsInChildren;
		for (int i = 0; i < array.Length; i++)
		{
			TAdd obj = array[i].transform.gameObject.AddComponent<TAdd>();
			tuning?.Invoke(obj);
		}
		return componentsInChildren.Length != 0;
	}

	public static bool DisplaceComponent<TFind, TDisplace>(Transform transform, Action<TFind, TDisplace> tuning) where TFind : Component where TDisplace : Component
	{
		TFind[] componentsInChildren = transform.GetComponentsInChildren<TFind>();
		TFind[] array = componentsInChildren;
		foreach (TFind val in array)
		{
			TDisplace arg = val.transform.gameObject.AddComponent<TDisplace>();
			tuning?.Invoke(val, arg);
			UnityEngine.Object.DestroyImmediate(val);
		}
		return componentsInChildren.Length != 0;
	}

	public static string GetNameInHierarchy(Transform t, Transform relativeTo)
	{
		List<string> list = new List<string>();
		while (t != null || t != relativeTo)
		{
			list.Insert(0, t.name);
			t = t.parent;
		}
		if (t == relativeTo)
		{
			return string.Join("/", list);
		}
		return null;
	}
}
