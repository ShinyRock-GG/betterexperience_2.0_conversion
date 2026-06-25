using System.Collections.Generic;
using System.Linq;
using BetterExperience.CustomScene.Poser;
using BetterExperience.GameScopes;
using BetterExperience.Utils;
using BetterExperience.Wrappers.Characters;
using UnityEngine;

namespace BetterExperience.CustomScene.Characters;

internal class ExtensibleAnimator : IBoneAnimator
{
	public class PrivateAnimatorState
	{
		public AnimationLayerState[] layers { get; private set; }

		public AnimationLayerState additiveLayer { get; private set; }

		public PrivateAnimatorState(AnimationLayerState[] layers, AnimationLayerState additiveLayer)
		{
			this.layers = layers;
			this.additiveLayer = additiveLayer;
		}
	}

	public class AnimationLayerState
	{
		public AnimationClipState SourceState { get; set; }

		public AnimationClipState TargetState { get; set; }

		public bool IsPlaying
		{
			get
			{
				if (TargetState == null)
				{
					return false;
				}
				return TargetState.IsPlaying;
			}
		}

		public AnimatorLayer[] AdditiveLayers { get; internal set; }
	}

	public class AnimationClipState : IAnimationClipState
	{
		internal BoneMuscleData seqMuscles;

		internal IKTargetData seqIk;

		public SeqClip seqClip { get; private set; }

		public float seqTime { get; set; }

		public bool seqInitialized { get; set; }

		public PoseAnimationClip Clip => seqClip.source;

		public bool IsPlaying { get; internal set; }

		public int Cycles { get; set; }

		public IBoneDisposition Disposition { get; private set; }

		public AnimationCompletionMode CompletionMode { get; set; }

		public float Weight { get; set; } = 1f;

		public float BlendingTime { get; set; }

		public AnimationStage Stage { get; set; }

		public bool Cyclic
		{
			get
			{
				if (!Clip.Cyclic)
				{
					return CompletionMode == AnimationCompletionMode.Loop;
				}
				return true;
			}
		}

		public AnimationClipState(SeqClip clip, IReadOnlyList<Transform> bones, IBoneDisposition boneDisposition)
		{
			IsPlaying = true;
			Disposition = boneDisposition;
			seqClip = clip;
		}
	}

	public class ArmatureBones : IBoneDisposition
	{
		private Transform[] bones;

		private Transform hipBone;

		private Transform rootMotionTransform;

		public Armature Skeleton { get; private set; }

		public Vector3 PostureOffset { get; set; }

		public Quaternion PostureRotation { get; set; }

		public int Count => bones.Length;

		public IReadOnlyList<Transform> Bones => bones;

