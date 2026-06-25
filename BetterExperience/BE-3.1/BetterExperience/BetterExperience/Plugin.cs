using System;
using System.IO;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BetterExperience.Features;
using BetterExperience.Features.Console;
using BetterExperience.Features.GeneTool;
using BetterExperience.Features.Overlay;
using BetterExperience.Features.PluginOptions;
using BetterExperience.Features.SceneCameras;
using BetterExperience.GameScopes;
using BetterExperience.HarmonyPatches;
using BetterExperience.Utils;
using BetterExperience.Wrappers.Windows;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace BetterExperience;

[BepInPlugin("f95.betterexperience", "Better Experience Mod", "1.3.1")]
[BepInDependency(/*Could not decode attribute arguments.*/)]
public class Plugin : BaseUnityPlugin
{
	internal static Observable DrawGUI = new Observable();

	internal static Observable DoUpdate = new Observable();

	private ScopeSupport PluginScope = new ScopeSupport
	{
		Name = "Root"
	};

	private bool errorReported;

	public static bool MonkeyMode { get; private set; }

	public static bool MonkeyPresent { get; private set; }

	public Plugin()
	{
		ConfigEntry<bool> val = ((BaseUnityPlugin)this).Config.Bind<bool>("Features", "SuppressMonkey", false, "");
		MonkeyPresent = Chainloader.PluginInfos.ContainsKey("com.thora.monkey");
		MonkeyMode = MonkeyPresent;
		if (MonkeyPresent && val.Value)
		{
			GameObject gameObject = GameObject.Find("Monkey");
			if (gameObject != null)
			{
				gameObject.SetActive(value: false);
				MonkeyMode = false;
			}
		}
	}

	private void CreateFeatures()
	{
		PluginScope.Provide(this);
		PluginScope.Provide<ConfigFile>(((BaseUnityPlugin)this).Config);
		PluginScope.AddService(new GlobalPersistenceService());
		PluginScope.AddService(new DispatcherService(DrawGUI, DoUpdate));
		SessionTracker sessionTracker = PluginScope.AddService(new SessionTracker());
		sessionTracker.SessionServices.Add(() => new GameOptionsWindow());
		sessionTracker.InterviewServices.Add(() => new GeneToolWindow());
		PluginScope.AddService(new OverlayFeature());
		PluginScope.AddService(new ConsoleFeature());
		PluginScope.AddService(new PluginOptionsService());
		if (!MonkeyMode)
		{
			PluginScope.AddService(new SceneCameraFeature());
		}
		PluginScope.AddService(new AlternativeGeneticsFeature());
		if (!MonkeyMode)
		{
			PluginScope.AddService(new PlayerScaler());
		}
		PluginScope.AddService(new BetterHandFeature());
		PluginScope.AddService(new GuestIO());
		PluginScope.AddService(new NoMeansNoFeature());
		PluginScope.AddService(new EmoSpy());
		PluginScope.AddService(new DragControl());
		PluginScope.AddService(new NotAMicFeature());
		PluginScope.AddService(new NaturalLanguageFeature());
		PluginScope.AddService(new GeneWatchFeature());
		PluginScope.AddService(new SkipTextFeature());
		PluginScope.AddService(new VelocityControlFeature());
		PluginScope.AddService(new MultithreadingFeature());
		PluginScope.AddService(new LipsDeformerFeature());
		sessionTracker.SessionServices.Add(() => new TestFeature());
		PluginScope.AddService(new AmateurModelFeature());
		PluginScope.AddService(new AutoThrustFeature());
		PluginScope.AddService(new DialogManagerFeature());
		PluginScope.AddService(new MissionControlFeature());
		PluginScope.AddService(new LexiconProcessorFeature());
		PluginScope.AddService(new AutoSeekerFeature());
		PluginScope.AddService(new PlayerPostureFeature());
		PluginScope.AddService(new SingleGroupFeature());
		PluginScope.AddService(new AutoRateGuestFeature());
	}

	public T AddService<T>(T instance) where T : PluginService
	{
		return PluginScope.AddService(instance);
	}

	public void Awake()
	{
		Logger.LoggerImpl = ((BaseUnityPlugin)this).Logger;
		try
		{
			PluginScope.OnException += PluginScope_OnException;
			CreateFeatures();
			PluginScope.Start();
			Harmony.CreateAndPatchAll(typeof(SMAGlobalPatches), (string)null);
		}
		catch (Exception ex)
		{
			((BaseUnityPlugin)this).Logger.LogError((object)ex);
			new MainModalWindow().MessageError("BetterExperience is totally doomed\nStartupError: " + ex.Message + "\nMod definitely won't work properly");
			TryShowStacktrace(ex);
		}
	}

	private void PluginScope_OnException(ScopeSupport.ScopeExceptionEvent obj)
	{
		obj.Handled = true;
		new Logger().Error(obj.Exception, "Shutting down scope {0} due to uncaught exception", obj.Sender.Name);
		obj.Sender.Dispose();
		if (!errorReported)
		{
			errorReported = true;
			new MainModalWindow().MessageError("BetterExperience is doomed\n" + obj.Sender.Name + ": " + obj.Exception.Message + "\nNo further error reporting.");
			TryShowStacktrace(obj.Exception);
		}
	}

	private void TryShowStacktrace(Exception e)
	{
		try
		{
			GameSession current = PluginScope.Lookup<SessionTracker>().Current;
			if (current != null)
			{
				ScopeSupport scopeSupport = new ScopeSupport();
				current.Scope.AddChild(scopeSupport);
				current.Scope.Lookup<OverlayService>().AddDrawable(new StacktraceWindow(e, scopeSupport), scopeSupport);
			}
		}
		catch (Exception ex)
		{
			((BaseUnityPlugin)this).Logger.LogError((object)ex);
		}
	}

	public static string GetConfigFilePath(params string[] relname)
	{
		return Path.Combine(Paths.ConfigPath, "f95.betterexperience", Path.Combine(relname));
	}

	public static T LoadExistingJsonObject<T>(params string[] name)
	{
		string configFilePath = GetConfigFilePath(name);
		if (File.Exists(configFilePath))
		{
			return (T)JsonConvert.DeserializeObject(File.ReadAllText(configFilePath), typeof(T));
		}
		new Logger().Warn("Missing expected file {0}", configFilePath);
		return default(T);
	}

	public static void SaveJsonObject<T>(T obj, params string[] name)
	{
		string configFilePath = GetConfigFilePath(name);
		string contents = JsonConvert.SerializeObject(obj);
		File.WriteAllText(configFilePath, contents);
	}
}
