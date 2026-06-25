namespace BetterExperience.CustomScene.Poser;

public interface IAnimationClipState
{
	PoseAnimationClip Clip { get; }

	bool Cyclic { get; }
}
