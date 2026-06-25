namespace BetterExperience.Features.BetterHand;

internal interface IGripController
{
	bool AllowGripUpdate { get; set; }

	GripPose CurrentPose { get; }

	bool TransitionComplete { get; }

	bool PoserEnabled { get; set; }

	void SetPose(GripPose pose, bool forcePose);

	void ResetIdlePose();
}
