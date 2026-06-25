using System;
using System.Collections.Generic;
using Assets;
using Assets._ReusableScripts;
using Assets._ReusableScripts.UI.Drawing;
using Assets.SingletonesAndSystemasGlobales.AbstractLayer.Globales;
using Assets.TValle.IU.Runtime.Drawing;
using Assets.TValle.IU.Runtime.Drawing.Abstracts;
using BepInEx.Configuration;
using BetterExperience.GameScopes;
using BetterExperience.HarmonyPatches;
using BetterExperience.Utils;
using BetterExperience.Wrappers.Characters;
using BetterExperience.Wrappers.Windows;
using UnityEngine;
using UnityEngine.UIElements;

namespace BetterExperience.Features.Overlay;

internal class SessionOverlayRenderService : SessionService
{
	private List<Drawable> renderables;

	private List<Drawable> mainMenuRenderables;

	private List<Drawable> alwaysOnScreenRenderables;

	private List<TemporaryNotification> postLoad;

	private List<TemporaryNotification> activeNotifications = new List<TemporaryNotification>();

	private DrawableLabel padding = new DrawableLabel("");

	private GameOptionsWindow optionsWnd;

	private ModificadorDeBool m_canHideCursor;

	private bool loadingComplete;

	private HashSet<GenericUserPanelBase> visibleWindows = new HashSet<GenericUserPanelBase>();

	private HashSet<GenericUserPanelBase> initialWindows = new HashSet<GenericUserPanelBase>();

	private List<GenericFlotanteUserPanel> floatingWindows = new List<GenericFlotanteUserPanel>();

	private bool overlayHidden;

	private UIToolkitInitializer uiTK;

	private Camera lastMainCamera;

	private DispatcherService dispatcher;

	private IInputHandle hotkey;

	public ConfigEntry<KeyboardShortcut> HideOverlayHotkey { get; internal set; }

	public bool EnableUITK { get; internal set; }

	public SessionOverlayRenderService(List<Drawable> renderables, List<TemporaryNotification> notifications, List<Drawable> mainMenuRenderables, List<Drawable> alwaysOnScreenRenderables)
	{
		this.renderables = renderables;
		postLoad = notifications;
		this.mainMenuRenderables = mainMenuRenderables;
		this.alwaysOnScreenRenderables = alwaysOnScreenRenderables;
		lastMainCamera = Camera.main;
	}

	public override void OnStart()
	{
		if (EnableUITK)
		{
			try
			{
				StartUITK();
			}
			catch (Exception ex)
			{
				logger.Error(ex, "Cannot start UITK");
				if (uiTK != null)
				{
					uiTK.Dispose();
				}
				uiTK = null;
				EnableUITK = false;
			}
		}
		dispatcher = Lookup<DispatcherService>();
		dispatcher.DrawGUI.Add(OnRender, base.Scope);
		dispatcher.DoUpdate.Add(OnUpdate, base.Scope);
		hotkey = dispatcher.Input.KeyboardEvent(HideOverlayHotkey, base.Scope);
		SMAGlobalPatches.OnLoadaderScreenUpdate.Add(OnLoaderScreenUpdate, base.Scope);
		optionsWnd = Lookup<GameOptionsWindow>();
		m_canHideCursor = Singleton<ConfiguracionGeneralDeMouse>.instance.canHideCursorModificableAnd.ObtenerModificadorNotNull((UnityEngine.Object)(object)Lookup<Plugin>());
		initialWindows = SubscribeToAllLargePanels(base.Scope);
		base.Session.OnGuestReady += Session_OnGuestReady;
		FindFloatingWindows();
	}

	private void FindFloatingWindows()
	{
		floatingWindows.Clear();
		PanelBase[] array = UnityEngine.Object.FindObjectsOfType<PanelBase>();
		for (int i = 0; i < array.Length; i++)
		{
			GenericFlotanteUserPanel component = array[i].GetComponent<GenericFlotanteUserPanel>();
			if (component != null)
			{
				floatingWindows.Add(component);
			}
		}
	}

	private void StartUITK()
	{
		uiTK = new UIToolkitInitializer();
		UIDocument uIDocument = uiTK.InitializeUIToolkit();
		uiTK.FixBattlehub();
		foreach (Drawable renderable in renderables)
		{
			if (renderable.NativeComponent != null && renderable.EnableNative)
			{
				uIDocument.rootVisualElement.Add(renderable.NativeComponent);
			}
		}
	}

	private void Session_OnGuestReady(GuestCharacter obj)
	{
		SubscribeToAllLargePanels(base.Session.Guest.Scope, initialWindows);
	}

