using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BetterExperience.Features;

namespace BetterExperience.CustomScene;

[BepInPlugin("f95.betterexperience.cs", "Better Scene Mod", "1.5.2")]
[BepInDependency(/*Could not decode attribute arguments.*/)]
public class Plugin : BaseUnityPlugin
{
	public void Awake()
	{
		BetterExperience.Plugin core = (BetterExperience.Plugin)(object)Chainloader.PluginInfos["f95.betterexperience"].Instance;
		CustomSceneFeature cs = core.AddService(new CustomSceneFeature(((BaseUnityPlugin)this).Config));
		cs.Scope.Provide<ConfigFile>(((BaseUnityPlugin)this).Config);
		cs.Scope.AddService(new AnimateUndressFeature());
		cs.Scope.AddService(new AnimateGotoFeature());
		cs.Scope.AddService(new AnimatePostureChangeFeature());
		cs.Scope.AddService(new ActorControllerTuningFeature());
		core.AddService(new ProxyVolumeFeature());
	}
}
