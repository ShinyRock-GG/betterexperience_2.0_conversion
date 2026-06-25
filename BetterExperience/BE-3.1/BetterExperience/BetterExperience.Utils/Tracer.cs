using UnityEngine;

namespace BetterExperience.Utils;

public class Tracer
{
	public static void DrawRay(Vector3 start, Vector3 dir, Color color = default(Color), float duration = 0.2f)
	{
		if (color == default(Color))
		{
			color = Color.magenta;
		}
		DrawLine(start, start + dir, color, duration);
	}

	public static void DrawLine(Vector3 start, Vector3 end, float duration = 0.2f)
	{
		DrawLine(start, end, Color.red, duration);
	}

	public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0.2f)
	{
		GameObject gameObject = new GameObject();
		gameObject.transform.position = start;
		gameObject.AddComponent<LineRenderer>();
		LineRenderer component = gameObject.GetComponent<LineRenderer>();
		component.material = new Material(Shader.Find("Sprites/Default"));
		component.startColor = color;
		component.endColor = color;
		component.startWidth = 0.001f;
		component.endWidth = 0.001f;
		component.SetPosition(0, start);
		component.SetPosition(1, end);
		component.alignment = LineAlignment.View;
		Object.Destroy(gameObject, duration);
	}

	internal static void DrawWireBox(Transform transform, Bounds bounds)
	{
		Vector3 vector = transform.TransformPoint(bounds.center + bounds.extents);
		Vector3 start = transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1f, 1f, 1f)));
		Vector3 vector2 = transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(1f, 1f, -1f)));
		Vector3 vector3 = transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1f, 1f, -1f)));
		Vector3 vector4 = transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(1f, -1f, 1f)));
		Vector3 vector5 = transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1f, -1f, 1f)));
		Vector3 end = transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(1f, -1f, -1f)));
		Vector3 vector6 = transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1f, -1f, -1f)));
		DrawLine(start, vector);
		DrawLine(vector5, vector4);
		DrawLine(vector3, vector2);
		DrawLine(vector6, end);
		DrawLine(start, vector3);
		DrawLine(vector, vector2);
		DrawLine(vector5, vector6);
		DrawLine(vector4, end);
		DrawLine(start, vector5);
		DrawLine(vector, vector4);
		DrawLine(vector3, vector6);
		DrawLine(vector2, end);
	}

	public static void DrawTransform(Transform t)
	{
		DrawRay(t.position, t.up, Color.red);
		DrawRay(t.position, t.right, Color.green);
		DrawRay(t.position, t.forward, Color.blue);
	}
}