	private HashSet<GenericUserPanelBase> SubscribeToAllLargePanels(ScopeSupport scope, HashSet<GenericUserPanelBase> except = null)
	{
		HashSet<GenericUserPanelBase> hashSet = new HashSet<GenericUserPanelBase>();
		PanelBase[] array = UnityEngine.Object.FindObjectsOfType<PanelBase>();
		foreach (PanelBase panelBase in array)
		{
			GenericUserPanelBase panelbase = panelBase.GetComponent<GenericUserPanelBase>();
			if ((except == null || !except.Contains(panelbase)) && panelbase is GenericUserPanelOnMainCanvas)
			{
				scope.EventHandler(delegate(Action<GenericUserPanelBase> h)
				{
					panelbase.showed += h;
				}, delegate(Action<GenericUserPanelBase> h)
				{
					panelbase.showed -= h;
				}, LargePanel_stateChanged);
				scope.EventHandler(delegate(Action<GenericUserPanelBase> h)
				{
					panelbase.hided += h;
				}, delegate(Action<GenericUserPanelBase> h)
				{
					panelbase.hided -= h;
				}, LargePanel_stateChanged);
				hashSet.Add(panelbase);
			}
		}
		return hashSet;
	}

	private void LargePanel_stateChanged(GenericUserPanelBase obj)
	{
		if (obj.isShowing)
		{
			visibleWindows.Add(obj);
		}
		else
		{
			visibleWindows.Remove(obj);
		}
	}

	public override void OnStop()
	{
		activeNotifications.ForEach(RemoveNotification);
		if (uiTK != null)
		{
			uiTK.Dispose();
		}
		base.OnStop();
	}

	private void OnUpdate()
	{
		dispatcher.Input.Enabled = !optionsWnd.Visible && !base.Session.Modal.Visible;
		if (optionsWnd.Visible)
		{
			return;
		}
		if (!lastMainCamera.isActiveAndEnabled)
		{
			RebindCamera();
		}
		if (loadingComplete && postLoad.Count > 0)
		{
			DrainNotifications();
		}
		activeNotifications.RemoveIf(delegate(TemporaryNotification n)
		{
			n._timer += Time.deltaTime;
			if (n.Duration <= n._timer)
			{
				RemoveNotification(n);
				return true;
			}
			if (n.Duration - n._timer < n.FadeOut)
			{
				n.Drawable.Transparency = (n.Duration - n._timer) / n.FadeOut;
			}
			else if (n.FadeIn > 0f && n._timer < n.FadeIn)
			{
				n.Drawable.Transparency = Mathf.Min(1f, n._timer / n.FadeIn);
			}
			return false;
		});
		if (hotkey.Down)
		{
			overlayHidden = !overlayHidden;
		}
		if (uiTK != null)
		{
			uiTK.SetVisible(!overlayHidden);
		}
	}

	private void RebindCamera()
	{
		lastMainCamera = Camera.main;
		foreach (GenericFlotanteUserPanel floatingWindow in floatingWindows)
		{
			if (floatingWindow.isBinded)
			{
				Canvas componentInChildren = floatingWindow.GetComponentInChildren<Canvas>();
				if (componentInChildren != null)
				{
					componentInChildren.worldCamera = lastMainCamera;
				}
			}
		}
	}

	private void RemoveNotification(TemporaryNotification n)
	{
		if (n.SpecificContainer == null)
		{
			renderables.Remove(n.Drawable);
		}
		else
		{
			n.SpecificContainer.Remove(n.Drawable);
		}
	}

	private void OnLoaderScreenUpdate(bool obj)
	{
		if (!obj)
		{
			loadingComplete = true;
		}
	}

	private void DrainNotifications()
	{
		activeNotifications.AddRange(postLoad);
		postLoad.Clear();
		activeNotifications.ForEach(delegate(TemporaryNotification n)
		{
			if (n.SpecificContainer == null)
			{
				renderables.Add(n.Drawable);
			}
			else
			{
				n.SpecificContainer.Add(n.Drawable);
			}
		});
	}

	private void OnRender()
	{
		if (!base.Session.Modal.Visible)
		{
			DrawContext drawContext = new DrawContext();
			RenderList(drawContext, alwaysOnScreenRenderables);
			if (optionsWnd.Visible)
			{
				RenderList(drawContext, mainMenuRenderables);
			}
			else if (visibleWindows.Count == 0 && !overlayHidden)
			{
				RenderList(drawContext, renderables);
			}
			m_canHideCursor.valor.valor = !drawContext.DrawCursor;
		}
	}

	private void RenderList(DrawContext rc, List<Drawable> list)
	{
		foreach (Drawable item in list)
		{
			rc.Native = item.EnableNative && item.NativeComponent != null && EnableUITK;
			rc.NativeCached = rc.Native && !item.Dirty;
			if (!rc.NativeCached)
			{
				rc.Begin();
				try
				{
					item.Draw(rc);
				}
				finally
				{
					rc.Complete();
				}
			}
		}
	}
}
