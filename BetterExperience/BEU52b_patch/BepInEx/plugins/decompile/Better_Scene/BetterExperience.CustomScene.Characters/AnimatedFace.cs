using Assets._ReusableScripts.CuchiCuchi.Controllers;
using Assets._ReusableScripts.CuchiCuchi.Controllers.Ojos.Parpadeos;
using Assets.TValle.BeachGirl.Runtime;
using BetterExperience.CustomScene.Poser;
using BetterExperience.Wrappers.Characters;
using UnityEngine;

namespace BetterExperience.CustomScene.Characters;

internal class AnimatedFace : AnimatedSystem
{
	private Logger logger = Logger.Create<AnimatedFace>();

	private GesturesWeights gestures;

	private GesturesWeights bindingWeights;

	public AnimatedFace(ExtensibleAnimator.PrivateAnimatorState state, GesturesWeights gestures)
		: base(state)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		this.gestures = gestures;
		bindingWeights = new GesturesWeights(gestures.FaceBlendShapes.Keys);
	}

	public override void Initialize(ExtensibleAnimator.AnimationClipState state)
	{
		gestures.CopyTo(bindingWeights);
	}

	public override void Update(ExtensibleAnimator.AnimationClipState state, float dt)
	{
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Expected I4, but got Unknown
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0273: Expected I4, but got Unknown
		//IL_02ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_031d: Unknown result type (might be due to invalid IL or missing references)
		//IL_031f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0321: Unknown result type (might be due to invalid IL or missing references)
		//IL_032a: Unknown result type (might be due to invalid IL or missing references)
		//IL_033b: Unknown result type (might be due to invalid IL or missing references)
		//IL_036a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0394: Unknown result type (might be due to invalid IL or missing references)
		SeqClip seq = state.seqClip;
		(float t0, float t1, AnimationKeyFrame c0, AnimationKeyFrame c1) tuple = seq.FindConfiguration(state.seqTime);
		float t0 = tuple.t0;
		float t1 = tuple.t1;
		AnimationKeyFrame c0 = tuple.c0;
		AnimationKeyFrame c1 = tuple.c1;
		float a = Mathf.InverseLerp(t0, t1, state.seqTime);
		GesturesData f0 = c0.Gestures;
		GesturesData f1 = c1.Gestures;
		if (f0 == null)
		{
			f0 = GesturesData.NONE;
		}
		if (f1 == null)
		{
			f1 = GesturesData.NONE;
		}
		gestures.EyesOverride = f0.EyesEnabled || f1.EyesEnabled;
		gestures.MouthOverride = f0.MouthEnabled || f1.MouthEnabled;
		gestures.FaceOverride = f0.FaceEnabled || f1.FaceEnabled;
		gestures.FaceBlendShapesOverride = f0.FaceBlendShapesEnabled || f1.FaceBlendShapesEnabled;
		if (state.Weight < 1f)
		{
			GesturesWeights obj = gestures;
			obj.EyesOverride |= bindingWeights.EyesOverride;
			GesturesWeights obj2 = gestures;
			obj2.MouthOverride |= bindingWeights.MouthOverride;
			GesturesWeights obj3 = gestures;
			obj3.FaceOverride |= bindingWeights.FaceOverride;
			GesturesWeights obj4 = gestures;
			obj4.FaceBlendShapesOverride |= bindingWeights.FaceBlendShapesOverride;
		}
		gestures.EyeExpressions.Clear();
		gestures.MouthExpression.Clear();
		gestures.FaceExpression.Clear();
		gestures.FaceBlendShapes.Clear();
		EyeExpressionType[] keys = gestures.EyeExpressions.Keys;
		foreach (EyeExpressionType k in keys)
		{
			OjosExpresionController.Tipo t2 = (OjosExpresionController.Tipo)k;
			f0.Eyes.TryGetValue(t2, out var y0);
			f1.Eyes.TryGetValue(t2, out var y1);
			float y2 = Mathf.Lerp(y0, y1, a);
			if (state.Weight < 1f)
			{
				y2 = Mathf.Lerp(bindingWeights.EyeExpressions[k], y2, state.Weight);
			}
			if (y2 > 0f)
			{
				gestures.EyeExpressions[k] = y2;
			}
		}
		MouthGesture[] keys2 = gestures.MouthExpression.Keys;
		foreach (MouthGesture k2 in keys2)
		{
			TiposDeGestosDeBoca t3 = (TiposDeGestosDeBoca)k2;
			f0.Mouth.TryGetValue(t3, out var y3);
			f1.Mouth.TryGetValue(t3, out var y4);
			float y5 = Mathf.Lerp(y3, y4, a);
			if (state.Weight < 1f)
			{
				y5 = Mathf.Lerp(bindingWeights.MouthExpression[k2], y5, state.Weight);
			}
			if (y5 > 0f)
			{
				gestures.MouthExpression[k2] = y5;
			}
		}
		FaceExpressionType[] keys3 = gestures.FaceExpression.Keys;
		foreach (FaceExpressionType k3 in keys3)
		{
			TipoDeExpresion t4 = (TipoDeExpresion)k3;
			f0.Face.TryGetValue(t4, out var y6);
			f1.Face.TryGetValue(t4, out var y7);
			float y8 = Mathf.Lerp(y6, y7, a);
			if (state.Weight < 1f)
			{
				y8 = Mathf.Lerp(bindingWeights.FaceExpression[k3], y8, state.Weight);
			}
			if (y8 > 0f)
			{
				gestures.FaceExpression[k3] = y8;
			}
		}
		string[] keys4 = gestures.FaceBlendShapes.Keys;
		foreach (string k4 in keys4)
		{
			string t5 = k4;
			f0.FaceBlendShapes.TryGetValue(t5, out var y9);
			f1.FaceBlendShapes.TryGetValue(t5, out var y10);
			float y11 = Mathf.Lerp(y9, y10, a);
			if (state.Weight < 1f)
			{
				y11 = Mathf.Lerp(bindingWeights.FaceBlendShapes[k4], y11, state.Weight);
			}
			if (y11 > 0f)
			{
				gestures.FaceBlendShapes[k4] = y11;
			}
		}
		gestures.Dirty = true;
	}

	public override void Apply(ExtensibleAnimator.AnimationClipState state, float dt)
	{
	}
}
