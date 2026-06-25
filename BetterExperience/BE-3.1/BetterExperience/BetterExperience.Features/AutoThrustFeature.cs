using System;
using Assets._ReusableScripts.CuchiCuchi;
using Assets._ReusableScripts.CuchiCuchi.AI;
using Assets._ReusableScripts.CuchiCuchi.AI.Emociones;
using Assets._ReusableScripts.CuchiCuchi.Dependentes.Controllers;
using Assets._ReusableScripts.CuchiCuchi.PhysicsAndBonesScripts;
using BepInEx.Configuration;
using BetterExperience.Features.Overlay;
using BetterExperience.Features.PluginOptions;
using BetterExperience.GameScopes;
using UnityEngine;

namespace BetterExperience.Features;

internal class AutoThrustFeature : PluginFeature
{
	public class AutoThrustService : SessionService
	{
		public class SequenceState
		{
			public float Velocity { get; set; }

			public BoneStretchedChain hole { get; set; }

			public Transform HoleEntrance { get; set; }

			public float MaxPRatio { get; set; }

			public MotionType Motion { get; set; }

			public bool HoleDepthLimit => hole.maximaProfundidadVirtualAlcanzada;

			public bool HoleDiameterLimit => hole.maximaAnchuraVirtualAlcanzada;

			public int Step { get; set; }

			public float NonDeformedExitPRatio { get; set; }

			public float ExitDeformation { get; set; }

			internal float UpdatePRatio(float pRatio)
			{
				if (MaxPRatio > pRatio)
				{
					MaxPRatio = Mathf.Lerp(MaxPRatio, pRatio, 0.1f);
				}
				else
				{
					MaxPRatio = Mathf.Lerp(MaxPRatio, pRatio, 0.3f);
				}
				return MaxPRatio;
			}

			internal void UpdateVelocity(float targetVelocity)
			{
				Velocity = LerpVelocity(Velocity, targetVelocity);
			}

			public float LerpVelocity(float actual, float target)
			{
				return Mathf.Min(Mathf.Lerp(actual, target, 0.1f), actual + (float)Math.Sign(target - actual) * Mathf.Min(0.07f, Mathf.Abs(target - actual)));
			}
		}

		private const float MAX_DEFORMATION_FACTOR = 0.6f;

		private OverlayService overlay;

		private IInputHandle hotkeyHandle;

		private PelvisMovementController controller;

		private PlacerBase pleasure;

		private ConfigEntry<KeyboardShortcut> hotkey;

		private ConfigEntry<bool> useConstantVelocity;

		private float deltaDepth;

		private float lastDepth;

		public int DepthLookahead { get; set; } = 1;

		public float MaxDepth { get; set; } = 0.2f;

		public float MaxBalancedVelocity { get; set; } = 0.7f;

		public float MinVelocity { get; set; } = 0.05f;

		public float MaxVelocity { get; set; } = 0.7f;

		public float MaxSafeVelocity { get; set; } = 0.15f;

		public float ThrustBalance { get; set; } = 0.5f;

		public float UserThrustBalance { get; set; } = 0.5f;

		public bool UseDynamicThrust => true;

		public SequenceState Sequence { get; private set; }

		public AutoThrustService(ConfigEntry<KeyboardShortcut> hotkey, ConfigEntry<bool> useConstantVelocity)
		{
			this.hotkey = hotkey;
			this.useConstantVelocity = useConstantVelocity;
		}

		public override void OnStart()
		{
			base.OnStart();
			overlay = Lookup<OverlayService>();
			hotkeyHandle = Lookup<DispatcherService>().Input.KeyboardEvent(hotkey, base.Scope);
			controller = base.Session.Player.GameObject.GetComponentInChildren<PelvisMovementController>();
			Lookup<DispatcherService>().DoUpdate.Add(OnUpdate, base.Scope);
			controller.updatingPelvisPosition += Controller_updatingPelvisPosition;
			EmocionesFemeninas componentInChildren = base.Session.Guest.Impl.GetComponentInChildren<EmocionesFemeninas>();
			pleasure = componentInChildren.placer;
		}

		public override void OnStop()
		{
			base.OnStop();
			if (controller != null)
			{
				controller.updatingPelvisPosition -= Controller_updatingPelvisPosition;
			}
		}

		private void Controller_updatingPelvisPosition(ref Vector3 currentLocalTarget, Transform effectorTransform, PelvisMovementController sender)
		{
			deltaDepth = currentLocalTarget.z - lastDepth;
			lastDepth = currentLocalTarget.z;
		}

		private void OnUpdate()
		{
			ReactInput();
			if (Sequence != null)
			{
				Process();
			}
		}

		private float GetPenetrationDepth()
		{
			return Sequence.hole.penetracionLocalActual * Sequence.hole.worldScale;
		}

		private float GetPenetrationFactor()
		{
			Penis pene = base.Session.Player.Character.pene;
			return GetPenetrationDepth() / pene.worldLength;
		}

