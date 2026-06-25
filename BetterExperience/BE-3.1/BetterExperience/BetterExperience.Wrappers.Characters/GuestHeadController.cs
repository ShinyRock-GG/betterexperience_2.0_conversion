using System;
using Assets;
using Assets._ReusableScripts;
using Assets._ReusableScripts.CuchiCuchi.Characters.Controladores.ControlladoresDeColoDePrioridad;
using Assets.SingletonesAndSystemasGlobales.AbstractLayer.Globales;
using Assets.TValle.BeachGirl.Runtime;
using Assets.TValle.SystemasConstraints.RunTime.ChildOfConstraints.Implementation;
using BetterExperience.GameScopes;
using BetterExperience.Utils;
using UnityEngine;

namespace BetterExperience.Wrappers.Characters;

public class GuestHeadController : IDisposable
{
	private Logger logger = new Logger();

	private IControlladorDeBark talkController;

	private ControladorDeGestosDeBoca mimicController;

	private bool muted;

	public BitMask<TiposDeGestosDeBoca> ActiveMimics { get; private set; }

	public Observable<BitMask<TiposDeGestosDeBoca>> ActiveMimicChanged { get; private set; } = new Observable<BitMask<TiposDeGestosDeBoca>>();

	public bool Mute
	{
		get
		{
			return muted;
		}
		set
		{
			if (muted != value)
			{
				muted = value;
				if (muted)
				{
					talkController.FlagMinPrioridad(int.MaxValue, 60f);
				}
				else
				{
					talkController.FlagMinPrioridad(int.MinValue, 0f);
				}
			}
		}
	}

	public GuestHeadController(GameObject gameObject)
	{
		talkController = gameObject.GetComponentInChildren<IControlladorDeBark>();
		mimicController = gameObject.GetComponentInChildren<ControladorDeGestosDeBoca>();
		if (mimicController.isStared)
		{
			Singleton<SystemaMainChildOf>.instance.completedCalled += Instance_completedCalled;
			return;
		}
		mimicController.stared += delegate
		{
			Singleton<SystemaMainChildOf>.instance.completedCalled += Instance_completedCalled;
		};
	}

	private void Instance_completedCalled(SystemaMainChildOf obj)
	{
		try
		{
			BitMask<TiposDeGestosDeBoca> bitMask = default(BitMask<TiposDeGestosDeBoca>);
			ControladorDeGestosDeBoca.Stado currentStado = mimicController.currentStado;
			if (currentStado != null)
			{
				for (int i = 0; i < currentStado.Count; i++)
				{
					ControladorDeGestosDeBoca.Orden orden = currentStado.ordenes[i];
					if (orden != null)
					{
						bitMask = bitMask.Add(orden.tipoDeGesto);
					}
				}
			}
			if (!ActiveMimics.Equals(bitMask))
			{
				ActiveMimics = bitMask;
				ActiveMimicChanged.Invoke(bitMask);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Err");
		}
	}

	public void Dispose()
	{
		if (Singleton<SystemaMainChildOf>.existeEnScena)
		{
			Singleton<SystemaMainChildOf>.instance.completedCalled -= Instance_completedCalled;
		}
	}

	public void Say(string text, int priority = int.MaxValue, float perCharDurationFactor = 0.5f, float durationFactor = 1f)
	{
		talkController.Bark(text, vocalizar: true, priority, ControllerPrioridadConfig.prioridad, perCharDurationFactor, durationFactor);
	}
}
