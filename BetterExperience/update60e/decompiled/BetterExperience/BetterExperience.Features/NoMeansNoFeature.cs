using Assets._ReusableScripts.CuchiCuchi.Chars.Memorias;
using BepInEx.Configuration;
using BetterExperience.Features.PluginOptions;
using BetterExperience.GameScopes;

namespace BetterExperience.Features;

internal class NoMeansNoFeature : PluginFeature
{
	private class DisableConfirmationDialogService : SessionService
	{
		private static string ALREADY_ASKED_CONFIRMATION_VAR_NAME = "DESPACHAR_SEGURIDAD_PREGUNTA";

		private bool subscribed;

		private MemoriaDeCharacterTemporal memoria;

		public override void OnStart()
		{
			memoria = base.Session.Guest.Impl.GetComponentInChildren<MemoriaDeCharacterTemporal>();
			if (((MemoriaDeCharacterBase)memoria).memoria != null)
			{
				MemoriaDeCharacterBase.RegistrarCantidadPlus(memoria, ALREADY_ASKED_CONFIRMATION_VAR_NAME, ALREADY_ASKED_CONFIRMATION_VAR_NAME);
			}
			memoria.loaded += Memoria_loaded;
			subscribed = true;
		}

		private void Memoria_loaded(MemoriaDeCharacterBase obj)
		{
			MemoriaDeCharacterBase.RegistrarCantidadPlus(obj, ALREADY_ASKED_CONFIRMATION_VAR_NAME, ALREADY_ASKED_CONFIRMATION_VAR_NAME);
			obj.loaded -= Memoria_loaded;
			memoria.loaded -= Memoria_loaded;
			subscribed = false;
		}

		public override void OnStop()
		{
			base.OnStop();
			if (subscribed)
			{
				memoria.loaded -= Memoria_loaded;
			}
		}
	}

	private ConfigEntry<bool> enableFeature;

	public override bool Enabled => enableFeature.Value;

	public override void Configure(ConfigFile config)
	{
		enableFeature = config.Bind<bool>("Features", "EnableNoMeansNo", true, "Enable NoMeansNo: disables 'Are you sure you want me to leave?' question");
	}

	public override void OnInit()
	{
		base.OnInit();
		Lookup<PluginOptionsService>().Expose(enableFeature, base.Scope, PluginOptionsService.SettingsType.guest);
	}

	public override void OnStart()
	{
		Lookup<SessionTracker>().InterviewServices.Add(() => new DisableConfirmationDialogService());
	}
}
