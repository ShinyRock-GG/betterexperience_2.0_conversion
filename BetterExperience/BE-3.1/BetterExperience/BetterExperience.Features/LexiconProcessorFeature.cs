using Assets._ReusableScripts.Textos;
using Assets.SingletonesAndSystemasGlobales.AbstractLayer.Globales;
using BepInEx.Configuration;
using BetterExperience.Features.Lexicon;
using BetterExperience.Features.PluginOptions;
using BetterExperience.GameScopes;

namespace BetterExperience.Features;

internal class LexiconProcessorFeature : PluginFeature
{
	public class LexiconService : SessionService
	{
		public override void OnStart()
		{
			base.OnStart();
			Lookup<LexiconProcessorFeature>().Process();
		}

		private void DumpSynomyms()
		{
			_ = Singleton<DiccionarioDeSinonimos>.instance;
		}
	}

	private ConfigEntry<bool> enableFeature;

	private LexiconProcessor processor;

	public override bool Enabled => enableFeature.Value;

	public override void Configure(ConfigFile config)
	{
		enableFeature = config.Bind<bool>("Features", "EnableLexiconProcessor", false, "Enable Lexicon Processor: dialogs export and modification");
	}

	public override void OnInit()
	{
		base.OnInit();
		Lookup<PluginOptionsService>().Expose(enableFeature, base.Scope);
	}

	public void Process()
	{
		processor.Process();
	}

	public override void OnStart()
	{
		base.OnStart();
		processor = new LexiconProcessor(Lookup<PersistenceService>());
		Lookup<SessionTracker>().InterviewServices.Add(() => new LexiconService());
	}
}
