using System;
using BetterExperience.GameScopes;

namespace BetterExperience.Features.Lexicon;

internal class LexiconProcessor : IDisposable
{
	private ITextProcessor[] processors;

	public LexiconProcessor(PersistenceService service)
	{
		processors = new ITextProcessor[4]
		{
			new BodyPartsProcessor(service),
			new WordsProcessor(service),
			new ExpressionsProcessor(service),
			new ConversationProcessor(service)
		};
	}

	public void Process()
	{
		ITextProcessor[] array = processors;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Process();
		}
	}

	public void Dispose()
	{
	}
}
