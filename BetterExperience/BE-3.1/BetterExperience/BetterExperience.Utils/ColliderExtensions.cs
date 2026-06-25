using System.Linq;
using UnityEngine;

namespace BetterExperience.Utils;

public static class ColliderExtensions
{
	private static readonly Vector3[] capsuleOrientations = new Vector3[3]
	{
		new Vector3(1f, 0f, 0f),
		new Vector3(0f, 1f, 0f),
		new Vector3(0f, 0f, 1f)
	};

	public static bool QueryOverlaps(this CapsuleCollider capsule, Transform transform, out Collider[] result, int layerMask)
	{
		Vector3 vector = capsuleOrientations[capsule.direction];
		float num = capsule.height / 2f - capsule.radius;
		Vector3 position = capsule.center + vector * num;
		Vector3 position2 = capsule.center - vector * num;
		position = transform.TransformPoint(position);
		position2 = transform.TransformPoint(position2);
		Vector3 r = transform.TransformVector(capsule.radius, capsule.radius, capsule.radius);
		float radius = (from xyz in Enumerable.Range(0, 3)
			select (xyz != capsule.direction) ? r[xyz] : 0f).Select(Mathf.Abs).Max();
		result = Physics.OverlapCapsule(position, position2, radius, layerMask);
		return result.Length != 0;
	}

	public static bool QueryOverlaps(this BoxCollider box, Transform transform, out Collider[] result, int layerMask)
	{
		Vector3 center = transform.TransformPoint(box.center);
		Vector3 halfExtents = transform.TransformVector(box.size) / 2f;
		result = Physics.OverlapBox(center, halfExtents, transform.rotation, layerMask);
		return result.Length != 0;
	}

	public static bool QueryRaycast(this BoxCollider box, Transform transform, Vector3 direction, out RaycastHit result, int layerMask, float distance)
	{
		Vector3 center = transform.TransformPoint(box.center);
		Vector3 result2 = transform.TransformVector(box.size);
		result2 = UnityUtils.Abs(result2);
		box.enabled = false;
		try
		{
			return Physics.BoxCast(center, result2 / 2f, direction, out result, transform.rotation, distance, layerMask);
		}
		finally
		{
			box.enabled = true;
		}
	}
}
