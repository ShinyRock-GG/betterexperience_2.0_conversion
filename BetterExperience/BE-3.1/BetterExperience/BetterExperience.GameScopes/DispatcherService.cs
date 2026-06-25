using System;
using System.Collections;
using System.Collections.Generic;
using Assets;
using Assets._ReusableScripts.Globales.Updater;
using UnityEngine;

namespace BetterExperience.GameScopes;

public class DispatcherService : PluginService
{
	private class UnityListener : CustomUpdatedMonobehaviourBase
	{
		public DispatcherService Dispatcher { get; internal set; }

		public override GlobalUpdater.UpdateType? updateEvent1 => GlobalUpdater.UpdateType.meshGeneralModsUpdate1;

		public void Update()
		{
			Dispatcher.DoUpdate.Invoke();
			Dispatcher.PostUpdate();
		}

		public void OnGUI()
		{
			Dispatcher.DrawGUI.Invoke();
		}

		public override void OnUpdateEvent1()
		{
			Dispatcher.MeshGeneralUpdate1.Invoke();
		}
	}

	private GameObject gameObject;

	private List<Action> deferredActions = new List<Action>();

	public Observable DrawGUI { get; }

	public Observable DoUpdate { get; }

	public Observable MeshGeneralUpdate1 { get; } = new Observable();

	public InputManager Input { get; } = new InputManager();

	public DispatcherService(Observable drawGUI, Observable doUpdate)
	{
		DrawGUI = drawGUI;
		DoUpdate = doUpdate;
		DoUpdate.Add(Input.OnUpdate, base.Scope);
	}

	public override void OnStart()
	{
		base.OnStart();
		gameObject = new GameObject("BetterExperienceDispatcherObject");
		gameObject.hideFlags |= HideFlags.HideAndDontSave;
		UnityEngine.Object.DontDestroyOnLoad(gameObject);
		gameObject.AddComponent<UnityListener>().Dispatcher = this;
	}

	public Coroutine StartCoroutine(IEnumerator e)
	{
		return gameObject.GetComponent<UnityListener>().StartCoroutine(e);
	}

	private void PostUpdate()
	{
		while (deferredActions.Count > 0)
		{
			Action action = deferredActions[0];
			deferredActions.RemoveAt(0);
			action();
		}
	}

	public override void OnStop()
	{
		base.OnStop();
		if (gameObject != null)
		{
			UnityEngine.Object.DestroyImmediate(gameObject);
			gameObject = null;
		}
	}

	public void InvokeLater(Action action)
	{
		deferredActions.Add(action);
	}
}
