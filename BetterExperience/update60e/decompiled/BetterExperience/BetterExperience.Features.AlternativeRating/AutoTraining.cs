using System;
using System.Collections;
using Assets._ReusableScripts.Genetica.NPCs;
using Assets._ReusableScripts.Scenes;
using Assets._ReusableScripts.Tiempo;
using Assets.Productos.Juegos.Reception.Scripts.Genetica.Eventos;
using Assets.Productos.Juegos.Reception.Scripts.TimepoEventosDeJuego;
using Assets.SingletonesAndSystemasGlobales.AbstractLayer.Globales;
using BepInEx.Configuration;
using BetterExperience.Features.Overlay;
using BetterExperience.GameScopes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BetterExperience.Features.AlternativeRating;

internal class AutoTraining : SessionService
{
	private bool _active;

	private bool activeOnce;

	private DispatcherService disp;

	private OverlayService overlay;

	private bool active
	{
		get
		{
			return _active;
		}
		set
		{
			_active = value;
			if (_active)
			{
				if (activeOnce)
				{
					overlay.InfoMessage("Autotrain once started");
				}
				else
				{
					overlay.InfoMessage("Autotrain started");
				}
			}
			else
			{
				overlay.InfoMessage("Autotrain stopped");
			}
		}
	}

	public override void OnStart()
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		base.OnStart();
		disp = Lookup<DispatcherService>();
		overlay = Lookup<OverlayService>();
		IInputHandle toggle = disp.Input.KeyboardEvent(new KeyboardShortcut(KeyCode.Home, Array.Empty<KeyCode>()), base.Scope);
		IInputHandle toggleOnce = disp.Input.KeyboardEvent(new KeyboardShortcut(KeyCode.End, Array.Empty<KeyCode>()), base.Scope);
		disp.DoUpdate.Add(delegate
		{
			if (toggle.Up || toggleOnce.Up)
			{
				activeOnce = toggleOnce.Up;
				active = !active;
				if (!active)
				{
					activeOnce = false;
				}
			}
		});
		disp.StartCoroutine(ControlRoutine(), base.Scope);
	}

	private IEnumerator ControlRoutine()
	{
		while (true)
		{
			yield return new WaitForSeconds(1.5f);
			if (!active)
			{
				continue;
			}
			if (SceneManager.GetSceneByName("EntrevistaHeroina").IsValid())
			{
				logger.Info("Interview found");
				if (base.Session.Guest != null)
				{
					logger.Info("Sending guest home");
					base.Session.TerminateInterview();
					continue;
				}
				yield return Singleton<HorariosNormalesDeEntrevistas>.instance.AvanzarTiempoHastaSiguienteEntrevista();
				EventoDiarioHorario evt = Singleton<HorariosNormalesDeEntrevistas>.instance.ObtenerCurrentEntrevistaEvento();
				if (evt.id == "MondayMorning")
				{
					active = false;
				}
			}
			else
			{
				if (!SceneManager.GetSceneByName("EntrevistaVacia").IsValid())
				{
					continue;
				}
				if (CanStartInterview())
				{
					logger.Info("Starting interview");
					StartInterview();
					if (activeOnce)
					{
						activeOnce = false;
						active = false;
					}
				}
				else
				{
					logger.Info("Going home");
					yield return Singleton<HorariosNormalesDeEntrevistas>.instance.AvanzarTiempoHastaSiguienteEntrevista();
					EventoDiarioHorario evt2 = Singleton<HorariosNormalesDeEntrevistas>.instance.ObtenerCurrentEntrevistaEvento();
					if (evt2.id == "MondayMorning")
					{
						active = false;
					}
				}
			}
		}
	}

	private unsafe void StartInterview()
	{
		//IL_000e: Expected O, but got Ref
		//IL_0016: Expected O, but got Ref
		//IL_0055: Expected O, but got Ref
		//IL_005d: Expected O, but got Ref
		SceneLoader.Pedido @default = SceneLoader.Pedido.@default;
		((SceneLoader.Pedido)(&@default)).scenaIndex = 4;
		((SceneLoader.Pedido)(&@default)).load = false;
		Singleton<SceneLoader>.instance.AddPedido(@default);
		PiscinasDeEventosDeEntrevista instance = Singleton<PiscinasDeEventosDeEntrevista>.instance;
		if (instance != null)
		{
			PiscinaDeNpcsManager piscinaDeNpcsManager = instance.ObtenerPiscinaDeCurrentEvento();
			if (piscinaDeNpcsManager != null)
			{
				piscinaDeNpcsManager.ObtenerSujetosAgrupadoNoCalificadoCount();
			}
		}
		SceneLoader.Pedido default2 = SceneLoader.Pedido.@default;
		((SceneLoader.Pedido)(&default2)).scenaIndex = 5;
		((SceneLoader.Pedido)(&default2)).load = true;
		Singleton<SceneLoader>.instance.AddPedido(default2);
	}

	private bool CanStartInterview()
	{
		PiscinasDeEventosDeEntrevista instance = Singleton<PiscinasDeEventosDeEntrevista>.instance;
		PiscinaDeNpcsManager piscinaDeNpcsManager = ((instance != null) ? instance.ObtenerPiscinaDeCurrentEvento() : null);
		if (((piscinaDeNpcsManager != null) ? new int?(piscinaDeNpcsManager.ObtenerSujetosAgrupadoNoCalificadoCount()) : ((int?)null)).GetValueOrDefault() > 0)
		{
			return true;
		}
		return false;
	}
}
