using System;
using System.Collections.Generic;
using Assets;
using Assets._ReusableScripts.CuchiCuchi;
using Assets._ReusableScripts.CuchiCuchi.Controllers;
using Assets._ReusableScripts.CuchiCuchi.PhysicsAndBonesScripts;
using Assets._ReusableScripts.PhysicsScripts;
using Assets.SingletonesAndSystemasGlobales.AbstractLayer.Globales;
using BepInEx.Configuration;
using BetterExperience.Features.PluginOptions;
using BetterExperience.GameScopes;
using BetterExperience.HarmonyPatches;
using HarmonyLib;
using UnityEngine;

namespace BetterExperience.Features;

internal class LipsDeformerFeature : PluginFeature
{
	private class LabiaController : SessionService
	{
		private class AdminState
		{
			public int Index { get; set; }

			public Action PatchCallback { get; set; }

			public Vector3 PosTargetInitial { get; set; }

			public Traverse<Vector3> PosTargetAccessor { get; set; }
		}

		private VagController controller;

		private float[] targetPointsL;

		private float[] targetPointsR;

		private float[] realTargetsL;

		private float[] realTargetsR;

		private float _initialWidth;

		private Transform labiaRootTransform;

		private IDictionary<JointDistancesAdmin, AdminState> overriders = new Dictionary<JointDistancesAdmin, AdminState>();

		public float[] DefaultTargetsL => realTargetsL;

		public float[] DefaultTargetsR => realTargetsR;

		public float[] TargetsL => targetPointsL;

		public float[] TargetsR => targetPointsR;

		public float[] YTargets { get; set; }

		internal float Width => controller.vagHoleController.hole.maxAnchuraVirtualLocal;

		public float InitialWidth
		{
			get
			{
				if (_initialWidth == 0f)
				{
					if (!(PenetrationDepth > 0f))
					{
						return Width;
					}
					_initialWidth = Width;
				}
				return _initialWidth;
			}
		}

		internal float PenetrationDepth
		{
			get
			{
				BoneStretchedChain hole = controller.vagHoleController.hole;
				if (hole.isPenetrated)
				{
					return hole.penetracionLocalActual;
				}
				return 0f;
			}
		}

		internal bool WidthLimit
		{
			get
			{
				BoneStretchedChain hole = controller.vagHoleController.hole;
				if (hole.isPenetrated)
				{
					return hole.maximaAnchuraVirtualAlcanzada;
				}
				return false;
			}
		}

		internal float ScalerX => (labiaRootTransform.localScale.x - 0.7f) / 0.45f;

		internal float ScalerZ => (labiaRootTransform.localScale.z - 0.8752499f) / 0.19450003f;

		public override void OnStart()
		{
			base.OnStart();
			controller = base.Session.Guest.Impl.GetComponentInChildren<VagController>();
			SMAGlobalPatches.BeforeUpdateJointDistances.Add(OnOverrideJointDistance, base.Scope);
			targetPointsL = new float[controller.labiaController.l.chain.puntosBase.Count];
			targetPointsR = new float[targetPointsL.Length];
			realTargetsL = new float[targetPointsL.Length];
			realTargetsR = new float[targetPointsL.Length];
			YTargets = new float[targetPointsL.Length];
			ConfigureOverriders(controller.labiaController.l.chain, targetPointsL, realTargetsL);
			ConfigureOverriders(controller.labiaController.r.chain, targetPointsR, realTargetsR);
			labiaRootTransform = base.Session.Guest.Puppet.GetBoneTransform(Singleton<MapasDeHuesos>.instance.mapas.vagLabiaBonesMap.vagLabiaRoot);
		}

		private void ConfigureOverriders(Linear7BoneChainBase chain, float[] targetPoints, float[] realPoints)
		{
			for (int i = 0; i < chain.puntosBase.Count; i++)
			{
				JointDistancesAdmin admin = chain.puntosBase[i].jointDistancesAdmin;
				int idx = i;
				AdminState adminState = (overriders[admin] = new AdminState());
				AdminState state = adminState;
				state.PatchCallback = delegate
				{
					realPoints[idx] = admin.configuracion.finalTagetPoistionMod;
					if (YTargets[idx] == 0f)
					{
						admin.configuracion.finalTagetPoistionMod = Mathf.Min(admin.configuracion.finalTagetPoistionMod, targetPoints[idx]);
					}
					UpdateGrip(state);
				};
				targetPoints[i] = (realPoints[i] = admin.configuracion.finalTagetPoistionMod);
				state.PosTargetAccessor = Traverse.Create((object)admin).Field<Vector3>("targetPos");
				state.PosTargetInitial = state.PosTargetAccessor.Value;
				state.Index = i;
			}
		}