		private float GetDeformationFactor(float penetrationFactor)
		{
			Penis pene = base.Session.Player.Character.pene;
			float value = (pene.punta.physicBone.position - pene.@base.physicBone.position).magnitude / pene.worldLength;
			value = Mathf.InverseLerp(0f, 0.9f - Mathf.InverseLerp(0.7f, 1f, penetrationFactor) * 0.3f, value);
			if (Sequence.NonDeformedExitPRatio == 1f)
			{
				value = 1f;
			}
			return value;
		}

		private float GetPenetrationRatio()
		{
			Penis pene = base.Session.Player.Character.pene;
			return Mathf.InverseLerp(pene.worldTipPartLength * 0.75f, pene.worldLength * 0.75f, GetPenetrationDepth());
		}

		private void Process()
		{
			if (Sequence.Motion == MotionType.NONE)
			{
				Thrust(0.01f);
			}
			Penis pene = base.Session.Player.Character.pene;
			float num = pene.worldTipPartLength / pene.worldLength;
			float penetrationFactor = GetPenetrationFactor();
			float deformationFactor = GetDeformationFactor(penetrationFactor);
			if (Sequence.Motion == MotionType.OUT)
			{
				float num2 = GetVelocity(MotionType.OUT);
				float velocity = GetVelocity(MotionType.IN);
				if (num2 > MaxBalancedVelocity)
				{
					num2 = Mathf.Lerp(MaxBalancedVelocity, num2, penetrationFactor * penetrationFactor * penetrationFactor);
				}
				else if (velocity < num2)
				{
					num2 = Mathf.Lerp(velocity, num2, penetrationFactor);
				}
				float num3 = MathfExtension.LerpConMedio(0.5f, 0.75f, 1.5f, GetVelocity01(num2)) * num;
				if (penetrationFactor > num3 || deformationFactor < 1f)
				{
					Thrust(0f - num2);
				}
				else
				{
					Thrust(num2);
				}
				return;
			}
			float relativeHipsToHoleDistance = GetRelativeHipsToHoleDistance();
			float num4 = MathfExtension.LerpConMedio(0.75f, 1f, 1.75f, GetVelocity01(MotionType.IN)) * num;
			float num5 = GetVelocity(MotionType.IN);
			bool flag = Sequence.HoleDepthLimit && penetrationFactor > num4;
			if (Sequence.HoleDiameterLimit && (double)deformationFactor < 0.8)
			{
				if (Sequence.NonDeformedExitPRatio == 0f)
				{
					Sequence.NonDeformedExitPRatio = GetPenetrationRatio();
				}
				if (deformationFactor < 1f)
				{
					num5 *= Mathf.Pow(0.5f, deformationFactor);
				}
			}
			if (lastDepth < MaxDepth && deltaDepth > 0.00015f && deformationFactor > 0.6f && !flag && relativeHipsToHoleDistance > 0f)
			{
				Thrust(num5);
				return;
			}
			Thrust(0f - num5);
			Sequence.ExitDeformation = deformationFactor;
		}

		private float GetRelativeHipsToHoleDistance()
		{
			Vector3 position = Sequence.HoleEntrance.position;
			Vector3 position2 = base.Session.Player.Character.pene.@base.physicBone.position;
			Transform animatorRootMotionTransform = base.Session.Player.Character.animatorRootMotionTransform;
			position = animatorRootMotionTransform.InverseTransformPoint(position);
			position2 = animatorRootMotionTransform.InverseTransformPoint(position2);
			Vector3 vector = Vector3.Project(position, Vector3.forward);
			Vector3 vector2 = Vector3.Project(position2, Vector3.forward);
			return vector.z - vector2.z;
		}

		private void Thrust(float dv)
		{
			MotionType motionType = ((dv > 0f) ? MotionType.IN : MotionType.OUT);
			if (Sequence.Motion != motionType)
			{
				Sequence.Motion = motionType;
				if (motionType == MotionType.OUT)
				{
					UpdateVelocitySettings();
				}
			}
			float num = dv;
			if (motionType == MotionType.OUT)
			{
				num = 0f - Mathf.Max(Mathf.Abs(num), MaxSafeVelocity);
			}
			num *= GetVelocityMultiplier(Sequence.Motion);
			controller.ControlProfundidad(num);
		}

		private float GetVelocity(MotionType motion)
		{
			return Sequence.Velocity * GetVelocityMultiplier(motion);
		}

		private float GetVelocityMultiplier(MotionType motion)
		{
			float num = ((motion != MotionType.IN) ? (1f - Mathf.Clamp(ThrustBalance, 0.5f, 1f)) : Mathf.Clamp(ThrustBalance, 0f, 0.5f));
			num = num * num / 0.25f;
			return Mathf.Max(num, 0.001f);
		}

		private float GetVelocity01(MotionType motion)
		{
			return MathfExtension.InverseLerpConMedio(MinVelocity, MaxSafeVelocity, MaxVelocity, GetVelocity(motion));
		}

