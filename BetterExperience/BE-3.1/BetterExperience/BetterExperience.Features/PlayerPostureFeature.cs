using System;
using System.Runtime.CompilerServices;
using Assets._ReusableScripts.CuchiCuchi.Dependentes.Controllers;
using Assets._ReusableScripts.CuchiCuchi.Dependentes.FinalIk;
using BepInEx.Configuration;
using BetterExperience.Features.PluginOptions;
using BetterExperience.GameScopes;
using HarmonyLib;
using RootMotion.Dynamics;
using UnityEngine;

namespace BetterExperience.Features;

internal class PlayerPostureFeature : PluginFeature
{
	public class PlayerPostureService : SessionService
	{
		private LocalEffectorOffset offset;

		private IInputHandle up;

		private IInputHandle down;

		private IInputHandle alt;

		private IInputHandle shift;

		private float maxBending = 1f;

		private float bending;

		private float maxBendingLocal;

		private bool bendingNow;

		private bool pelvisGrounded;

		private bool isSitting;

		private Vector3 leftFootOffset;

		private Vector3 rightFootOffset;

		private Vector3 leftFootOffsetTarget;

		private Vector3 rightFootOffsetTarget;

		private float velocity = 1f;

		private CapsuleCollider hipsCapsule;

		private PelvisRaycast legsRaycast = new PelvisRaycast();

		private float legLength;

		private float PelvisGroundingDistance { get; } = 0.135f;

		private float PevlisGroundingThreshold { get; } = 0.05f;

		private float PevlisDeGroundingThreshold { get; } = 0.3f;

		private int GroundingLayerMask { get; set; } = 1;

		private float GroundingRaycastDistance { get; set; } = 1f;

		public ConfigEntry<bool> EnableBending { get; internal set; }

		public ConfigEntry<KeyboardShortcut> UpKey { get; internal set; }

		public ConfigEntry<KeyboardShortcut> DownKey { get; internal set; }

		public ConfigEntry<bool> EnableFixHandsOffset { get; internal set; }

		public ConfigEntry<bool> EnableSitIK { get; internal set; }

		public ConfigEntry<bool> EnableKneelIK { get; internal set; }

		public unsafe override void OnStart()
		{
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_011c: Expected O, but got Ref
			base.OnStart();
			offset = base.Session.Player.GameObject.GetComponentInChildren<LocalEffectorOffset>();
			DispatcherService dispatcherService = Lookup<DispatcherService>();
			up = dispatcherService.Input.KeyboardEvent(UpKey, base.Scope);
			down = dispatcherService.Input.KeyboardEvent(DownKey, base.Scope);
			alt = dispatcherService.Input.KeyboardEvent(new KeyboardShortcut(KeyCode.LeftAlt, Array.Empty<KeyCode>()), base.Scope);
			shift = dispatcherService.Input.KeyboardEvent(new KeyboardShortcut(KeyCode.LeftShift, Array.Empty<KeyCode>()), base.Scope);
			Lookup<DispatcherService>().DoUpdate.Add(OnUpdate, base.Scope);
			PelvisMovementController componentInChildren = base.Session.Player.GameObject.GetComponentInChildren<PelvisMovementController>();
			componentInChildren.updatingPelvisPosition += Ctl_updatingPelvisPosition;
			PelvisMovementInterceptor.AfterPelvisIKUpdated.Add(AfterPelvisControllerUpdated);
			((PelvisMovementController.Range)System.Runtime.CompilerServices.Unsafe.AsPointer(ref componentInChildren.yRange)).min = -2f;
			Collider collider = base.Session.Player.Character.GetComponentInChildren<PuppetMaster>().GetMuscle(HumanBodyBones.Hips).colliders[0];
			hipsCapsule = collider as CapsuleCollider;
			Animator animator = base.Session.Player.Character.animator;
			Transform boneTransform = animator.GetBoneTransform(HumanBodyBones.Hips);
			Transform boneTransform2 = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
			legLength = boneTransform.position.y - boneTransform2.position.y;
		}

		private void LegsRaycast(Transform effectorTransform)
		{
			legsRaycast.LeftHit = null;
			legsRaycast.RightHit = null;
			Vector3 direction = -base.Session.Player.Character.animatorRootMotionTransform.up;
			float x = hipsCapsule.height / 1.5f;
			Vector3 vector = effectorTransform.TransformVector(x, 0f, 0f);
			if (Physics.Raycast(new Ray(effectorTransform.position - vector, direction), out var hitInfo, legLength, GroundingLayerMask))
			{
				legsRaycast.LeftHit = hitInfo;
			}
			if (Physics.Raycast(new Ray(effectorTransform.position + vector, direction), out hitInfo, legLength, GroundingLayerMask))
			{
				legsRaycast.RightHit = hitInfo;
			}
		}