		private void UpdateGrip(AdminState state)
		{
			Vector3 posTargetInitial = state.PosTargetInitial;
			posTargetInitial.y -= YTargets[state.Index];
			state.PosTargetAccessor.Value = posTargetInitial;
		}

		private void OnOverrideJointDistance(JointDistancesAdmin obj)
		{
			if (overriders.TryGetValue(obj, out var value))
			{
				value.PatchCallback();
			}
		}

		internal void Invalidate()
		{
			controller.labiaController.UpdateDesgaste(force: true);
		}
	}

	private class LipsDeformerService : SessionService
	{
		private class State
		{
			public float WidthFactor { get; set; }

			public float YDeformationLowerBound { get; set; }

			public float YDeformationUpperBound { get; set; }
		}

		private LipsDeformerFeature feature;

		private static float[] maxTargetPoints = new float[7] { 1.25f, 0.75f, 0.5f, 0f, -1f, -1.5f, -2f };

		private float leftWeight;

		private float rightWeight;

		private LabiaController backend;

		private Transform lowerLegL;

		private Transform lowerLegR;

		private Transform hips;

		private float lastPenetration;

		private bool hasResistance;

		private bool anyResistance;

		private State state = new State();

		public LipsDeformerService(LipsDeformerFeature feature)
		{
			this.feature = feature;
		}

		public override void OnStart()
		{
			base.OnStart();
			backend = Lookup<LabiaController>();
			lowerLegL = base.Session.Guest.Puppet.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
			lowerLegR = base.Session.Guest.Puppet.GetBoneTransform(HumanBodyBones.RightLowerLeg);
			hips = base.Session.Guest.Puppet.GetBoneTransform(HumanBodyBones.Hips);
			IIKUpdater ik = base.Session.Guest.Impl.GetComponentInChildren<IIKUpdater>();
			base.Scope.EventHandler(delegate(Action<IIKUpdater> handler)
			{
				ik.onPhysicsIKUpdated += handler;
			}, delegate(Action<IIKUpdater> handler)
			{
				ik.onPhysicsIKUpdated -= handler;
			}, delegate
			{
				OnAfterPhysicsUpdate();
			});
		}

		private void OnAfterPhysicsUpdate()
		{
			bool flag = false;
			float num = Mathf.Clamp((backend.Width - backend.InitialWidth) / backend.InitialWidth, 0f, 2f);
			if (!ExtendedMonoBehaviour.AlmostEqual(state.WidthFactor, num))
			{
				state.WidthFactor = num;
				float num2 = 1f + 0.5f * backend.ScalerX;
				float t = backend.ScalerZ;
				float t2 = Mathf.Clamp01(num);
				if (!feature.enableShapeAwareGrip.Value)
				{
					num2 = 1.5f;
					t = 1f;
				}
				if (!feature.enableResistanceAwareGrip.Value)
				{
					t2 = 0f;
				}
				state.YDeformationLowerBound = Mathf.Lerp(-0.05f, -0.03f, t2) * num2 * 0.3f;
				float b = Mathf.Lerp(-0.001f, -0.005f, t) + 0.02f;
				float a = Mathf.Lerp(-0.001f, -0.01f, t) + 0.02f;
				state.YDeformationUpperBound = Mathf.Lerp(a, b, t2);
			}
			if (Time.frameCount % 10 == 0)
			{
				Vector3 vector = hips.transform.InverseTransformPoint(lowerLegL.position);
				Vector3 vector2 = hips.transform.InverseTransformPoint(lowerLegR.position);
				float x = vector.x;
				float x2 = vector2.x;
				x = Mathf.InverseLerp(0.1f, 0.4f, x) * num;
				x2 = Mathf.InverseLerp(0.1f, 0.4f, 0f - x2) * num;
				if (!feature.EnableLipGapDeformer.Value)
				{
					x = 0f;
					x2 = 0f;
				}
				else if (feature.EnablePermanentLipGap.Value)
				{
					x = 1f;
					x2 = 1f;
				}
				if (leftWeight != x || rightWeight != x2)
				{
					leftWeight = x;
					rightWeight = x2;
					float[] targetsL = backend.TargetsL;
					for (int i = 0; i < targetsL.Length; i++)
					{
						backend.TargetsL[i] = Mathf.Lerp(backend.DefaultTargetsL[i], maxTargetPoints[i], x);
						backend.TargetsR[i] = Mathf.Lerp(backend.DefaultTargetsL[i], maxTargetPoints[i], x2);
					}
					flag = true;
				}
			}
			float num3 = backend.PenetrationDepth - lastPenetration;
			if (num3 > 0f)
			{
				hasResistance = backend.WidthLimit;
			}
			num3 *= 2f;
			anyResistance |= hasResistance;
			float num4 = backend.YTargets[6];
			if (!feature.enableGripSimulation.Value || backend.PenetrationDepth == 0f || (!hasResistance && !anyResistance && feature.enableResistanceAwareGrip.Value))
			{
				lastPenetration = 0f;
				if (num4 != 0f)
				{
					flag = true;
					SetYTargets(0f);
				}
			}
			else
			{
				num3 = Mathf.MoveTowards(0f, num3, 0.3f * Time.deltaTime);
				float num5 = Mathf.Clamp(num4 + num3, state.YDeformationLowerBound, state.YDeformationUpperBound);
				if (num5 != num4)
				{
					SetYTargets(num5);
					flag = true;
				}
			}
			lastPenetration = backend.PenetrationDepth;
			if (flag)
			{
				backend.Invalidate();
			}
		}

