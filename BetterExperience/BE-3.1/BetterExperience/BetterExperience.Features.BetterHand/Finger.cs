using System.Linq;
using BetterExperience.Wrappers.Physics;
using UnityEngine;

namespace BetterExperience.Features.BetterHand;

internal class Finger
{
	private CapsuleCollider[] colliders;

	private PhysicalPuppet puppet;

	private Vector3[] capsuleOrientations = new Vector3[3]
	{
		new Vector3(1f, 0f, 0f),
		new Vector3(0f, 1f, 0f),
		new Vector3(0f, 0f, 1f)
	};

	public Transform[] Transforms { get; private set; }

	public Quaternion[] startRotations { get; private set; }

	public Finger(PhysicalPuppet puppet, params HumanBodyBones[] bones)
	{
		this.puppet = puppet;
		Transforms = new Transform[bones.Length];
		colliders = new CapsuleCollider[bones.Length];
		startRotations = new Quaternion[bones.Length];
		for (int i = 0; i < bones.Length; i++)
		{
			Transforms[i] = puppet.GetBoneTransform(bones[i]);
			foreach (Collider item in puppet.ColliderByName(Transforms[i].name))
			{
				CapsuleCollider capsuleCollider = item as CapsuleCollider;
				if (capsuleCollider != null)
				{
					colliders[i] = capsuleCollider;
					break;
				}
			}
			startRotations[i] = Transforms[i].localRotation;
		}
	}

	public float RotateLocal(int index, Vector3 axis, float maxAngle, bool ignoreCollision, Quaternion additionalRotation)
	{
		Transform transform = Transforms[index];
		if (transform.localRotation != startRotations[index])
		{
			transform.localRotation = startRotations[index];
		}
		if (maxAngle == 0f)
		{
			return 0f;
		}
		Quaternion localRotation = transform.localRotation * additionalRotation;
		float num = 0f;
		if (TryRotateLocal(index, axis, maxAngle) || ignoreCollision)
		{
			return maxAngle;
		}
		transform.localRotation = localRotation;
		if (!TryRotateLocal(index, axis, 0f))
		{
			return 0f;
		}
		while (Mathf.Abs(maxAngle) > 0.1f)
		{
			float num2 = maxAngle / 2f;
			if (TryRotateLocal(index, axis, num2))
			{
				localRotation = transform.localRotation;
				maxAngle -= num2;
				num += num2;
				continue;
			}
			if ((double)num2 <= 0.1)
			{
				break;
			}
			maxAngle = num2;
			transform.localRotation = localRotation;
		}
		return num;
	}

	public bool TryRotateLocal(int index, Vector3 axis, float maxAngle)
	{
		Transforms[index].Rotate(axis, maxAngle, Space.Self);
		if (TestCollision(index))
		{
			return false;
		}
		return true;
	}

	public bool TestCollision(int index)
	{
		for (int i = 0; i <= index; i++)
		{
			if (TestCollision(colliders[i]))
			{
				return true;
			}
		}
		return false;
	}

	public bool TestCollision(CapsuleCollider capsule)
	{
		Vector3 vector = capsuleOrientations[capsule.direction];
		float num = capsule.height / 2f - capsule.radius;
		Vector3 position = capsule.center + vector * num;
		Vector3 position2 = capsule.center - vector * num;
		position = capsule.transform.TransformPoint(position);
		position2 = capsule.transform.TransformPoint(position2);
		Vector3 r = capsule.transform.TransformVector(capsule.radius, capsule.radius, capsule.radius);
		float radius = (from xyz in Enumerable.Range(0, 3)
			select (xyz != capsule.direction) ? r[xyz] : 0f).Select(Mathf.Abs).Max();
		Collider[] array = Physics.OverlapCapsule(position, position2, radius);
		if (array.Length != 0)
		{
			foreach (Collider collider in array)
			{
				if (collider is MeshCollider && !puppet.ContainsCollider(collider))
				{
					return true;
				}
			}
		}
		return false;
	}
}
