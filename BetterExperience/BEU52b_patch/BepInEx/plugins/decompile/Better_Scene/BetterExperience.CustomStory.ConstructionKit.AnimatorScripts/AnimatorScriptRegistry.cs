using System;
using System.Collections.Generic;
using BetterExperience.CustomScene.ConstructionKit;
using BetterExperience.CustomScene.Poser;
using UnityEngine;

namespace BetterExperience.CustomStory.ConstructionKit.AnimatorScripts;

internal class AnimatorScriptRegistry
{
	public List<KeyValuePair<string, Action<PoseAnimationClip>>> Scripts { get; } = new List<KeyValuePair<string, Action<PoseAnimationClip>>>();

	public Vector3 DeltaRootPos { get; set; }

	public Quaternion DeltaRootRot { get; set; }

	public ClipEditorPanelService Service { get; internal set; }

	public AnimatorScriptRegistry()
	{
		Scripts.Add(new KeyValuePair<string, Action<PoseAnimationClip>>("Smooth Forward Loop", delegate(PoseAnimationClip p)
		{
			SmoothMotionLoop smoothMotionLoop = new SmoothMotionLoop(p);
			smoothMotionLoop.HasForwardMotion = true;
			smoothMotionLoop.Process();
		}));
		Scripts.Add(new KeyValuePair<string, Action<PoseAnimationClip>>("Smooth Rotation Loop", delegate(PoseAnimationClip p)
		{
			SmoothMotionLoop smoothMotionLoop = new SmoothMotionLoop(p);
			smoothMotionLoop.HasForwardMotion = false;
			smoothMotionLoop.Process();
		}));
		Scripts.Add(new KeyValuePair<string, Action<PoseAnimationClip>>("Hyper Pin Muscles", delegate(PoseAnimationClip p)
		{
			new HyperPinMuscles(p).Process();
		}));
		Scripts.Add(new KeyValuePair<string, Action<PoseAnimationClip>>("Root transform correction", delegate(PoseAnimationClip p)
		{
			new RootTransformCorrection(p, this).Process();
		}));
		Scripts.Add(new KeyValuePair<string, Action<PoseAnimationClip>>("Delete frames range", delegate(PoseAnimationClip p)
		{
			new FrameRangeRemoval(p, this).Process();
		}));
		Scripts.Add(new KeyValuePair<string, Action<PoseAnimationClip>>("Delete unused keyframes", delegate(PoseAnimationClip p)
		{
			new KeyframeCleanup(p, this).Process();
		}));
	}
}