		private void Ctl_updatingPelvisPosition(ref Vector3 currentLocalTarget, Transform effectorTransform, PelvisMovementController sender)
		{
			Transform animatorRootMotionTransform = base.Session.Player.Character.animatorRootMotionTransform;
			Vector3 direction = -base.Session.Player.Character.animatorRootMotionTransform.up;
			LegsRaycast(effectorTransform);
			bool flag;
			if (Physics.Raycast(new Ray(effectorTransform.position, direction), out var hitInfo, GroundingRaycastDistance, GroundingLayerMask))
			{
				if (hitInfo.distance <= PelvisGroundingDistance)
				{
					float num = PelvisGroundingDistance - hitInfo.distance;
					float num2 = currentLocalTarget.y - offset.leftThighOffset.y;
					num -= num2;
					if (num > 0f)
					{
						currentLocalTarget.y += num;
						logger.Error("Compensate overflow");
					}
				}
				if (hitInfo.distance < PelvisGroundingDistance + PevlisGroundingThreshold)
				{
					flag = true;
				}
				else if (hitInfo.distance > PelvisGroundingDistance + PevlisDeGroundingThreshold)
				{
					if (pelvisGrounded && isSitting)
					{
						currentLocalTarget.y = 0f;
						logger.Error("Reset pelvis local");
					}
					flag = false;
				}
				else
				{
					flag = false;
				}
			}
			else
			{
				flag = false;
			}
			if (!flag && pelvisGrounded && isSitting)
			{
				currentLocalTarget.y = 0f;
				logger.Error("Stand up");
			}
			pelvisGrounded = flag;
			isSitting = false;
			if (!pelvisGrounded)
			{
				return;
			}
			isSitting = true;
			if (Physics.Raycast(effectorTransform.position + animatorRootMotionTransform.forward * 0.5f, direction, out var hitInfo2, GroundingRaycastDistance, GroundingLayerMask))
			{
				if (hitInfo2.distance - hitInfo.distance < 0f || hitInfo2.distance < PelvisGroundingDistance)
				{
					isSitting = false;
				}
				else if (hitInfo2.distance < PelvisGroundingDistance + PevlisGroundingThreshold * 1.5f)
				{
					isSitting = false;
				}
			}
		}

		private void AfterPelvisControllerUpdated()
		{
			Vector3 vector = (offset.leftThighOffset + offset.rightThighOffset) / 2f;
			float num = vector.y / 4f;
			float num2 = legLength + vector.y;
			if (pelvisGrounded && isSitting && EnableSitIK.Value)
			{
				float num3 = 2f;
				float num4 = 3f;
				float z = Mathf.Min(0.5f, (0f - num) * num4);
				leftFootOffsetTarget = new Vector3(num * num3, 0f, z);
				rightFootOffsetTarget = new Vector3((0f - num) * num3, 0f, z);
				velocity = 1f;
			}
			else if (legsRaycast.AnyHit && EnableKneelIK.Value)
			{
				float num5 = -0.25f;
				float num6 = -0f;
				float num7 = (legsRaycast.LeftHit.HasValue ? (num2 - legsRaycast.LeftHit.Value.distance) : 0f);
				float num8 = (legsRaycast.RightHit.HasValue ? (num2 - legsRaycast.RightHit.Value.distance) : 0f);
				float num9 = ((num7 < 0.5f * legLength) ? 1 : 0);
				float num10 = ((num8 < 0.5f * legLength) ? 1 : 0);
				if (num7 != 0f)
				{
					leftFootOffsetTarget = new Vector3(num6, num7, num5 * num9);
				}
				else
				{
					leftFootOffsetTarget = new Vector3(num / 2f, 0f, num);
				}
				if (num8 != 0f)
				{
					rightFootOffsetTarget = new Vector3(0f - num6, num8, num5 * num10);
				}
				else
				{
					rightFootOffsetTarget = new Vector3((0f - num) / 2f, 0f, num);
				}
				velocity = 1f;
			}
			else
			{
				leftFootOffsetTarget = new Vector3(num / 2f, 0f, num);
				rightFootOffsetTarget = new Vector3((0f - num) / 2f, 0f, num);
				velocity = 2f;
			}
			leftFootOffset = Vector3.MoveTowards(leftFootOffset, leftFootOffsetTarget, Time.deltaTime * velocity);
			rightFootOffset = Vector3.MoveTowards(rightFootOffset, rightFootOffsetTarget, Time.deltaTime * velocity);
			offset.leftFootOffset = leftFootOffset;
			offset.rightFootOffset = rightFootOffset;
			if (EnableFixHandsOffset.Value)
			{
				offset.leftHandOffset = new Vector3(0f, num, 0f);
				offset.rightHandOffset = new Vector3(0f, num, 0f);
			}
			offset.leftShoulderOffset = new Vector3(0f, (0f - Mathf.Abs(bending)) / 2f, bending);
			offset.rightShoulderOffset = new Vector3(0f, (0f - Mathf.Abs(bending)) / 2f, bending);
		}

