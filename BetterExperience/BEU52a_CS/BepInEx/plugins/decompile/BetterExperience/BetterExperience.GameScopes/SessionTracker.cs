using System;
using System.Collections.Generic;
using Assets._ReusableScripts.CuchiCuchi.Dependentes.ScenaManagers;
using Assets._ReusableScripts.Globales;
using Assets.Productos.Juegos.Reception.Scripts.Dependientes.ScenaManagers;
using BetterExperience.HarmonyPatches;
using BetterExperience.Wrappers.Characters;
using UnityEngine.SceneManagement;

namespace BetterExperience.GameScopes;

public class SessionTracker : PluginService
{
	private class GameSessionImpl : GameSession
	{
		public GameSessionImpl(bool single)
		{
			base.SingleMode = single;
		}

		public new void SetInterviewInstance(EntrevistaConFemale obj)
		{
			base.SetInterviewInstance(obj);
		}
	}

	public const string GAMEPLAY_LOGIC_SCENE = "EntrevistaGamePlayLogic";

	public const string RATING_GAME_LOBBY_SCENE = "EntrevistaVacia";

	public const string RATING_GAME_CHARACTER_SCENE = "EntrevistaHeroina";

	public const string SINGLE_CHARACTER_SCENE = "EntrevistaSingleMode";

	public const string DESIGNER_MODE_SCENE = "DesignerGamePlayLogic";

	private GameSessionImpl _current;

	public Observable<GameSession> OnNewSession = new Observable<GameSession>();

	private EntrevistaConFemale deferredSingleInterview;

	public GameSession Current => _current;

	public List<Func<PluginService>> SessionServices { get; } = new List<Func<PluginService>>();

	public List<Func<PluginService>> InterviewServices { get; } = new List<Func<PluginService>>();

	public bool DesignerMode { get; private set; }

	public override void OnStart()
	{
		base.OnStart();
		SMAGlobalPatches.OnBeforeSave.Add(PreSaveHook, base.Scope);
		SceneManager.sceneLoaded += SceneManager_sceneLoaded;
		SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
	}

	public override void OnStop()
	{
		base.OnStop();
		SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
		SceneManager.sceneUnloaded -= SceneManager_sceneUnloaded;
	}

	private void SceneManager_sceneUnloaded(Scene unloadedScene)
	{
		try
		{
			OnSceneUnloaded(unloadedScene);
		}
		catch (Exception e)
		{
			base.Scope.NotifyCrash(e);
		}
	}

	private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode arg1)
	{
		try
		{
			OnSceneLoaded(scene);
		}
		catch (Exception e)
		{
			base.Scope.NotifyCrash(e);
		}
	}

	private void OnSceneUnloaded(Scene unloadedScene)
	{
		if (unloadedScene.name == "EntrevistaGamePlayLogic")
		{
			OnCurrentSceneUnload_Event();
		}
		if (unloadedScene.name == "DesignerGamePlayLogic")
		{
			DesignerMode = false;
		}
	}

	private void OnSceneLoaded(Scene scene)
	{
		if ((scene.name == "EntrevistaSingleMode" || scene.name == "EntrevistaVacia") && Current == null)
		{
			if (DesignerMode)
			{
				return;
			}
			_current = new GameSessionImpl(scene.name == "EntrevistaSingleMode");
			base.Scope.Provide(_current, _current.Scope);
			foreach (Func<PluginService> supplier in SessionServices)
			{
				Current.Scope.AddService(supplier());
			}
			Current.OnGuestReady += delegate(GuestCharacter guest)
			{
				foreach (Func<PluginService> current in InterviewServices)
				{
					guest.Scope.AddService(current());
				}
			};
			OnNewSession.Invoke(Current);
			Current.Scope.Start();
		}
		if (scene.name == "DesignerGamePlayLogic")
		{
			DesignerMode = true;
		}
		if (scene.name == "EntrevistaHeroina")
		{
			if (_current != null)
			{
				if (_current.Guest == null)
				{
					LinkInterviewInstance(_current, scene);
				}
				else
				{
					logger.Error("Ignoring new character");
				}
			}
			else
			{
				logger.Error("Character loaded before session initialization");
			}
		}
		else if (scene.name == "EntrevistaSingleMode")
		{
			if (_current != null)
			{
				LinkInterviewInstance(_current, scene);
			}
			else
			{
				logger.Error("Character loaded before session initialization");
			}
		}
	}

	private void LinkInterviewInstance(GameSessionImpl session, Scene scene)
	{
		EntrevistaConFemale interview = (EntrevistaConFemale)SceneSingletonV2<ScenaCharacteresManager>.Instance(scene);
		if (interview.isStared)
		{
			session.SetInterviewInstance(interview);
			return;
		}
		interview.stared += delegate
		{
			session.SetInterviewInstance(interview);
		};
	}

	private void OnCurrentSceneUnload_Event()
	{
		if (Current != null)
		{
			Current.Scope.Dispose();
			_current = null;
		}
		deferredSingleInterview = null;
	}

	private void PreSaveHook()
	{
		if (Current != null)
		{
			DateTime dt = DateTime.Now;
			Current.PreSave.Invoke();
		}
	}
}
