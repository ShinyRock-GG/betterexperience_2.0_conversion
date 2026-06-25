using System;
using System.Collections;
using Assets._ReusableScripts;
using Assets._ReusableScripts.Globales;
using Assets._ReusableScripts.UI.Modales.Globales;
using Assets.Productos.Juegos.Reception.Scripts.Entrevistas;
using Assets.SingletonesAndSystemasGlobales.AbstractLayer.Globales;
using Assets.TValle.IU.Runtime.Drawing.Paneles.Modelos;
using Assets.TValle.IU.Runtime.Modales;
using BetterExperience.GameScopes;
using HarmonyLib;
using UnityEngine;

namespace BetterExperience.Features;

internal class MainMenuTracker : PluginService
{
	private PanelMainMenu mainMenu;

	public bool IsMainMenu { get; private set; }

	public Observable OnStateChanged { get; } = new Observable();

	public override void OnStart()
	{
		base.OnStart();
		Lookup<DispatcherService>().StartCoroutine(WaitForMainMenu(), base.Scope);
		Lookup<SessionTracker>().OnNewSession.Add(delegate(GameSession ss)
		{
			IsMainMenu = false;
			OnStateChanged.Invoke();
			ss.Scope.OnDispose += delegate
			{
				Lookup<DispatcherService>().StartCoroutine(WaitForMainMenu(), base.Scope);
			};
		}, base.Scope);
	}

	private IEnumerator WaitForMainMenu()
	{
		GameObject menu = GameObject.Find("/PlayerDesksMainMenu");
		while (menu == null)
		{
			yield return new WaitForSeconds(0.3f);
			menu = GameObject.Find("/PlayerDesksMainMenu");
		}
		IsMainMenu = true;
		mainMenu = UnityEngine.Object.FindObjectOfType<PanelMainMenu>();
		OnStateChanged.Invoke();
	}

	public void Continue(Action cb)
	{
		Traverse method = Traverse.Create((object)mainMenu).Method("UnloadMainLoadEmptyEntrevista", new Type[2]
		{
			typeof(Action),
			typeof(Action)
		}, (object[])null);
		method.GetValue(new object[2]
		{
			new Action(GlobalSingletonV2<MemoriaJson>.instance.LoadFromDiskDefaultFile),
			(Action)delegate
			{
				Singleton<ConfiguracionGeneralUsuario>.instance.playerName = GlobalSingletonV2<MemoriaJson>.instance.LeerDeep("UserName", crear: true).FindData("UserName", "Anon");
			}
		});
		cb();
		IsMainMenu = false;
		OnStateChanged.Invoke();
	}

	public void SingleInterview(Action cb)
	{
		PortraitsDialog diag = Singleton<ModalWindow>.instance.MostrarPortraitsDialog();
		((PortraitsModelBase)diag.panelDePortraits.portraitsModel).staring += delegate(PortraitsModelBase model)
		{
			if (model.protraitsDisponibles.ContieneIndex(model.currentSelected))
			{
				Traverse.Create((object)mainMenu).Method("LoadSingle", new object[2] { diag, model }).GetValue();
				cb();
				IsMainMenu = false;
				OnStateChanged.Invoke();
			}
			else
			{
				Singleton<ModalWindow>.instance.Clear();
			}
		};
		((PortraitsModelBase)diag.panelDePortraits.portraitsModel).canceling += delegate
		{
			Singleton<ModalWindow>.instance.Clear();
		};
	}
}
