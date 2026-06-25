using System;
using System.Collections.Generic;
using Assets._ReusableScripts.CuchiCuchi;
using Assets._ReusableScripts.CuchiCuchi.Dependentes.AI.Reactores.Effector;
using Assets._ReusableScripts.CuchiCuchi.Dependentes.Ai.Reactores.Orales;
using Assets._ReusableScripts.CuchiCuchi.Dependentes.Controllers;
using Assets.TValle.BeachGirl;
using BepInEx.Configuration;
using BetterExperience.Features.Overlay;
using BetterExperience.Features.PluginOptions;
using BetterExperience.GameScopes;
using BetterExperience.Utils;
using UnityEngine;

namespace BetterExperience.Features;

internal class AutoSeekerFeature : PluginFeature
{
	private class AutoSeekerService : SessionService
	{
		private const float TRANSLATION_SPEED = 0.1f;

		private const int MAX_SOLVABLE_V_ANGLE = 80;

		private const float TRANSLATION_PRECISSION = 0.005f;

		private IInputHandle hotkey;

		private PelvisMovementController ctl;

		private AutoplacerState state;

		private Vector3 pelvisTarget;

		private List<Behaviour> unwantedBehaviors = new List<Behaviour>();

		private OverlayService overlay;

		private AutoThrustFeature.AutoThrustService autoThruster;

		private bool autoscale;

		public float MaxDepth
		{
			get
			{
				if (autoThruster == null)
				{
					return 0.3f;
				}
				return autoThruster.MaxDepth / 2f;
			}
		}

		public ConfigEntry<KeyboardShortcut> HotkeyCfg { get; internal set; }

		public ConfigEntry<bool> Autothrust { get; internal set; }

		public override void OnStart()
		{
			base.OnStart();
			overlay = Lookup<OverlayService>();
			DispatcherService dispatcherService = Lookup<DispatcherService>();
			dispatcherService.DoUpdate.Add(OnUpdate, base.Scope);
			hotkey = dispatcherService.Input.KeyboardEvent(HotkeyCfg, base.Scope);
			ctl = base.Session.Player.GameObject.GetComponentInChildren<PelvisMovementController>();
			base.Session.Guest.Puppet.GetIKBoneTransform(base.Session.Guest.Impl.vagHole.entrada);
			base.Session.Guest.Puppet.GetIKBoneTransform(base.Session.Guest.Impl.anusHole.entrada);
			base.Session.Guest.Puppet.GetIKBoneTransform(base.Session.Guest.Impl.bocaHole.entrada);
			base.Scope.EventHandler(delegate(UpdatingPelvisPosition h)
			{
				ctl.updatingPelvisPosition += h;
			}, delegate(UpdatingPelvisPosition h)
			{
				ctl.updatingPelvisPosition -= h;
			}, OnUpdatingPelvisPosition);
			InitUnwantedBehaviors();
			autoThruster = TryLookup<AutoThrustFeature.AutoThrustService>();
			autoscale = TryLookup<PlayerScaler.ScalerService>() != null;
		}

		private void InitUnwantedBehaviors()
		{
			ReactorConEffectorAEstimulosTactiles[] componentsInChildren = base.Session.Guest.Impl.GetComponentsInChildren<ReactorConEffectorAEstimulosTactiles>();
			foreach (ReactorConEffectorAEstimulosTactiles item in componentsInChildren)
			{
				unwantedBehaviors.Add(item);
			}
			ReactorSexAtPorVerPene[] componentsInChildren2 = base.Session.Guest.Impl.GetComponentsInChildren<ReactorSexAtPorVerPene>();
			foreach (ReactorSexAtPorVerPene item2 in componentsInChildren2)
			{
				unwantedBehaviors.Add(item2);
			}
			ReactorSexAtPorTocarPene[] componentsInChildren3 = base.Session.Guest.Impl.GetComponentsInChildren<ReactorSexAtPorTocarPene>();
			foreach (ReactorSexAtPorTocarPene item3 in componentsInChildren3)
			{
				unwantedBehaviors.Add(item3);
			}
			ReactorSexAtPorSerPenetrada[] componentsInChildren4 = base.Session.Guest.Impl.GetComponentsInChildren<ReactorSexAtPorSerPenetrada>();
			foreach (ReactorSexAtPorSerPenetrada item4 in componentsInChildren4)
			{
				unwantedBehaviors.Add(item4);
			}
		}

		private void OnUpdatingPelvisPosition(ref Vector3 currentLocalTarget, Transform effectorTransform, PelvisMovementController sender)
		{
			pelvisTarget = currentLocalTarget;
		}

		private void SetScriptsEnabled(bool enabled)
		{
			foreach (Behaviour unwantedBehavior in unwantedBehaviors)
			{
				unwantedBehavior.enabled = enabled;
			}
		}

