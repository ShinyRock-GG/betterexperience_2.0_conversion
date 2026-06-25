namespace BetterExperience.CustomScene.Characters;

internal class AnimSystems
{
	public AnimatedArmature AnimatedArmature { get; private set; }

	public AnimatedFace AnimatedFace { get; private set; }

	public AnimSystems(AnimatedArmature animatedArmature, AnimatedFace animatedFace)
	{
		AnimatedArmature = animatedArmature;
		AnimatedFace = animatedFace;
	}

	internal void Bind()
	{
		AnimatedArmature.Bind();
	}

	internal void BeforeUpdate()
	{
		AnimatedArmature.BeforeUpdate();
		AnimatedFace.BeforeUpdate();
	}

	internal void Apply(ExtensibleAnimator.AnimationClipState primaryState, float dt)
	{
		AnimatedArmature.Apply(primaryState, dt);
		AnimatedFace.Apply(primaryState, dt);
	}

	internal void Initialize(ExtensibleAnimator.AnimationClipState state)
	{
		AnimatedArmature.Initialize(state);
		AnimatedFace.Initialize(state);
	}

	internal void Update(ExtensibleAnimator.AnimationClipState state, float dt0)
	{
		AnimatedArmature.Update(state, dt0);
		AnimatedFace.Update(state, dt0);
	}

	internal void OnRollover(ExtensibleAnimator.AnimationClipState state)
	{
		AnimatedArmature.OnRollover(state);
	}
}
