using System.Collections.Generic;
using BetterExperience.GameScopes;
using BetterExperience.Utils;
using PixelCrushers.DialogueSystem;

namespace BetterExperience.Features.Lexicon;

internal class ConversationProcessor : ITextProcessor
{
	public class ConversationMapping
	{
		public Dictionary<string, DialogEntryMapping> Entries { get; set; } = new Dictionary<string, DialogEntryMapping>();
	}

	public class DialogEntryMapping
	{
		public Dictionary<string, int> Links = new Dictionary<string, int>();

		public int Actor { get; set; }

		public string Text { get; set; }

		public string Condition { get; set; }

		public ConditionPriority ConditionPriority { get; set; }

		public string Script { get; set; }

		public string MenuText { get; set; }

		public DialogEntryMapping()
		{
		}

		public DialogEntryMapping(DialogueEntry e)
		{
			Actor = e.ActorID;
			Condition = e.conditionsString;
			ConditionPriority = e.conditionPriority;
			Script = e.userScript;
			Text = e.DialogueText;
			MenuText = e.MenuText;
			foreach (Link outgoingLink in e.outgoingLinks)
			{
				Links[outgoingLink.destinationDialogueID + "@" + outgoingLink.destinationConversationID] = (int)outgoingLink.priority;
			}
		}
	}

	private PersistenceService persistence;

	private Dictionary<int, ConversationMapping> mapping;

	public ConversationProcessor(PersistenceService persistence)
	{
		this.persistence = persistence;
	}

	public void Process()
	{
		bool flag = false;
		if (mapping == null)
		{
			flag = true;
			mapping = persistence.Persisted(() => new Dictionary<int, ConversationMapping>(), "lexicon/conversations", exchange: true);
		}
		foreach (Conversation conversation in DialogueManager.DatabaseManager.MasterDatabase.conversations)
		{
			if (conversation.id <= 10)
			{
				continue;
			}
			if (!mapping.TryGetValue(conversation.id, out var value))
			{
				ConversationMapping conversationMapping = (mapping[conversation.id] = new ConversationMapping());
				value = conversationMapping;
			}
			foreach (DialogueEntry e in conversation.dialogueEntries)
			{
				DialogEntryMapping valueOrAdd = value.Entries.GetValueOrAdd(e.id + "@" + conversation.id, () => new DialogEntryMapping(e));
				e.DefaultDialogueText = valueOrAdd.Text;
				e.DialogueText = valueOrAdd.Text;
				e.LocalizedDialogueText = valueOrAdd.Text;
				e.MenuText = valueOrAdd.MenuText;
				e.DefaultMenuText = valueOrAdd.MenuText;
				e.LocalizedMenuText = e.MenuText;
			}
		}
		if (flag)
		{
			persistence.Persist(mapping, "lexicon/conversations", exchange: true);
		}
	}
}
