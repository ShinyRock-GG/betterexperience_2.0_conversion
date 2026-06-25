using System;
using Assets;
using BepInEx.Configuration;
using BetterExperience.Features.Overlay;
using BetterExperience.Features.PluginOptions;
using BetterExperience.GameScopes;
using BetterExperience.HarmonyPatches;
using UnityEngine;

namespace BetterExperience.Features;

internal class VelocityControlFeature : PluginFeature
{
	public class VelocityControlService : SessionService
	{
		private VelocityControlFeature feature;

		private RangeValueV2? lastVelocity;

		private RangeValueV2? desiredVelocity;

		private RangeValueV2? lastDepth;

		private RangeValueV2? desiredDepth;

		private int lastVelocityDirection;

		private int lastDepthDirection;

		private OverlayService overlay;

		public float Velocity
		{
			get
			{
				if (!desiredVelocity.HasValue)
				{
					return 0f;
				}
				return desiredVelocity.Value.max;
			}
			set
			{
				desiredVelocity = ((value > 0f) ? new RangeValueV2?(new RangeValueV2(0f, value)) : ((RangeValueV2?)null));
			}
		}

		public float Depth
		{
			get
			{
				if (!desiredDepth.HasValue)
				{
					return 0f;
				}
				return desiredDepth.Value.max;
			}
			set
			{
				desiredDepth = ((value > 0f) ? new RangeValueV2?(new RangeValueV2(0f, value)) : ((RangeValueV2?)null));
			}
		}

		public float MaxVelocity => feature.MaxVelocity.Value;

		public VelocityControlService(VelocityControlFeature feature)
		{
			this.feature = feature;
		}

		public override void OnStart()
		{
			base.OnStart();
			SMAGlobalPatches.OnComputeMaxVelocity.Add(OnUpdateVelocity, base.Scope);
			SMAGlobalPatches.OnComputeMaxDepth.Add(OnUpdateDepth, base.Scope);
			feature.OnFaster.Add(delegate
			{
				ChangeVelocity(1);
			}, base.Scope);
			feature.OnSlower.Add(delegate
			{
				ChangeVelocity(-1);
			}, base.Scope);
			feature.OnDeeper.Add(delegate
			{
				ChangeDepth(1);
			}, base.Scope);
			feature.OnShallow.Add(delegate
			{
				ChangeDepth(-1);
			}, base.Scope);
			overlay = Lookup<OverlayService>();
		}

		private void ChangeVelocity(int direction)
		{
			if (lastVelocity.HasValue && feature.VelocityStep.Value > 0f)
			{
				int num = 1;
				if (lastVelocityDirection == direction)
				{
					num = 2;
				}
				lastVelocityDirection = direction;
				float a = lastVelocity.Value.max + (float)(num * direction) * feature.VelocityStep.Value;
				a = Mathf.Max(a, feature.VelocityStep.Value);
				desiredVelocity = new RangeValueV2(0f, a);
				overlay.InfoMessage($"New velocity value {a}");
			}
		}

		private void ChangeDepth(int direction)
		{
			if (lastDepth.HasValue && feature.DepthStep.Value > 0f)
			{
				int num = 1;
				if (lastDepthDirection == direction)
				{
					num = 2;
				}
				lastDepthDirection = direction;
				float b = lastDepth.Value.max + (float)(num * direction) * feature.DepthStep.Value;
				b = Mathf.Max(feature.DepthStep.Value, b);
				desiredDepth = new RangeValueV2(0f, b);
				overlay.InfoMessage($"New depth value {b}");
			}
		}