		private void OnUpdate()
		{
			MaleChar character = base.Session.Player.Character;
			if (state != null)
			{
				if (state.ExitReason == ExitReason.None)
				{
					if (hotkey.Up && hotkey.Duration < 2f)
					{
						state.ExitReason = ExitReason.Manual;
					}
					else if (character.pene.isPenetrating)
					{
						state.ExitReason = ExitReason.Completed;
					}
					else if (character.pene.IsBlocked())
					{
						state.ExitReason = ExitReason.NoTool;
					}
				}
				if (state.ExitReason != ExitReason.None)
				{
					SetScriptsEnabled(enabled: true);
					if (state.ExitReason == ExitReason.VerticalAngleTooWide)
					{
						overlay.InfoMessage("Auto-seek sequence stopped: too wide angle");
					}
					else if (state.ExitReason == ExitReason.UnreachableTarget)
					{
						overlay.InfoMessage("Auto-seek sequence stopped: unreachable target [ is player too short? ]");
					}
					else
					{
						if (state.ExitReason == ExitReason.Completed && autoThruster != null && Autothrust.Value)
						{
							autoThruster.TryStartSequence();
						}
						overlay.InfoMessage("Auto-seek sequence stopped");
					}
					state = null;
				}
				else
				{
					Tracer.DrawTransform(state.Hole);
					UpdatePlacement();
				}
			}
			else if (hotkey.Up && hotkey.Duration < 2f && !character.pene.isPenetrating && !character.pene.IsBlocked())
			{
				state = new AutoplacerState();
				state.RootTransform = base.Session.Player.RootMotion;
				SetScriptsEnabled(enabled: false);
				overlay.InfoMessage("Auto-seek sequence started {0}s", hotkey.Duration);
				state.Hole = GetClosestHole();
			}
		}

		private void UpdatePlacement()
		{
			Vector3 vector = state.RootTransform.InverseTransformPoint(state.Hole.position);
			Transform tip = ((Penetrador)base.Session.Player.Character.pene).tip;
			state.TranslateInto = vector - state.RootTransform.InverseTransformPoint(tip.position);
			Tracer.DrawTransform(state.RootTransform);
			if (!state.ResetComplete)
			{
				if (Mathf.Abs(pelvisTarget.z) > 0.005f)
				{
					float num = Mathf.Clamp(0f - pelvisTarget.z, -0.01f, 0.01f);
					ctl.ControlProfundidadDelta(num);
					return;
				}
				state.ResetComplete = true;
				if (autoscale)
				{
					base.Session.Player.ResetScale();
				}
			}
			if (UpdateRotation())
			{
				return;
			}
			Transform rootMotion = base.Session.Player.RootMotion;
			Vector3 translateInto = state.TranslateInto;
			float num2 = UnityUtils.FromToAxisAngle(tip.forward, -state.Hole.forward, rootMotion.right);
			translateInto.z += base.Session.Player.Character.pene.worldTipPartLength * 0.1f;
			if (translateInto.y + pelvisTarget.y > 0.2f)
			{
				state.ExitReason = ExitReason.UnreachableTarget;
				return;
			}
			bool flag = Mathf.Abs(translateInto.y) > 0.005f;
			bool flag2 = Mathf.Abs(translateInto.x) > 0.005f;
			if (!state.FinalStage && (flag || flag2) && translateInto.z < 0.1f)
			{
				translateInto.z -= 1f;
			}
			if (translateInto.z > 0f && !state.FinalStage)
			{
				if (flag)
				{
					if (translateInto.y < 0f || pelvisTarget.y < 0f)
					{
						float num3 = translateInto.y * Mathf.Min(1f, Time.deltaTime * 5f);
						ctl.ControlVerticalDelta(num3);
					}
					else if (autoscale)
					{
						Vector3 vector2 = Vector3.up * 0.1f * Time.deltaTime;
						base.Session.Player.AddScale(vector2);
					}
					else
					{
						state.ExitReason = ExitReason.UnreachableTarget;
					}
					return;
				}
				if (Mathf.Abs(num2) > 1f && (num2 < 0f || pelvisTarget.y < 0f) && Mathf.Abs(pelvisTarget.z) < MaxDepth)
				{
					float a = Mathf.Abs(num2) / 100f;
					a = Mathf.Max(a, 0.01f);
					float num4 = Mathf.Clamp(num2, 0f - a, a);
					ctl.ControlProfundidad(0f - num4);
					return;
				}
				if (flag2)
				{
					float x = TranslateTowards(translateInto.x);
					base.Session.Player.Move(new Vector3(x, 0f, 0f));
					return;
				}
				state.FinalStage = true;
			}
			if (Mathf.Abs(translateInto.z) > 0f)
			{
				float z = TranslateTowards(translateInto.z, 0.05f);
				base.Session.Player.Move(new Vector3(0f, 0f, z));
			}
		}

