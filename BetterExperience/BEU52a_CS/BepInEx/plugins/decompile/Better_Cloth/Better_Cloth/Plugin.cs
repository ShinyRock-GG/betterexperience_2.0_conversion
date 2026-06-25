using BepInEx;
using BepInEx.Bootstrap;
using BetterExperience;

namespace Better_Cloth;

[BepInPlugin("f95.betterexperience.cloth", "Better Cloth Mod", "1.5.2")]
[BepInDependency(/*Could not decode attribute arguments.*/)]
public class Plugin : BaseUnityPlugin
{
	public void Awake()
	{
		BetterExperience.Plugin core = (BetterExperience.Plugin)(object)Chainloader.PluginInfos["f95.betterexperience"].Instance;
		core.AddService(new ClothFeature());
	}
}
