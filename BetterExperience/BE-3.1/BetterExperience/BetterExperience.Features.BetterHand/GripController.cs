using Assets;
using BetterExperience.GameScopes;
using BetterExperience.Wrappers.Physics;
using UnityEngine;

namespace BetterExperience.Features.BetterHand;

internal class GripController : SessionService, IGripController
{
	private GripSolver solver;

	private const float GRIP_TARGET_ANGLE = -90f;

	private bool hasBindingPose;

	private bool bindingPoseResetRequired;

	public PhysicalPuppet Puppet { get; private set; }

	public GripPose CurrentPose { get; private set; }

	public bool TransitionComplete { get; private set; } = true;

	public bool PoserEnabled { get; set; } = true;

	public bool HasBindingPose => hasBindingPose;

	public bool AllowGripUpdate { get; set; } = true;

	public GripController()
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
			for (int i = 0; i < solver.FingerCount; i++)
			{
				solver.RequestPose(i, new float[3], 0f, checkCollision: false);
			}
			break;
		}
		case GripPose.FingersDown:
		{
			float[] forwardAngles2 = new float[3] { -11.25f, -15f, -30f };
			float[] forwardAngles3 = new float[3] { -22.5f, -30f, -60f };
			solver.RequestPose(0, forwardAngles3, -5f, checkCollision: true);
			solver.RequestPose(1, forwardAngles3, 0f, checkCollision: true);
			solver.RequestPose(2, forwardAngles3, 5f, checkCollision: true);
			solver.RequestPose(3, forwardAngles3, 10f, checkCollision: true);
			solver.RequestPose(4, forwardAngles2, 0f, checkCollision: true);
			break;
		}
		case GripPose.FingersUp:
		{
			float[] forwardAngles = new float[3] { 0f, 0f, 90f };
			solver.RequestPose(0, forwardAngles, -5f, checkCollision: false);
			solver.RequestPose(1, forwardAngles, 0f, checkCollision: false);
			solver.RequestPose(2, forwardAngles, 5f, checkCollision: false);
			solver.RequestPose(3, forwardAngles, 10f, checkCollision: false);
			solver.RequestPose(4, forwardAngles, 0f, checkCollision: false);
			break;
		}
		}
		TransitionComplete = false;
	}

	public override void OnStart()
	{
		base.OnStart();
		Puppet = new PhysicalPuppet(base.Session.Player.GameObject);
		solver = new GripSolver(Puppet, AllowGripUpdate);
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
			solver.Apply();
		}
	}

	public void ResetIdlePose()
	{
		bindingPoseResetRequired = true;
	}

	private void Ik_onAllIKsUpdated(IIKUpdater obj)
	{
		if (PoserEnabled)
		{
			if (bindingPoseResetRequired)
			{
				solver.CaptureBindingPose();
				bindingPoseResetRequired = false;
				hasBindingPose = true;
			}
			if (hasBindingPose)
			{
				TransitionComplete = solver.Update(Time.deltaTime);
			}
		}
	}
}