		private void OnUpdate()
		{
			if (!EnableBending.Value)
			{
				bending = 0f;
				bendingNow = false;
				return;
			}
			int num = 0;
			if (up.IsHold)
			{
				num = 1;
			}
			else if (down.IsHold)
			{
				num = -1;
			}
			float num2 = 1f;
			if (alt.IsHold)
			{
				num2 *= 0.5f;
			}
			if (shift.IsHold)
			{
				num2 *= 2f;
			}
			if (num != 0)
			{
				if (!bendingNow)
				{
					bendingNow = true;
					if (bending != 0f && Math.Sign(bending) != Math.Sign(num))
					{
						maxBendingLocal = 0f;
					}
					else
					{
						maxBendingLocal = maxBending;
					}
				}
				bending = Mathf.MoveTowards(bending, (float)num * maxBendingLocal, Time.deltaTime * num2);
			}
			else
			{
				bendingNow = false;
			}
		}
	}

	private class PelvisRaycast
	{
		public RaycastHit? LeftHit { get; internal set; }

		public RaycastHit? RightHit { get; internal set; }

		public bool AnyHit
		{
			get
			{
				if (!LeftHit.HasValue)
				{
					return RightHit.HasValue;
				}
				return true;
			}
		}
	}

	public static class PelvisMovementInterceptor
	{
		public static Observable AfterPelvisIKUpdated { get; } = new Observable();

		[HarmonyPatch(typeof(PelvisMovementController), "OnUpdateEvent1")]
		[HarmonyPostfix]
		public static void After_PelvisMovementController_OnUpdateEvent1()
		{
			AfterPelvisIKUpdated.Invoke();
		}
	}

	private ConfigEntry<bool> configEnableFeature;

	private ConfigEntry<bool> enableBending;

	private ConfigEntry<KeyboardShortcut> upShortcut;

	private ConfigEntry<KeyboardShortcut> downShortcut;

	private ConfigEntry<bool> enableFixHandsOffset;

	private ConfigEntry<bool> enableSitIK;

	private ConfigEntry<bool> enableKneelIK;

	public override bool Enabled => configEnableFeature.Value;

	public override void Configure(ConfigFile config)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		configEnableFeature = config.Bind<bool>("Features", "PlayerPosture", true, "Extended player posing: Enable feature");
		enableBending = config.Bind<bool>("PlayerPosture", "EnableBending", true, "PlayerPosture: Enable bending");
		upShortcut = config.Bind<KeyboardShortcut>("PlayerPosture", "BendFwdKey", new KeyboardShortcut(KeyCode.PageUp, Array.Empty<KeyCode>()), "PlayerPosture: Bend forward");
		downShortcut = config.Bind<KeyboardShortcut>("PlayerPosture", "BendBwdKey", new KeyboardShortcut(KeyCode.PageDown, Array.Empty<KeyCode>()), "PlayerPosture: Bend backward");
		enableFixHandsOffset = config.Bind<bool>("PlayerPosture", "EnableHandsOffset", false, "PlayerPosture: Enable 'fix hands offset' - hands will move down with hips");
		enableSitIK = config.Bind<bool>("PlayerPosture", "EnableSitIK", true, "PlayerPosture: Enable IK-based sit pose");
		enableKneelIK = config.Bind<bool>("PlayerPosture", "EnableKneelIK", true, "PlayerPosture: Enable IK-based kneel pose");
	}

	public override void OnStart()
	{
		base.OnStart();
		Harmony.CreateAndPatchAll(typeof(PelvisMovementInterceptor), (string)null);
		Lookup<SessionTracker>().SessionServices.Add(() => new PlayerPostureService
		{
			EnableBending = enableBending,
			UpKey = upShortcut,
			DownKey = downShortcut,
			EnableFixHandsOffset = enableFixHandsOffset,
			EnableSitIK = enableSitIK,
			EnableKneelIK = enableKneelIK
		});
	}

	public override void OnInit()
	{
		base.OnInit();
		Lookup<PluginOptionsService>().Expose(configEnableFeature, base.Scope, PluginOptionsService.SettingsType.player);
		Lookup<PluginOptionsService>().Expose(enableBending, base.Scope, PluginOptionsService.SettingsType.player);
		Lookup<PluginOptionsService>().Expose(upShortcut, base.Scope, PluginOptionsService.SettingsType.player);
		Lookup<PluginOptionsService>().Expose(downShortcut, base.Scope, PluginOptionsService.SettingsType.player);
		Lookup<PluginOptionsService>().Expose(enableFixHandsOffset, base.Scope, PluginOptionsService.SettingsType.player);
		Lookup<PluginOptionsService>().Expose(enableSitIK, base.Scope, PluginOptionsService.SettingsType.player);
		Lookup<PluginOptionsService>().Expose(enableKneelIK, base.Scope, PluginOptionsService.SettingsType.player);
	}
}
