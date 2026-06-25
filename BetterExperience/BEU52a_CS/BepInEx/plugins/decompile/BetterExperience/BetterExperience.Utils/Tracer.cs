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
		GameObject myLine = new GameObject();
		myLine.transform.position = start;
		myLine.AddComponent<LineRenderer>();
		LineRenderer lr = myLine.GetComponent<LineRenderer>();
		lr.material = new Material(Shader.Find("Sprites/Default"));
		lr.startColor = color;
		lr.endColor = color;
		lr.startWidth = 0.001f;
		lr.endWidth = 0.001f;
		lr.SetPosition(0, start);
		lr.SetPosition(1, end);
		lr.alignment = LineAlignment.View;
		Object.Destroy(myLine, duration);
	}

	public static void DrawWireBox(Transform transform, Bounds bounds)
	{
		Vector3 topFrontRight = transform.TransformPoint(bounds.center + bounds.extents);
		Vector3 topFrontLeft = transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1f, 1f, 1f)));
		Vector3 topBackRight = transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(1f, 1f, -1f)));
		Vector3 topBackLeft = transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1f, 1f, -1f)));
		Vector3 bottomFrontRight = transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(1f, -1f, 1f)));
		Vector3 bottomFrontLeft = transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1f, -1f, 1f)));
		Vector3 bottomBackRight = transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(1f, -1f, -1f)));
		Vector3 bottomBackLeft = transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1f, -1f, -1f)));
		DrawLine(topFrontLeft, topFrontRight);
		DrawLine(bottomFrontLeft, bottomFrontRight);
		DrawLine(topBackLeft, topBackRight);
		DrawLine(bottomBackLeft, bottomBackRight);
		DrawLine(topFrontLeft, topBackLeft);
		DrawLine(topFrontRight, topBackRight);
		DrawLine(bottomFrontLeft, bottomBackLeft);
		DrawLine(bottomFrontRight, bottomBackRight);
		DrawLine(topFrontLeft, bottomFrontLeft);
		DrawLine(topFrontRight, bottomFrontRight);
		DrawLine(topBackLeft, bottomBackLeft);
		DrawLine(topBackRight, bottomBackRight);
	}

	public static void DrawTransform(Transform t)
	{
		DrawRay(t.position, t.up, Color.red);
		DrawRay(t.position, t.right, Color.green);
		DrawRay(t.position, t.forward, Color.blue);
	}
}
