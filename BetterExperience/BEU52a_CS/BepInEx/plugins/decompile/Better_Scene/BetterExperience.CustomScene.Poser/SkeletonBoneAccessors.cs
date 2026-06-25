using System;
using System.Collections.Generic;
using Assets.Base.Bones.Gizmos.Runtime;
using UnityEngine;

namespace BetterExperience.CustomScene.Poser;

internal static class SkeletonBoneAccessors
{
	public class SkeletonBones : IBoneDisposition
	{
		private Transform[] bones;

		private Transform hipBone;

		private Transform rootMotionTransform;

		public GizmosDeSkeleton Skeleton { get; private set; }

		public Vector3 PostureOffset { get; set; }

		public Quaternion PostureRotation { get; set; }

		public int Count => bones.Length;

		public IReadOnlyList<Transform> Bones => bones;

		public Vector3 HipOffset
		{
			get
			{
				return hipBone.localPosition;
			}
			set
			{
				hipBone.localPosition = value;
			}
		}

		public Vector3 RootOffset
		{
			get
			{
				return Skeleton.rootBone.position - rootMotionTransform.TransformPoint(PostureOffset);
			}
			set
			{
				Skeleton.rootBone.position = rootMotionTransform.TransformPoint(PostureOffset);
			}
		}

		public Quaternion RootRotation
		{
			get
			{
				return Quaternion.Inverse(rootMotionTransform.rotation * PostureRotation) * Skeleton.rootBone.rotation;
			}
			set
			{
				Skeleton.rootBone.rotation = rootMotionTransform.rotation * PostureRotation;
			}
		}

		public SkeletonBones(GizmosDeSkeleton skeleton, Transform rootMotionTransform)
		{
			Skeleton = skeleton;
			bones = new Transform[skeleton.mainBones.Count - 1];
			for (int i = 1; i < skeleton.mainBones.Count; i++)
			{
				bones[i - 1] = skeleton.mainBones[i];
			}
			hipBone = skeleton.rootBone.FindDeepChild("CC_Base_Hip");
			this.rootMotionTransform = rootMotionTransform;
		}

		public Quaternion GetRotation(int index)
		{
			return bones[index].localRotation;
		}

		public void SetRotation(int index, Quaternion value)
		{
			bones[index].localRotation = value;
		}

		public bool IsHip(int index)
		{
			return bones[index] == hipBone;
		}

		public IBoneDisposition CreateBuffer()
		{
			return new InMemoryBones(Count);
		}
	}

	public class InMemoryBones : IBoneDisposition
	{
		private Quaternion[] rotations;

		public int Count => rotations.Length;

		public Vector3 HipOffset { get; set; }

		public Vector3 RootOffset { get; set; }

		public Quaternion RootRotation { get; set; }

		public InMemoryBones(int count)
		{
			rotations = new Quaternion[count];
		}

		public Quaternion GetRotation(int index)
		{
			return rotations[index];
		}

		public void SetRotation(int index, Quaternion value)
		{
			rotations[index] = value;
		}
	}

	internal static void CopyTo(IBoneDisposition from, IBoneDisposition to)
	{
		for (int i = 0; i < from.Count; i++)
		{
			to.SetRotation(i, from.GetRotation(i));
		}
		to.HipOffset = from.HipOffset;
		to.RootOffset = from.RootOffset;
		to.RootRotation = from.RootRotation;
	}

	internal static void CopyTo(IBoneDisposition from, IBoneDisposition to, Func<int, bool> predicate, float blend)
	{
		for (int i = 0; i < from.Count; i++)
		{
			if (predicate(i))
			{
				to.SetRotation(i, Quaternion.Lerp(to.GetRotation(i), from.GetRotation(i), blend));
			}
		}
	}
}