		private float GetVelocity01(float velocity)
		{
			return MathfExtension.InverseLerpConMedio(MinVelocity, MaxSafeVelocity, MaxVelocity, velocity);
		}

		private void UpdateVelocitySettings()
		{
			Sequence.Step++;
			float velocity = GetVelocity(MotionType.IN);
			float a = Mathf.Lerp(MinVelocity, MaxVelocity, pleasure.value.value / 100f);
			Penis pene = base.Session.Player.Character.pene;
			float pRatio = Mathf.InverseLerp(pene.worldTipPartLength * 0.75f, pene.worldLength * 0.75f, GetPenetrationDepth());
			pRatio = Sequence.UpdatePRatio(pRatio);
			float num = Mathf.Lerp(MinVelocity, MaxVelocity, pRatio);
			if (num < MaxSafeVelocity)
			{
				num = MaxSafeVelocity;
			}
			if (lastDepth > MaxDepth && Sequence.ExitDeformation > 0.8f && (pRatio > 0.2f || velocity > MaxBalancedVelocity))
			{
				num = MaxVelocity;
			}
			float num2 = Mathf.Min(a, num);
			if (useConstantVelocity.Value)
			{
				num2 = num;
			}
			Sequence.UpdateVelocity(num2);
			float t = Mathf.InverseLerp(MinVelocity, MaxBalancedVelocity / 2f, num2);
			ThrustBalance = Mathf.Lerp(0.8f, 0.5f, t);
			if (Sequence.NonDeformedExitPRatio > 0f)
			{
				float a2 = Mathf.Lerp(MinVelocity, MaxBalancedVelocity, Sequence.NonDeformedExitPRatio);
				a2 = Mathf.Min(a2, Sequence.Velocity);
				a2 = Mathf.Lerp(velocity, a2, 0.3f);
				float f = a2 / Sequence.Velocity * 0.25f;
				ThrustBalance = Mathf.Sqrt(f);
				Sequence.NonDeformedExitPRatio = 0f;
			}
			if (ThrustBalance == 0.5f)
			{
				ThrustBalance = Mathf.Max(UserThrustBalance, (Sequence.Velocity > MaxBalancedVelocity) ? 0.6f : 0.5f);
			}
		}

		private void ReactInput()
		{
			if (base.Session.Player.Character.pene.isPenetrating)
			{
				if (hotkeyHandle.Up && hotkeyHandle.Duration < 2f)
				{
					if (Sequence != null)
					{
						StopSequence();
					}
					else
					{
						StartSequence();
					}
				}
			}
			else
			{
				StopSequence();
			}
		}

		public void TryStartSequence()
		{
			if (base.Session.Player.Character.pene.isPenetrating && Sequence == null)
			{
				StartSequence();
			}
		}

		private void StartSequence()
		{
			if (Sequence == null)
			{
				Sequence = new SequenceState
				{
					Velocity = MinVelocity,
					hole = base.Session.Player.Character.pene.TryGetPenetratingHole()
				};
				if (Sequence.hole != null)
				{
					Sequence.HoleEntrance = base.Session.Guest.Puppet.GetIKBoneTransform(Sequence.hole.entrada);
				}
				UpdateVelocitySettings();
				overlay.InfoMessage("Auto-thrust sequence started");
			}
		}

		private void StopSequence()
		{
			if (Sequence != null)
			{
				Sequence = null;
				overlay.InfoMessage("Auto-thrust sequence stopped");
			}
		}
	}

	public enum MotionType
	{
		NONE,
		IN,
		OUT
	}

	private ConfigEntry<bool> enableFeature;

	private ConfigEntry<KeyboardShortcut> hotkey;

	private ConfigEntry<bool> constantVelocity;

	public override bool Enabled => enableFeature.Value;

	public override void Configure(ConfigFile config)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		enableFeature = config.Bind<bool>("Features", "EnableAutoThrust", true, "Enable Auto Thrust: Automatic pelvis motion");
		hotkey = config.Bind<KeyboardShortcut>("AutoThrust", "MainHotkey", new KeyboardShortcut(KeyCode.Space, Array.Empty<KeyCode>()), "Auto Thrust: Start/stop hotkey");
		constantVelocity = config.Bind<bool>("AutoThrust", "ConstantVelocity", false, "Auto Thrust: Constant velocity");
	}

	public override void OnInit()
	{
		base.OnInit();
		Lookup<PluginOptionsService>().Expose(enableFeature, base.Scope, PluginOptionsService.SettingsType.player);
		Lookup<PluginOptionsService>().Expose(hotkey, base.Scope, PluginOptionsService.SettingsType.player);
		Lookup<PluginOptionsService>().Expose(constantVelocity, base.Scope, PluginOptionsService.SettingsType.player);
	}

	public override void OnStart()
	{
		base.OnStart();
		Lookup<SessionTracker>().InterviewServices.Add(() => new AutoThrustService(hotkey, constantVelocity));
	}
}
