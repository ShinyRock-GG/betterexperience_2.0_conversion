using Assets._ReusableScripts;
using Assets._ReusableScripts.CuchiCuchi.AI.ReactoresDeEstimulos;
using Assets._ReusableScripts.CuchiCuchi.Controllers.Ojos.Parpadeos;
using Assets._ReusableScripts.CuchiCuchi.Dependentes.Ai.Reactores.ParaReactores.OjosExpresion;
using Assets._ReusableScripts.CuchiCuchi.Dependentes.Controllers;
using Assets.TValle.BeachGirl.Sexual;
using BepInEx.Configuration;
using BetterExperience.Features.PluginOptions;
using BetterExperience.GameScopes;
using UnityEngine;

namespace BetterExperience.Features;

internal class AmateurModelFeature : PluginFeature
{
	private class AmateurModelService : SessionService
	{
		private LookAtControllerV2 controller;

		private OjosExpresionController eyesController;

		private float time;

		private Camera lookAtCam;

		public float Duration { get; private set; } = 20f;

		public int Priority { get; private set; } = int.MaxValue;

		public bool EnableDontCloseEyes { get; set; } = true;

		public override void OnStart()
		{
			base.OnStart();
			controller = base.Session.Guest.Impl.GetComponentInChildren<LookAtControllerV2>();
			eyesController = base.Session.Guest.Impl.GetComponentInChildren<OjosExpresionController>();
			if (EnableDontCloseEyes)
			{
				OverrideEyesClosedAnimation();
			}
			ForceLookIntoCamera();
			Lookup<DispatcherService>().DoUpdate.Add(Update, base.Scope);
		}

		private void OverrideEyesClosedAnimation()
		{
			ParaReactor componentInChildren = base.Session.Guest.Impl.GetComponentInChildren<ParaReactorCerrarOjosAlAhogarse>();
			if (componentInChildren != null)
			{
				componentInChildren.enabled = false;
			}
			componentInChildren = base.Session.Guest.Impl.GetComponentInChildren<ParaReactorCerrarOjosAlCoito>();
			if (componentInChildren != null)
			{
				componentInChildren.enabled = false;
			}
		}

		private void Update()
		{
			time += Time.deltaTime;
			if (time > 5f || lookAtCam != Camera.main)
			{
				ForceLookIntoCamera();
				time = 0f;
			}
			if (EnableDontCloseEyes)
			{
				WidenEyes();
			}
		}

		private void WidenEyes()
		{
			float b = 0f;
			OjosExpresionController.Orden orden = eyesController.currentStado[OjosExpresionController.Tipo.achiquitar];
			if (orden != null)
			{
				b = orden.target;
				orden.target = 0f;
			}
			orden = eyesController.currentStado[OjosExpresionController.Tipo.agrandar];
			if (orden != null)
			{
				orden.target = Mathf.Max(orden.target, b);
			}
		}

		private void ForceLookIntoCamera()
		{
			lookAtCam = Camera.main;
			Transform transform = Camera.main.transform;
			controller.DetenerOrdenes();
			controller.Mirar(HeadWeight(), 1f, transform, LookAtControllerV2.LookAtType.fijamente, true, LookAtControllerV2.LookAtType.fijamente, true, 1f, Priority, Duration, ControllerPrioridadConfig.prioridad, default(Vector3));
		}

		private float HeadWeight()
		{
			if (base.Session.Player.Character.peneDeCharacter.isPenetrating && base.Session.Player.Character.peneDeCharacter.TryGetPenetratingHole() is IBocaHole)
			{
				return 0f;
			}
			return 1f;
		}
	}

	private ConfigEntry<bool> enableFeature;

	private ConfigEntry<bool> enableDontCloseEyes;

	public override bool Enabled => enableFeature.Value;

	public override void Configure(ConfigFile config)
	{
		enableFeature = config.Bind<bool>("Features", "EnableAmateurModel", true, "Enable Amateur Model: She has irresistable urge to look at the camera");
		enableDontCloseEyes = config.Bind<bool>("AmateurModel", "EnableDontCloseEyes", false, "Amateur Model: Enable dont-close-eyes: prevents some long animations with eyes closed");
	}

	public override void OnInit()
	{
		base.OnInit();
		Lookup<PluginOptionsService>().Expose(enableFeature, base.Scope, PluginOptionsService.SettingsType.guest);
		Lookup<PluginOptionsService>().Expose(enableDontCloseEyes, base.Scope, PluginOptionsService.SettingsType.guest);
	}

	public override void OnStart()
	{
		base.OnStart();
		Lookup<SessionTracker>().InterviewServices.Add(() => new AmateurModelService());
	}
}
