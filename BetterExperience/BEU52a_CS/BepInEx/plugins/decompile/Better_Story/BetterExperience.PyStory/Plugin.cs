using BepInEx;
using BepInEx.Bootstrap;

namespace BetterExperience.PyStory;

[BepInPlugin("f95.betterexperience.pycs", "Better Story Mod", "1.5.2")]
[BepInDependency(/*Could not decode attribute arguments.*/)]
[BepInDependency(/*Could not decode attribute arguments.*/)]
public class Plugin : BaseUnityPlugin
{
	public static Plugin Instance { get; private set; }

	public static string version => Chainloader.PluginInfos["f95.betterexperience.pycs"].Metadata.Version.ToString();

	public void Awake()
	{
		Instance = this;
		BetterExperience.Plugin core = (BetterExperience.Plugin)(object)Chainloader.PluginInfos["f95.betterexperience"].Instance;
		core.AddService(new PyStoryFeature());
	}
}
