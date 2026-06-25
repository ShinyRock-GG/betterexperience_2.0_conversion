using BetterExperience.Wrappers.Physics;
using UnityEngine;

namespace BetterExperience.Features.BetterHand;

internal class GripSolver
{
	private Logger logger = new Logger
	{
		Prefix = "GripSolver"
	};

	private PhysicalPuppet Puppet;

	private FingerAnimator[] fingers;

	public GripTarget GripTarget { get; private set; }

	public int FingerCount => fingers.Length;

	public GripSolver(PhysicalPuppet puppet, bool allowGripUpdate)
	{
		Puppet = puppet;
		Finger[] array = new Finger[5]
		{
			new Finger(Puppet, HumanBodyBones.RightIndexDistal, HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexProximal),
			new Finger(Puppet, HumanBodyBones.RightMiddleDistal, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleProximal),
			new Finger(Puppet, HumanBodyBones.RightRingDistal, HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingProximal),
			new Finger(Puppet, HumanBodyBones.RightLittleDistal, HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleProximal),
			new Finger(Puppet, HumanBodyBones.RightThumbDistal, HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbProximal)
		};
		fingers = new FingerAnimator[array.Length];
		GripTarget = new GripTarget(array.Length);
		for (int i = 0; i < array.Length; i++)
		{
			GripTarget.InitFinger(i, array[i].Transforms);
			fingers[i] = new FingerAnimator(array[i], GripTarget.TargetPose[i], GripTarget.TargetCompletion[i]);
			fingers[i].AllowGripUpdate = allowGripUpdate;
		}
	}

	internal void RequestPose(int fingerIdx, float[] forwardAngles, float sideAngle, bool checkCollision)
	{
		fingers[fingerIdx].RequestPose(forwardAngles, sideAngle, checkCollision);
	}

	internal void CaptureBindingPose()
	{
		for (int i = 0; i < fingers.Length; i++)
		{
			fingers[i].ResetBindingPoseNow();
			GripTarget.Bind(i);
		}
	}

	internal bool Update(float dt)
	{
		for (int num = fingers[0].Finger.Transforms.Length - 1; num >= 0; num--)
		{
			for (int i = 0; i < fingers.Length; i++)
			{
				if (GripTarget.IsPartCompleted(num + 1))
				{
					fingers[i].Update(num, dt);
				}
			}
		}
		GripTarget.Update(dt);
		bool flag = true;
		for (int j = 0; j < GripTarget.TargetCompletion.Length; j++)
		{
			for (int k = 0; k < GripTarget.TargetCompletion[j].Length; k++)
			{
				GripTarget.TargetCompletion[j][k] &= !fingers[j].IsAnimatorActive(k);
				flag &= GripTarget.TargetCompletion[j][k];
			}
		}
		return flag;
	}

	internal void Apply()
	{
		GripTarget.Apply();
	}
}