		private void SetYTargets(float y)
		{
			for (int i = 4; i < 7; i++)
			{
				backend.YTargets[i] = y;
			}
		}
	}

	private ConfigEntry<bool> enableFeature;

	public ConfigEntry<bool> EnableLipGapDeformer { get; private set; }

	public ConfigEntry<bool> EnablePermanentLipGap { get; private set; }

	public ConfigEntry<bool> enableGripSimulation { get; private set; }

	public ConfigEntry<bool> enableShapeAwareGrip { get; private set; }

	public ConfigEntry<bool> enableResistanceAwareGrip { get; private set; }

	public override bool Enabled => enableFeature.Value;

	public override void Configure(ConfigFile config)
	{
		enableFeature = config.Bind<bool>("Features", "EnableLipsDeformer", true, "Enable lips deformer: various labia deformations");
		EnableLipGapDeformer = config.Bind<bool>("LipsDefomer", "EnableLipGapDeformation", true, "Lips Deformer: Enable lip gap deformation: larger hole -> larger potential gap");
		EnablePermanentLipGap = config.Bind<bool>("LipsDefomer", "EnablePermanentGapDeformation", false, "Lips Deformer: Enable permanent max gap deformation");
		enableGripSimulation = config.Bind<bool>("LipsDefomer", "EnableGripSimulation", true, "Lips Deformer: Enable grip simulation");
		enableShapeAwareGrip = config.Bind<bool>("LipsDefomer", "ShapeAwareGripSimulation", true, "Lips Deformer: Shape Aware Grip: Link grip shape to lips shape");
		enableResistanceAwareGrip = config.Bind<bool>("LipsDefomer", "ResistanceAwareGripSimulation", true, "Lips Deformer: Resistance Aware Grip: Link grip shape to actual hole resistance");
	}

	public override void OnInit()
	{
		base.OnInit();
		Lookup<PluginOptionsService>().Expose(enableFeature, base.Scope, PluginOptionsService.SettingsType.guest);
		Lookup<PluginOptionsService>().Expose(EnableLipGapDeformer, base.Scope, PluginOptionsService.SettingsType.guest);
		Lookup<PluginOptionsService>().Expose(EnablePermanentLipGap, base.Scope, PluginOptionsService.SettingsType.guest);
		Lookup<PluginOptionsService>().Expose(enableGripSimulation, base.Scope, PluginOptionsService.SettingsType.guest);
		Lookup<PluginOptionsService>().Expose(enableShapeAwareGrip, base.Scope, PluginOptionsService.SettingsType.guest);
		Lookup<PluginOptionsService>().Expose(enableResistanceAwareGrip, base.Scope, PluginOptionsService.SettingsType.guest);
	}

	public override void OnStart()
	{
		base.OnStart();
		Lookup<SessionTracker>().InterviewServices.Add(() => new LabiaController());
		Lookup<SessionTracker>().InterviewServices.Add(() => new LipsDeformerService(this));
	}
}
