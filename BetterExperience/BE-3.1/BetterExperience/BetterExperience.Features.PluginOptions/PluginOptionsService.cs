using System;
using BepInEx.Configuration;
using BetterExperience.Features.Overlay;
using BetterExperience.GameScopes;
using UnityEngine;

namespace BetterExperience.Features.PluginOptions;

internal class PluginOptionsService : PluginService
{
	public enum SettingsType
	{
		general,
		player,
		guest
	}

	private class OptionsRenderer : SessionService
	{
		private Drawable settingsBtn;

		private Drawable window;

		private GeneralSettings[] settings;

		public OptionsRenderer(Drawable settingsBtn, Drawable window, params GeneralSettings[] settings)
		{
			this.settingsBtn = settingsBtn;
			this.window = window;
			this.settings = settings;
		}

		public override void OnStart()
		{
			Lookup<OverlayService>().AddMenuDrawable(new DockingContainer(Vector2Int.down + Vector2Int.left, settingsBtn), base.Scope);
			Lookup<OverlayService>().AddMenuDrawable(new DockingContainer(Vector2Int.right, window), base.Scope);
			GeneralSettings[] array = settings;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetSession(base.Session);
			}
		}
	}

	private DrawableButton settingsBtn;

	private DrawableWindow window;

	private DrawableTabPane optionsVLayout;

	private GeneralSettings generalSettings;

	private GeneralSettings playerSettings;

	private GeneralSettings guestSettings;

	private Observable onRefresh = new Observable();

	public override void OnInit()
	{
		settingsBtn = new DrawableButton();
		settingsBtn.Position = new Vector2(50f, -30f);
		settingsBtn.Text = "BetterExperience";
		settingsBtn.OnClick += delegate
		{
			window.Visible = !window.Visible;
			if (window.Visible)
			{
				onRefresh.Invoke();
			}
		};
		window = new DrawableWindow(800, 600);
		window.Text = "BetterExperience Settings";
		window.Visible = false;
		window.CanDrag = false;
		optionsVLayout = window.Add(new DrawableScrollView
		{
			PreferredSize = new Vector2(window.ClientSize.x, window.ClientSize.y - 20f)
		}).Add(new DrawableTabPane());
		generalSettings = new GeneralSettings();
		playerSettings = new GeneralSettings();
		guestSettings = new GeneralSettings();
		AddOptions("Common", generalSettings.Component, base.Scope, generalSettings.Refresh);
		AddOptions("Player", playerSettings.Component, base.Scope, playerSettings.Refresh);
		AddOptions("Guest", guestSettings.Component, base.Scope, guestSettings.Refresh);
	}

	public void AddOptions(string title, Drawable component, ScopeSupport scope, Action refreshCallback = null)
	{
		optionsVLayout.AddTab(title, component, scope);
		if (refreshCallback != null)
		{
			onRefresh.Add(refreshCallback, base.Scope);
		}
	}

	private GeneralSettings GetSettings(SettingsType settingsType)
	{
		return settingsType switch
		{
			SettingsType.player => playerSettings, 
			SettingsType.guest => guestSettings, 
			_ => generalSettings, 
		};
	}

	public void Expose(ConfigEntry<bool> configEntry, ScopeSupport scope, SettingsType type = SettingsType.general)
	{
		GetSettings(type).AddFlag(((ConfigEntryBase)configEntry).Description.Description, () => configEntry.Value, delegate(bool v)
		{
			configEntry.Value = v;
		}, ((ConfigEntryBase)configEntry).Definition.Section, scope);
	}

	public void Expose(ConfigEntry<KeyboardShortcut> configEntry, ScopeSupport scope, SettingsType type = SettingsType.general)
	{
		GetSettings(type).AddHotkey(((ConfigEntryBase)configEntry).Description.Description, () => configEntry.Value, delegate(KeyboardShortcut v)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			configEntry.Value = v;
		}, ((ConfigEntryBase)configEntry).Definition.Section, scope);
	}

	public void Expose(ConfigEntry<float> configEntry, ScopeSupport scope, SettingsType type = SettingsType.general)
	{
		GetSettings(type).AddValue(((ConfigEntryBase)configEntry).Description.Description, () => configEntry.Value, delegate(float v)
		{
			configEntry.Value = v;
		}, ((ConfigEntryBase)configEntry).Definition.Section, scope);
	}

	public void Expose(ConfigEntry<int> configEntry, ScopeSupport scope, SettingsType type = SettingsType.general)
	{
		GetSettings(type).AddValue(((ConfigEntryBase)configEntry).Description.Description, () => configEntry.Value, delegate(float v)
		{
			configEntry.Value = (int)v;
		}, ((ConfigEntryBase)configEntry).Definition.Section, scope);
	}

	public override void OnStart()
	{
		Lookup<SessionTracker>().SessionServices.Add(() => new OptionsRenderer(settingsBtn, window, generalSettings, playerSettings, guestSettings));
	}
}
