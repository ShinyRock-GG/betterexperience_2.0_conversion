using Assets;
using BetterExperience.GameScopes;
using BetterExperience.Wrappers.Physics;
using UnityEngine;

namespace BetterExperience.Features.BetterHand.Legacy;

internal class LegacyGripController : SessionService, IGripController
{
	private FingerAnimator[] fingers;

	private const float GRIP_TARGET_ANGLE = -90f;

	private bool hasBindingPose;

	private bool bindingPoseResetRequired;

	public PhysicalPuppet Puppet { get; private set; }

	public GripPose CurrentPose { get; private set; }

	public bool TransitionComplete { get; private set; } = true;

	public bool PoserEnabled { get; set; } = true;

	public bool HasBindingPose => hasBindingPose;

	public bool AllowGripUpdate { get; set; }

	public LegacyGripController()
	{
		logger = new Logger();
	}

	public void SetPose(GripPose pose, bool force = false)
	{
		if (!(CurrentPose != pose || force))
		{
			return;
		}
		CurrentPose = pose;
		switch (pose)
		{
		case GripPose.Idle:
		{
			for (int k = 0; k < fingers.Length; k++)
			{
				fingers[k].RequestPose(new float[3], 0f, resistable: false);
			}
			break;
		}
		case GripPose.FingersDown:
		{
			for (int j = 0; j < fingers.Length; j++)
			{
				fingers[j].SetTransitionResistable(v: true);
				fingers[j].RequestForwardRotation(new float[3] { -22.5f, -30f, -60f });
			}
			fingers[0].RequestSideRotation(-5f);
			fingers[1].RequestSideRotation(0f);
			fingers[2].RequestSideRotation(5f);
			fingers[3].RequestSideRotation(10f);
			fingers[4].RequestSideRotation(0f);
			break;
		}
		case GripPose.FingersUp:
		{
			for (int i = 0; i < fingers.Length; i++)
			{
				fingers[i].SetTransitionResistable(v: true);
				fingers[i].RequestForwardRotation(new float[3] { 0f, 0f, 90f });
			}
			fingers[0].RequestSideRotation(-5f);
			fingers[1].RequestSideRotation(0f);
			fingers[2].RequestSideRotation(5f);
			fingers[3].RequestSideRotation(10f);
			fingers[4].RequestSideRotation(0f);
			break;
		}
		}
		TransitionComplete = false;
	}

	public override void OnStart()
	{
		base.OnStart();
		Puppet = new PhysicalPuppet(base.Session.Player.GameObject);
		Finger[] array = new Finger[5]
		{
			new Finger(Puppet, HumanBodyBones.RightIndexDistal, HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexProximal),
			new Finger(Puppet, HumanBodyBones.RightMiddleDistal, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleProximal),
			new Finger(Puppet, HumanBodyBones.RightRingDistal, HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingProximal),
			new Finger(Puppet, HumanBodyBones.RightLittleDistal, HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleProximal),
			new Finger(Puppet, HumanBodyBones.RightThumbDistal, HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbProximal)
		};
		fingers = new FingerAnimator[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			fingers[i] = new FingerAnimator(array[i]);
		}
		IIKUpdater ik = base.Session.Player.GameObject.GetComponentInChildren<IIKUpdater>();
		ik.onPhysicsIKUpdated += ApplyPose;
		ik.onAllIKsUpdated += Ik_onAllIKsUpdated;
		base.Scope.OnDispose += delegate
		{
			ik.onAllIKsUpdated -= Ik_onAllIKsUpdated;
		};
		base.Scope.OnDispose += delegate
		{
			ik.onPhysicsIKUpdated -= ApplyPose;
		};
	}

	private void ApplyPose(IIKUpdater obj)
	{
		if (hasBindingPose && !PoserEnabled)
		{
			hasBindingPose = false;
		}
		if (PoserEnabled && hasBindingPose)
		{
			for (int i = 0; i < fingers.Length; i++)
			{
				fingers[i].ApplyTransforms();
			}
		}
	}

	public void ResetIdlePose()
	{
		bindingPoseResetRequired = true;
	}

	private void Ik_onAllIKsUpdated(IIKUpdater obj)
	{
		if (!PoserEnabled)
		{
			return;
		}
		if (bindingPoseResetRequired)
		{
			for (int i = 0; i < fingers.Length; i++)
			{
				fingers[i].ResetBindingPoseNow();
			}
			bindingPoseResetRequired = false;
			hasBindingPose = true;
		}
		if (!hasBindingPose)
		{
			return;
		}
		int minupdate = 0;
		bool flag = true;
		for (int num = fingers[0].Finger.Transforms.Length - 1; num >= 0; num--)
		{
			for (int j = 0; j < fingers.Length; j++)
			{
				fingers[j].Update(ref minupdate, num);
				flag &= fingers[j].completed[num];
			}
		}
		TransitionComplete = flag;
	}
}