		private void OnUpdateVelocity(SMAGlobalPatches.UpdateRangeEvent obj)
		{
			if (!desiredVelocity.HasValue && feature.OverrideDefaultVelocity.Value && feature.MaxVelocity.Value > 0f && feature.VelocityStep.Value > 0f)
			{
				int maxValue = Mathf.RoundToInt(feature.MaxVelocity.Value / feature.VelocityStep.Value) - 1;
				float value = (float)new System.Random().Next(maxValue) * feature.VelocityStep.Value;
				value = Mathf.Clamp(value, feature.VelocityStep.Value, feature.MaxVelocity.Value);
				desiredVelocity = new RangeValueV2(0f, value);
			}
			if (desiredVelocity.HasValue)
			{
				obj.Range = desiredVelocity.Value;
			}
			if (feature.MaxVelocity.Value > 0f && obj.Range.max > feature.MaxVelocity.Value)
			{
				obj.Range = new RangeValueV2(0f, feature.MaxVelocity.Value);
			}
			lastVelocity = obj.Range;
		}

		private void OnUpdateDepth(SMAGlobalPatches.UpdateRangeEvent obj)
		{
			if (desiredDepth.HasValue)
			{
				obj.Range = desiredDepth.Value;
			}
			lastDepth = obj.Range;
		}
	}

	private ConfigEntry<bool> enableFeature;

	public ConfigEntry<float> MaxVelocity { get; private set; }

	public ConfigEntry<float> VelocityStep { get; private set; }

	public ConfigEntry<bool> OverrideDefaultVelocity { get; private set; }

	public ConfigEntry<float> DepthStep { get; private set; }

	public override bool Enabled => enableFeature.Value;

	public Observable OnFaster { get; } = new Observable();

	public Observable OnSlower { get; } = new Observable();

	public Observable OnDeeper { get; set; } = new Observable();

	public Observable OnShallow { get; set; } = new Observable();

	public override void Configure(ConfigFile config)
	{
		enableFeature = config.Bind<bool>("Features", "EnableVelocityControl", true, "Enable autosex velocity constraints");
		MaxVelocity = config.Bind<float>("VelocityControl", "MaxVelocity", 1f, "VelocityControl: max velocity value");
		VelocityStep = config.Bind<float>("VelocityControl", "ChangeFactor", 0.1f, "VelocityControl: velocity change step");
		OverrideDefaultVelocity = config.Bind<bool>("VelocityControl", "StartAtMaxVelocity", false, "VelocityControl: override initial velocity");
		DepthStep = config.Bind<float>("VelocityControl", "DepthChangeFactor", 0.1f, "VelocityControl: depth change step");
	}

	public override void OnInit()
	{
		base.OnInit();
		PluginOptionsService pluginOptionsService = Lookup<PluginOptionsService>();
		pluginOptionsService.Expose(enableFeature, base.Scope, PluginOptionsService.SettingsType.guest);
		pluginOptionsService.Expose(OverrideDefaultVelocity, base.Scope, PluginOptionsService.SettingsType.guest);
		pluginOptionsService.Expose(MaxVelocity, base.Scope, PluginOptionsService.SettingsType.guest);
		pluginOptionsService.Expose(VelocityStep, base.Scope, PluginOptionsService.SettingsType.guest);
		pluginOptionsService.Expose(DepthStep, base.Scope, PluginOptionsService.SettingsType.guest);
	}

	public override void OnStart()
	{
		base.OnStart();
		Lookup<SessionTracker>().InterviewServices.Add(() => new VelocityControlService(this));
		NaturalLanguageFeature naturalLanguageFeature = null;
		try
		{
			naturalLanguageFeature = Lookup<NaturalLanguageFeature>();
		}
		catch (Exception)
		{
		}
		if (naturalLanguageFeature != null)
		{
			naturalLanguageFeature.AddExpression("faster", OnFaster.Invoke, base.Scope);
			naturalLanguageFeature.AddExpression("slower", OnSlower.Invoke, base.Scope);
			naturalLanguageFeature.AddExpression("deeper", OnDeeper.Invoke, base.Scope);
			naturalLanguageFeature.AddExpression("shallow", OnShallow.Invoke, base.Scope);
		}
	}
}