		public Transform RootMotionTransform => rootMotionTransform;

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
				return rootMotionTransform.InverseTransformPoint(Skeleton.RootBone.transform.position);
			}
			set
			{
				Skeleton.RootBone.transform.position = rootMotionTransform.TransformPoint(value);
			}
		}

		public Quaternion RootRotation
		{
			get
			{
				return Quaternion.Inverse(rootMotionTransform.rotation) * Skeleton.RootBone.transform.rotation;
			}
			set
			{
				Skeleton.RootBone.transform.rotation = rootMotionTransform.rotation * value;
			}
		}

		public ArmatureBones(Armature skeleton, Transform rootMotionTransform)
		{
			Skeleton = skeleton;
			bones = new Transform[skeleton.Bones.Count - 1];
			for (int i = 1; i < skeleton.Bones.Count; i++)
			{
				bones[i - 1] = skeleton.Bones[i];
			}
			hipBone = skeleton.RootBone.transform.FindDeepChild("CC_Base_Hip");
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
			return new SkeletonBoneAccessors.InMemoryBones(Count);
		}
	}

	protected Logger logger = new Logger
	{
		Prefix = "[Armature Animator] "
	};

	private PrivateAnimatorState state;

	private AnimationLayerState[] layers;

	private AnimationLayerState additiveLayer;

	private AnimatorLayer[] layerParents = new AnimatorLayer[7];

	private List<AnimationClipState> activeStates = new List<AnimationClipState>();

	private AnimSystems animationSystems;

	private Dictionary<string, (PoseAnimationClip, SeqClip)> clipcache = new Dictionary<string, (PoseAnimationClip, SeqClip)>();

	public Armature Armature { get; }

	public Observable<AnimatorLayer> OnLayerClipComplete { get; } = new Observable<AnimatorLayer>
	{
		Buffered = true
	};

	public Observable<AnimatorFrameChangedEvent> OnClipFrameChanged { get; } = new Observable<AnimatorFrameChangedEvent>
	{
		Buffered = true
	};

	public Observable<string, DynamicIKTarget, float> OnIKTargetChange => animatedArmature.OnIKTargetChange;

	public Observable<BoneMuscleData> OnMusclesChange => animatedArmature.OnMusclesChange;

	public Vector3 PostureOffset
	{
		get
		{
			return animatedArmature.PostureOffset;
		}
		set
		{
			animatedArmature.PostureOffset = value;
		}
	}

	public Quaternion PostureRotation
	{
		get
		{
			return animatedArmature.PostureRotation;
		}
		set
		{
			animatedArmature.PostureRotation = value;
		}
	}

	public Vector3 RootMotionOffset
	{
		get
		{
			return animatedArmature.RootMotionOffset;
		}
		set
		{
			animatedArmature.RootMotionOffset = value;
		}
	}

	public Quaternion RootMotionRotationOffset
	{
		get
		{
			return animatedArmature.RootMotionRotationOffset;
		}
		set
		{
			animatedArmature.RootMotionRotationOffset = value;
		}
	}

	private AnimatedArmature animatedArmature => animationSystems.AnimatedArmature;

	public bool IsPosing => layers[0].TargetState != null;

	public bool IsPlaying => layers[0].IsPlaying;

	public PoseAnimationClip PrimaryClip
	{
		get
		{
			if (!IsPosing)
			{
				return null;
			}
			return layers[0].TargetState.Clip;
		}
	}

	public ExtensibleAnimator(Armature armature, Transform rootMotionTransform, GesturesWeights gestures)
	{
		layerParents[0] = AnimatorLayer.Primary;
		layerParents[1] = AnimatorLayer.Primary;
		layerParents[2] = AnimatorLayer.Body;
		layerParents[3] = AnimatorLayer.Torso;
		layerParents[4] = AnimatorLayer.Torso;
		layerParents[5] = AnimatorLayer.Body;
		layerParents[6] = AnimatorLayer.Body;
		additiveLayer = new AnimationLayerState();
		layers = new AnimationLayerState[7].Fill(() => new AnimationLayerState());
		state = new PrivateAnimatorState(layers, additiveLayer);
		logger.EnableDebug = true;
		Armature = armature;
		animationSystems = new AnimSystems(new AnimatedArmature(state, armature, rootMotionTransform), new AnimatedFace(state, gestures));
		Bind();
	}

	internal IAnimationClipState GetActiveLayerClip(AnimatorLayer layer)
	{
		AnimationLayerState l = ((layer == AnimatorLayer.Additive) ? additiveLayer : layers[(int)layer]);
		if (l.TargetState == null)
		{
			return null;
		}
		if (!l.TargetState.IsPlaying)
		{
			return null;
		}
		return l.TargetState;
	}

	public void Bind()
	{
		animationSystems.Bind();
	}

	public void Update(float dt, float at)
	{
		animationSystems.BeforeUpdate();
		AnimationClipState primaryState = layers[0].TargetState;
		if ((primaryState != null || activeStates.Count != 0) && UpdateStates(at))
		{
			animationSystems.Apply(primaryState, dt);
			OnMusclesChange.Flush();
			OnIKTargetChange.Flush();
			OnClipFrameChanged.Flush();
			OnLayerClipComplete.Flush();
		}
	}

	private bool UpdateAnimationStage(float dt, AnimationClipState layer)
	{
		bool anyplaying = false;
		if (layer.Stage == AnimationStage.FadeOut && layer.Stage <= AnimationStage.FadeIn)
		{
			layer.Stage = AnimationStage.Stop;
		}
		if (layer.Stage == AnimationStage.Play && layer.Weight < 1f)
		{
			layer.Stage = AnimationStage.FadeIn;
		}
		if (layer.Stage == AnimationStage.FadeIn && layer.Weight >= 1f)
		{
			layer.Stage = AnimationStage.Play;
		}
		if (layer.Stage == AnimationStage.FadeIn)
		{
			anyplaying = true;
			if (layer.BlendingTime <= 0f)
			{
				layer.Weight = 1f;
			}
			else
			{
				layer.Weight = Mathf.Clamp01(layer.Weight + dt / layer.BlendingTime);
			}
		}
		else if (layer.Stage == AnimationStage.FadeOut)
		{
			anyplaying = true;
			if (layer.BlendingTime <= 0f)
			{
				layer.Weight = 0f;
			}
			else
			{
				layer.Weight = Mathf.Clamp01(layer.Weight - dt / layer.BlendingTime);
			}
		}
		else if (layer.Stage == AnimationStage.Stop)
		{
			if (layer == layers[0].TargetState)
			{
				OnLayerClipComplete.Invoke(AnimatorLayer.Primary);
			}
			else
			{
				AnimationLayerState primarystate = layers[0];
				for (int i = 1; i < layers.Length; i++)
				{
					if (layers[i].TargetState == layer)
					{
						layers[i].TargetState = layers[i - 1].TargetState;
						layers[i].TargetState.Weight = 0.5f;
						OnLayerClipComplete.Invoke((AnimatorLayer)i);
					}
				}
				animationSystems.AnimatedArmature.SignalIkChange(primarystate.TargetState, 0.5f);
			}
		}
		return anyplaying;
	}

	private void UpdateState(AnimationClipState state, float dt)
	{
		if (!state.seqInitialized)
		{
			animationSystems.Initialize(state);
			state.seqInitialized = true;
		}
		SeqClip seq = state.seqClip;
		while (dt > 0f)
		{
			float maxupdate = seq.length - state.seqTime;
			float dt2 = Mathf.Min(maxupdate, dt);
			state.IsPlaying = dt2 > 0f;
			dt -= dt2;
			state.seqTime += dt2;
			animationSystems.Update(state, dt2);
			if (dt > 0f)
			{
				if (state.Clip.Cyclic || state.CompletionMode == AnimationCompletionMode.Loop)
				{
					RolloverAnimation(state);
				}
				else
				{
					state.IsPlaying = false;
				}
				if (dt2 <= 0f)
				{
					break;
				}
			}
		}
	}

	private void RolloverAnimation(AnimationClipState state)
	{
		state.seqTime = 0f;
		state.Cycles++;
		animationSystems.OnRollover(state);
		state.seqTime = state.seqClip.loopTimeIndex;
	}

	private bool UpdateStates(float dt)
	{
		bool anyplaying = false;
		foreach (AnimationClipState state in activeStates)
		{
			anyplaying |= UpdateAnimationStage(dt, state);
			if (state.IsPlaying)
			{
				anyplaying = true;
				UpdateState(state, dt);
			}
		}
		bool invalidateStates = false;
		AnimationLayerState[] array = layers;
		foreach (AnimationLayerState layer in array)
		{
			if (layer.SourceState != null)
			{
				if (layer.TargetState == null)
				{
					layer.SourceState = null;
					invalidateStates = true;
				}
				else if (layer.TargetState.Stage != AnimationStage.FadeIn)
				{
					layer.SourceState = null;
					invalidateStates = true;
				}
			}
		}
		if (invalidateStates)
		{
			InvalidateActiveStates();
		}
		return anyplaying;
	}

	internal void ClearAnimation()
	{
		AnimationLayerState[] array = layers;
		foreach (AnimationLayerState layer in array)
		{
			layer.TargetState = null;
			layer.SourceState = null;
		}
		additiveLayer.SourceState = null;
		additiveLayer.TargetState = null;
		activeStates.Clear();
	}

	public void SetAnimation(PoseAnimationClip clip, AnimatorLayer layer, float blendingTime = -1f)
	{
		SetAnimation(clip, new AnimatorLayer[1] { layer }, blendingTime);
	}

	private SeqClip GetSeqClip(PoseAnimationClip pac, bool additive)
	{
		SeqClip psc = SeqClipConverter.Create(pac, animationSystems.AnimatedArmature.Bones, additive);
		clipcache[pac.UniqueName] = (pac, psc);
		return psc;
	}

	public void SetAnimation(PoseAnimationClip clip, ICollection<AnimatorLayer> layers, float blendingTime = -1f, AnimationCompletionMode completionMode = AnimationCompletionMode.Default)
	{
		if (logger.EnableDebug)
		{
			logger.Debug("Start animation {0}", clip.UniqueName);
		}
		if (clip.RootMotionType != RootMotionType.None && !clip.Cyclic)
		{
			SceneWarnings.Instance.Report("Critical animator problem: {0} but has no loop. Clips with root-motion must be cyclic", clip.UniqueName, clip.RootMotionType);
		}
		if (layers.Contains(AnimatorLayer.Body))
		{
			if (!layers.Contains(AnimatorLayer.Torso))
			{
				layers.Add(AnimatorLayer.Torso);
			}
			if (!layers.Contains(AnimatorLayer.LegLeft))
			{
				layers.Add(AnimatorLayer.LegLeft);
			}
			if (!layers.Contains(AnimatorLayer.LegRight))
			{
				layers.Add(AnimatorLayer.LegRight);
			}
		}
		if (layers.Contains(AnimatorLayer.Torso))
		{
			if (!layers.Contains(AnimatorLayer.HandLeft))
			{
				layers.Add(AnimatorLayer.HandLeft);
			}
			if (!layers.Contains(AnimatorLayer.HandRight))
			{
				layers.Add(AnimatorLayer.HandRight);
			}
		}
		bool primarySwitch = layers.Contains(AnimatorLayer.Primary);
		if (activeStates.Count == 0 || primarySwitch)
		{
			Bind();
		}
		AnimationClipState state = new AnimationClipState(GetSeqClip(clip, layers.Contains(AnimatorLayer.Additive)), animationSystems.AnimatedArmature.Bones, animationSystems.AnimatedArmature.CreateBuffer());
		state.CompletionMode = completionMode;
		if (blendingTime < 0f)
		{
			blendingTime = ((state.Clip.States.Count > 0) ? state.Clip.States[0].FadeIn : PoseAnimationFrame.DEFAULT.FadeIn);
		}
		if (primarySwitch)
		{
			for (int i = 0; i < this.layers.Length; i++)
			{
				SetLayerState(i, state, blendingTime);
			}
			SetAdditiveLayerState(null, blendingTime, null);
		}
		else if (layers.Contains(AnimatorLayer.Additive))
		{
			SetAdditiveLayerState(state, blendingTime, layers);
		}
		else
		{
			foreach (AnimatorLayer layer in layers)
			{
				SetLayerState((int)layer, state, blendingTime);
			}
		}
		InvalidateActiveStates();
	}

	private void SetLayerState(int layer, AnimationClipState state, float blendingTime)
	{
		AnimationClipState current = layers[layer].TargetState;
		int parentLayer = (int)layerParents[layer];
		if (current != null && layer > 0 && layers[parentLayer].TargetState != state)
		{
			layers[layer].SourceState = current;
			logger.Info("{0} clip displaced {1} on layer {2}", state.Clip.UniqueName, current.Clip.UniqueName, (AnimatorLayer)layer);
		}
		else
		{
			layers[layer].SourceState = null;
		}
		layers[layer].TargetState = state;
		layers[layer].TargetState.Weight = 0f;
		layers[layer].TargetState.BlendingTime = blendingTime;
		layers[layer].TargetState.Stage = AnimationStage.FadeIn;
	}

	private void SetAdditiveLayerState(AnimationClipState state, float blendingTime, ICollection<AnimatorLayer> layers)
	{
		if (state == null || additiveLayer.TargetState == null || additiveLayer.TargetState.Clip != state.Clip)
		{
			additiveLayer.TargetState = state;
			if (state != null)
			{
				logger.Debug("Set additive animation clip {0}", state.Clip.UniqueName);
				additiveLayer.TargetState.Weight = 0f;
				additiveLayer.TargetState.BlendingTime = blendingTime;
				additiveLayer.TargetState.Stage = AnimationStage.FadeIn;
				additiveLayer.AdditiveLayers = layers.ToArray();
			}
			else
			{
				logger.Debug("Clear additive animation clip");
			}
		}
	}

	private void InvalidateActiveStates()
	{
		activeStates.Clear();
		AnimationLayerState[] array = layers;
		foreach (AnimationLayerState s in array)
		{
			if (s.TargetState != null && !activeStates.Contains(s.TargetState))
			{
				activeStates.Add(s.TargetState);
			}
			if (s.SourceState != null && !activeStates.Contains(s.SourceState))
			{
				activeStates.Add(s.SourceState);
			}
		}
		if (additiveLayer.TargetState != null && !activeStates.Contains(additiveLayer.TargetState))
		{
			activeStates.Add(additiveLayer.TargetState);
		}
	}
}