		private float TranslateTowards(float target, float dampAt = 0f)
		{
			float num = 0.1f * Time.deltaTime;
			if (Mathf.Abs(target) < dampAt)
			{
				num /= 10f;
			}
			return Mathf.MoveTowards(0f, target, num);
		}

		private bool UpdateRotation()
		{
			Transform rootMotion = base.Session.Player.RootMotion;
			if (Mathf.Abs(UnityUtils.FromToAxisAngle(rootMotion.forward, -state.Hole.forward, rootMotion.right)) > 80f)
			{
				state.ExitReason = ExitReason.VerticalAngleTooWide;
				return true;
			}
			float num = UnityUtils.FromToAxisAngle(rootMotion.forward, -state.Hole.forward, rootMotion.up);
			if (Mathf.Abs(num) < 1f)
			{
				return false;
			}
			base.Session.Player.Rotate(Mathf.MoveTowards(0f, num, 50f * Time.deltaTime));
			return true;
		}

		private Transform GetClosestHole()
		{
			FemaleChar impl = base.Session.Guest.Impl;
			IPene peneDeCharacter = base.Session.Player.Character.peneDeCharacter;
			Transform hole = base.Session.Guest.Puppet.GetIKBoneTransform(impl.vagHole.entrada);
			float distance = Vector3.Distance(peneDeCharacter.tip.position, hole.position);
			TestTransform(impl.anusHole.entrada, ref distance, ref hole);
			TestTransform(impl.bocaHole.entrada, ref distance, ref hole);
			return hole;
		}

		private void TestTransform(Transform t, ref float distance, ref Transform hole)
		{
			IPene peneDeCharacter = base.Session.Player.Character.peneDeCharacter;
			t = base.Session.Guest.Puppet.GetIKBoneTransform(t);
			float num = Vector3.Distance(peneDeCharacter.tip.position, t.position);
			if (num < distance)
			{
				distance = num;
				hole = t;
			}
		}
	}

	private class AutoplacerState
	{
		public Transform Hole { get; set; }

		public Transform RootTransform { get; set; }

		public Vector3 TranslateInto { get; internal set; }

		public ExitReason ExitReason { get; set; }

		public bool FinalStage { get; internal set; }

		public bool ResetComplete { get; set; }

		public float GetRotation2()
		{
			float y = UnityUtils.ToEuler(Quaternion.LookRotation(-Hole.forward, RootTransform.up)).y;
			float y2 = UnityUtils.ToEuler(Quaternion.LookRotation(RootTransform.forward, RootTransform.up)).y;
			float num = y - y2;
			float num2 = Mathf.Abs(num);
			if (num2 < 90f && num2 > 1f)
			{
				return num;
			}
			return 0f;
		}

		public float GetRotation()
		{
			UnityUtils.FromToAxisAngle(RootTransform.forward, -Hole.forward, RootTransform.forward);
			return UnityUtils.FromToAxisAngle(RootTransform.forward, -Hole.forward, RootTransform.up);
		}
	}

	private enum ExitReason
	{
		None,
		UnreachableTarget,
		VerticalAngleTooWide,
		Manual,
		Completed,
		NoTool
	}

	private ConfigEntry<bool> enableFeature;

	private ConfigEntry<KeyboardShortcut> hotkey;

	private ConfigEntry<bool> autothrust;

	public override bool Enabled => enableFeature.Value;

	public override void Configure(ConfigFile config)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		enableFeature = config.Bind<bool>("Features", "AutoSeekerEnabled", true, "Enable auto seeker: automated hole seeker");
		hotkey = config.Bind<KeyboardShortcut>("AutoSeeker", "Hotkey", new KeyboardShortcut(KeyCode.Space, Array.Empty<KeyCode>()), "Autoplacer: start/stop hotkey");
		autothrust = config.Bind<bool>("AutoSeeker", "EnableAutothrust", true, "Auto seeker: Start auto-thrust sequence when ready");
	}

	public override void OnInit()
	{
		base.OnInit();
		Lookup<PluginOptionsService>().Expose(enableFeature, base.Scope, PluginOptionsService.SettingsType.player);
		Lookup<PluginOptionsService>().Expose(autothrust, base.Scope, PluginOptionsService.SettingsType.player);
		Lookup<PluginOptionsService>().Expose(hotkey, base.Scope, PluginOptionsService.SettingsType.player);
	}

	public override void OnStart()
	{
		base.OnStart();
		Lookup<SessionTracker>().InterviewServices.Add(() => new AutoSeekerService
		{
			HotkeyCfg = hotkey,
			Autothrust = autothrust
		});
	}
}
