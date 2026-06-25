using Assets.Productos.Juegos.Reception.Scripts.Genetica.Eventos;
using Assets.SingletonesAndSystemasGlobales.AbstractLayer.Globales;
using BepInEx.Configuration;
using BetterExperience.Features.Overlay;
using BetterExperience.Features.PluginOptions;
using BetterExperience.GameScopes;
using HarmonyLib;

namespace BetterExperience.Features;

internal class SingleGroupFeature : PluginFeature
{
	public class SingleGroupModeService : SessionService
	{
		public override void OnStart()
		{
			base.OnStart();
			if (Singleton<PiscinasDeEventosDeEntrevista>.existeEnScena)
			{
				LockNivel(Singleton<PiscinasDeEventosDeEntrevista>.instance);
			}
			else
			{
				logger.Error("No PiscinasDeEventosDeEntrevista");
			}
		}

		private void LockNivel(PiscinasDeEventosDeEntrevista instance)
		{
			Traverse accessor = Traverse.Create((object)instance);
			accessor.Field<bool>("m_overrideNivel").Value = true;
			accessor.Field<int>("m_overridingNivel").Value = 1;
			Lookup<OverlayService>().InfoMessage("Groups count locked");
		}
	}

	public static class HarmonyPatches
	{
	}

	private ConfigEntry<bool> enableFeature;

	public override bool Enabled => enableFeature.Value;

	public override void Configure(ConfigFile config)
	{
		enableFeature = config.Bind<bool>("Features", "SingleGroupMode", false, "Enable Single Group Mode: Use single gene pool regardless of level");
	}

	public override void OnInit()
	{
		base.OnInit();
		Lookup<PluginOptionsService>().Expose(enableFeature, base.Scope);
	}

	public override void OnStart()
	{
		base.OnStart();
		Lookup<SessionTracker>().SessionServices.Add(() => new SingleGroupModeService());
	}
}
