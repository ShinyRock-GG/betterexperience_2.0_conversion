using BepInEx.Configuration;
using BetterExperience.Features.BetterHand;
using BetterExperience.Features.PluginOptions;
using BetterExperience.GameScopes;

namespace BetterExperience.Features;

internal class BetterHandFeature : PluginFeature
{
	private ConfigEntry<bool> enableFeature;

	private ConfigEntry<bool> activateByClick;

	private ConfigEntry<bool> enableSmartHand;

	private ConfigEntry<bool> enableExperimental;

	private ConfigEntry<bool> enableLegacyGrip;

	private ConfigEntry<bool> enableSurfaceAssistance;

	public override bool Enabled => enableFeature.Value;

	public override void Configure(ConfigFile config)
	{
		enableFeature = config.Bind<bool>("Features", "EnableBetterHand", true, "BetterHand: Enable feature");
		activateByClick = config.Bind<bool>("BetterHand", "ActivateByClick", false, "BetterHand: Click-to-grab (Drag-to-grab otherwise)");
		enableSmartHand = config.Bind<bool>("BetterHand", "EnableSmartHand", true, "BetterHand: Enable automatic grip control");
		enableExperimental = config.Bind<bool>("BetterHand", "EnableExperimental", false, "BetterHand: Enable experimental features");
		enableLegacyGrip = config.Bind<bool>("BetterHand", "EnableLegacyGrip", false, "BetterHand: Use old grip controller");
		enableSurfaceAssistance = config.Bind<bool>("BetterHand", "EnableSurfaceAssistance", true, "BetterHand: Enable surface assistance");
	}

	public override void OnInit()
	{
		PluginOptionsService pluginOptionsService = Lookup<PluginOptionsService>();
		pluginOptionsService.Expose(enableFeature, base.Scope);
		pluginOptionsService.Expose(activateByClick, base.Scope);
		pluginOptionsService.Expose(enableSmartHand, base.Scope);
		pluginOptionsService.Expose(enableExperimental, base.Scope);
		pluginOptionsService.Expose(enableLegacyGrip, base.Scope);
		pluginOptionsService.Expose(enableSurfaceAssistance, base.Scope);
	}

	public override void OnStart()
	{
		Lookup<SessionTracker>().InterviewServices.Add(() => new HandPhysicsService());
		Lookup<SessionTracker>().InterviewServices.Add(() => new SmartHandService
		{
			FeatureEnabled = enableSmartHand.Value,
			Experimental = enableExperimental.Value,
			LegacyGrip = enableLegacyGrip.Value
		});
		Lookup<SessionTracker>().InterviewServices.Add(() => new HandGrabService
		{
			ActivateByClick = activateByClick.Value
		});
		Lookup<SessionTracker>().InterviewServices.Add(() => new SurfaceAssistanceService
		{
			Enabled = enableSurfaceAssistance.Value
		});
	}
}
