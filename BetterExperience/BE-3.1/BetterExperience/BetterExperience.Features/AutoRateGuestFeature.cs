using Assets._ReusableScripts.CuchiCuchi.Dependentes.ScenaManagers;
using Assets._ReusableScripts.Globales;
using Assets.Productos.Juegos.Reception.Scripts.Dependientes.ScenaManagers;
using Assets.Productos.Juegos.Reception.Scripts.Entrevistas;
using Assets.Productos.Juegos.Reception.Scripts.Entrevistas.Modelos;
using BepInEx.Configuration;
using BetterExperience.Features.PluginOptions;
using BetterExperience.GameScopes;
using BetterExperience.HarmonyPatches;
using UnityEngine;

namespace BetterExperience.Features;

internal class AutoRateGuestFeature : PluginFeature
{
	private ConfigEntry<bool> configEnableFeature;

	private ConfigEntry<int> configAutorateAppearanceTo;

	private ConfigEntry<int> configAutoratePersonalityTo;

	private bool adjusted;

	public Observable<EstrevistaRatingModelo> AfterGuestAutorated { get; } = new Observable<EstrevistaRatingModelo>();

	public override bool Enabled => configEnableFeature.Value;

	public override void Configure(ConfigFile config)
	{
		configEnableFeature = config.Bind<bool>("Features", "EnableAutorating", false, "Autorating: Enable feature");
		configAutorateAppearanceTo = config.Bind<int>("Autorating", "AutorateAppearance", 0, "Autorating: Appearance");
		configAutoratePersonalityTo = config.Bind<int>("Autorating", "AutoratePersonality", 0, "Autorating: Personality");
	}

	public override void OnInit()
	{
		PluginOptionsService pluginOptionsService = Lookup<PluginOptionsService>();
		pluginOptionsService.Expose(configEnableFeature, base.Scope);
		pluginOptionsService.Expose(configAutorateAppearanceTo, base.Scope);
		pluginOptionsService.Expose(configAutoratePersonalityTo, base.Scope);
	}

	public override void OnStart()
	{
		SMAGlobalPatches.AfterRatingModelRefreshed.Add(AfterRatingModelRefreshed, base.Scope);
		Lookup<SessionTracker>().OnNewSession.Add(delegate(GameSession session)
		{
			session.OnGuestReady += delegate
			{
				adjusted = false;
			};
		}, base.Scope);
	}

	private void AfterRatingModelRefreshed(PanelDeEntrevistaCalificacion obj)
	{
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		if (((EntrevistaConFemaleDePoolDelDia)SceneSingletonV2<ScenaCharacteresManager>.Instance(((Component)(object)obj).gameObject.scene)).flagScoreAparienciaCurrentFemaleV2.Count != 0 || adjusted)
		{
			return;
		}
		adjusted = true;
		int value = configAutorateAppearanceTo.Value;
		if (value > 0)
		{
			EstrevistaCalificacionAparienciaFisicaModelo aparienciaFisica = obj.ratingModel.aparienciaFisica;
			aparienciaFisica.arms = value;
			aparienciaFisica.body = value;
			aparienciaFisica.breast = value;
			aparienciaFisica.buttocks = value;
			aparienciaFisica.crotch = value;
			aparienciaFisica.eyes = value;
			aparienciaFisica.face = value;
			aparienciaFisica.hair = value;
			aparienciaFisica.head = value;
			aparienciaFisica.height = value;
			aparienciaFisica.legs = value;
			aparienciaFisica.mouth = value;
			aparienciaFisica.nose = value;
			aparienciaFisica.skin = value;
			aparienciaFisica.waist_hip = value;
		}
		int value2 = configAutoratePersonalityTo.Value;
		if (value2 > 0)
		{
			EstrevistaCalificacionPersonalidadModelo personalidad = obj.ratingModel.personalidad;
			personalidad.angerManagement = value2;
			personalidad.painTolerance = value2;
			personalidad.exhibitionism = value2;
			personalidad.servicing = value2;
			personalidad.slutness = value2;
			if (value2 >= 8)
			{
				personalidad.summarizing = (PersonalidadConsenting)4;
			}
			else if (value2 >= 6)
			{
				personalidad.summarizing = (PersonalidadConsenting)3;
			}
			else if (value2 >= 4)
			{
				personalidad.summarizing = (PersonalidadConsenting)2;
			}
			else if (value2 >= 2)
			{
				personalidad.summarizing = (PersonalidadConsenting)1;
			}
			else
			{
				personalidad.summarizing = (PersonalidadConsenting)0;
			}
		}
		AfterGuestAutorated.Invoke(obj.ratingModel);
	}
}
