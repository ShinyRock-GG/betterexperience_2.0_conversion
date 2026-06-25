using Assets;
using BetterExperience.Utils;
using UnityEngine;

namespace BetterExperience.Features.BetterHand.Legacy;

internal class FingerAnimator
{
	private const float FORWARD_DURATION = 0.2f;

	private FloatAnimator[] forwardAnimators;

	private FloatAnimator horizontalAnimator;

	private Quaternion[] boneRotation;

	private Quaternion[] targetRotation;

	private float[] forwardAngle;

	private bool resistable = true;

	private bool isActive = true;

	public bool[] completed;

	private Vector3 fixedPosition;

	public Finger Finger { get; private set; }

	public bool Active => isActive;

	public FingerAnimator(Finger finger)
	{
		Finger = finger;
		forwardAnimators = new FloatAnimator[finger.Transforms.Length];
		for (int i = 0; i < forwardAnimators.Length; i++)
		{
			forwardAnimators[i] = new FloatAnimator(0f, 0f, 0.2f);
		}
		horizontalAnimator = new FloatAnimator(0f, 0f, 1f);
		boneRotation = new Quaternion[finger.Transforms.Length];
		targetRotation = new Quaternion[finger.Transforms.Length];
		for (int j = 0; j < boneRotation.Length; j++)
		{
			boneRotation[j] = finger.Transforms[j].localRotation;
			targetRotation[j] = boneRotation[j];
		}
		forwardAngle = new float[finger.Transforms.Length];
		completed = new bool[finger.Transforms.Length];
	}

	public void Update(ref int minupdate, int i)
	{
		float deltaTime = Time.deltaTime;
		bool ignoreCollision = !resistable;
		bool active = horizontalAnimator.Active;
		Quaternion quaternion = Quaternion.identity;
		if (i == 2)
		{
			if (horizontalAnimator.Active)
			{
				minupdate = 2;
			}
			horizontalAnimator.Update(deltaTime);
			Finger.RotateLocal(2, Vector3.right, horizontalAnimator.Value, ignoreCollision: true);
			quaternion = Finger.startRotations[i] * Quaternion.Inverse(Finger.Transforms[i].localRotation);
			if (completed[i] && HandMoved())
			{
				completed[i] = false;
			}
		}
		if (i >= minupdate && !completed[i])
		{
			bool active2 = forwardAnimators[i].Active;
			forwardAnimators[i].Update(deltaTime);
			if (active2)
			{
				if (Finger.RotateLocal(i, Vector3.forward, forwardAnimators[i].Value, ignoreCollision) == 0f && forwardAnimators[i].Value != 0f)
				{
					forwardAnimators[i].Update(0f - deltaTime);
					forwardAnimators[i].Stop();
				}
				else
				{
					forwardAngle[i] = forwardAnimators[i].Value;
					targetRotation[i] = Finger.Transforms[i].localRotation * quaternion;
				}
			}
			boneRotation[i] = Quaternion.RotateTowards(boneRotation[i], targetRotation[i], deltaTime * 200f * 1.5f);
			bool flag = boneRotation[i] == targetRotation[i] && !forwardAnimators[i].Active;
			completed[i] = flag;
			if (!completed[i])
			{
				minupdate = i;
			}
		}
		Finger.Transforms[i].localRotation = Finger.startRotations[i];
		active |= !completed[i];
		if (isActive && !active)
		{
			fixedPosition = Finger.Transforms[2].position;
		}
		isActive = active;
	}

	private bool HandMoved()
	{
		return ExtendedMonoBehaviour.AlmostEqual(fixedPosition, Finger.Transforms[2].position, 0.1f);
	}

	public void RequestForwardRotation(float[] angles)
	{
		for (int i = 0; i < angles.Length; i++)
		{
			float to = forwardAnimators[i].To;
			forwardAnimators[i] = new FloatAnimator(to, angles[i], 0.2f);
		}
		isActive = true;
		completed.Fill(value: false);
	}

	public void RequestSideRotation(float angle)
	{
		float to = horizontalAnimator.To;
		horizontalAnimator = new FloatAnimator(to, angle, 0.1f);
		isActive = true;
		completed.Fill(value: false);
	}

	public void RequestPose(float[] forward, float side, bool resistable)
	{
		RequestForwardRotation(forward);
		RequestSideRotation(side);
		this.resistable = resistable;
	}

	internal void SetTransitionResistable(bool v)
	{
		resistable = v;
	}

	public void ResetBindingPoseNow()
	{
		for (int i = 0; i < Finger.Transforms.Length; i++)
		{
			Finger.startRotations[i] = Finger.Transforms[i].localRotation;
			boneRotation[i] = Finger.startRotations[i];
		}
	}

	internal void ToBindingPose()
	{
		for (int i = 0; i < Finger.Transforms.Length; i++)
		{
			Finger.Transforms[i].localRotation = Finger.startRotations[i];
		}
	}

	public void ApplyTransforms()
	{
		for (int i = 0; i < Finger.Transforms.Length; i++)
		{
			Finger.Transforms[i].localRotation = boneRotation[i];
		}
	}
}
