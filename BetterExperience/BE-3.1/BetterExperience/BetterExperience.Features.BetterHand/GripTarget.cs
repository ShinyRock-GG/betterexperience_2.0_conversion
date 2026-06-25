using UnityEngine;

namespace BetterExperience.Features.BetterHand;

internal class GripTarget
{
	public Transform[][] Fingers { get; private set; }

	public Quaternion[][] BindingPose { get; private set; }

	public Quaternion[][] TargetPose { get; private set; }

	public Quaternion[][] CurrentPose { get; private set; }

	public bool[][] TargetCompletion { get; private set; }

	public GripTarget(int fingerCount)
	{
		Fingers = new Transform[fingerCount][];
		BindingPose = new Quaternion[fingerCount][];
		TargetPose = new Quaternion[fingerCount][];
		CurrentPose = new Quaternion[fingerCount][];
		TargetCompletion = new bool[fingerCount][];
	}

	internal void Bind(int i)
	{
		for (int j = 0; j < Fingers[i].Length; j++)
		{
			BindingPose[i][j] = Fingers[i][j].localRotation;
			TargetPose[i][j] = BindingPose[i][j];
			CurrentPose[i][j] = BindingPose[i][j];
		}
	}

	internal void InitFinger(int i, Transform[] transforms)
	{
		Fingers[i] = transforms;
		BindingPose[i] = new Quaternion[transforms.Length];
		TargetPose[i] = new Quaternion[transforms.Length];
		CurrentPose[i] = new Quaternion[transforms.Length];
		TargetCompletion[i] = new bool[transforms.Length];
	}

	public bool Update(float dt)
	{
		bool result = false;
		for (int num = Fingers[0].Length - 1; num >= 0; num--)
		{
			for (int i = 0; i < Fingers.Length; i++)
			{
				Quaternion quaternion = CurrentPose[i][num];
				Quaternion quaternion2 = TargetPose[i][num];
				if (quaternion != quaternion2)
				{
					CurrentPose[i][num] = Quaternion.RotateTowards(quaternion, quaternion2, dt * 200f);
					result = true;
					TargetCompletion[i][num] = false;
				}
				else
				{
					TargetCompletion[i][num] = true;
				}
			}
		}
		return result;
	}

	internal void Apply()
	{
		for (int i = 0; i < Fingers.Length; i++)
		{
			for (int j = 0; j < Fingers[i].Length; j++)
			{
				Fingers[i][j].localRotation = CurrentPose[i][j];
			}
		}
	}

	internal bool IsPartCompleted(int j)
	{
		if (j >= TargetCompletion[0].Length - 1)
		{
			return true;
		}
		for (int i = 0; i < TargetCompletion.Length; i++)
		{
			if (!TargetCompletion[i][j])
			{
				return false;
			}
		}
		return true;
	}
}
