using System.Collections.Generic;
using BetterExperience.Features.Console;
using BetterExperience.GameScopes;
using PixelCrushers.DialogueSystem;

namespace BetterExperience.Features;

internal class DialogManagerFeature : PluginFeature
{
	public class DialogManagerService : SessionService
	{
		[ConsoleCommand("Dump dialog database to file", new string[] { "dm", "dump" })]
		public class DMExport
		{
		}

		[ConsoleCommand("Import dialog database to file", new string[] { "dm", "import" })]
		public class DMImport
		{
		}

		public override void OnStart()
		{
			base.OnStart();
			Lookup<ConsoleService>().RegisterCommand<DMExport>(OnExportDialogs, base.Scope);
			Lookup<ConsoleService>().RegisterCommand<DMImport>(OnImportDialogs, base.Scope);
		}

		public string OnImportDialogs(DMImport cmd)
		{
			Dictionary<int, ConversationMapping> dictionary = Lookup<PersistenceService>().Persisted(() => (Dictionary<int, ConversationMapping>)null, "dialogs_dump", exchange: true);
			if (dictionary != null)
			{
				PatchDialogs(dictionary);
				return "done";
			}
			return "file not found";
		}

		private void PatchDialogs(Dictionary<int, ConversationMapping> patch)
		{
			foreach (Conversation conversation in DialogueManager.DatabaseManager.MasterDatabase.conversations)
			{
				if (!patch.TryGetValue(conversation.id, out var value))
				{
					continue;
				}
				foreach (DialogueEntry dialogueEntry in conversation.dialogueEntries)
				{
					if (value.Entries.TryGetValue(dialogueEntry.id + "@" + conversation.id, out var value2))
					{
						if (value2.MenuText != null)
						{
							dialogueEntry.MenuText = value2.MenuText;
							dialogueEntry.LocalizedMenuText = value2.MenuText;
							dialogueEntry.DefaultMenuText = value2.MenuText;
						}
						if (value2.Text != null)
						{
							dialogueEntry.DialogueText = value2.Text;
							dialogueEntry.LocalizedDialogueText = value2.Text;
							dialogueEntry.DefaultDialogueText = value2.Text;
						}
					}
				}
			}
		}

		public string OnExportDialogs(DMExport cmd)
		{
			Dictionary<int, ConversationMapping> dictionary = new Dictionary<int, ConversationMapping>();
			foreach (Conversation conversation in DialogueManager.DatabaseManager.MasterDatabase.conversations)
			{
				ConversationMapping conversationMapping = (dictionary[conversation.id] = new ConversationMapping());
				ConversationMapping conversationMapping3 = conversationMapping;
				foreach (DialogueEntry dialogueEntry in conversation.dialogueEntries)
				{
					DialogEntryMapping dialogEntryMapping = (conversationMapping3.Entries[dialogueEntry.id + "@" + conversation.id] = new DialogEntryMapping());
					DialogEntryMapping dialogEntryMapping3 = dialogEntryMapping;
					dialogEntryMapping3.Actor = dialogueEntry.ActorID;
					dialogEntryMapping3.Text = dialogueEntry.DialogueText;
					dialogEntryMapping3.Condition = dialogueEntry.conditionsString;
					dialogEntryMapping3.ConditionPriority = dialogueEntry.conditionPriority;
					dialogEntryMapping3.Script = dialogueEntry.userScript;
					dialogEntryMapping3.MenuText = dialogueEntry.MenuText;
					foreach (Link outgoingLink in dialogueEntry.outgoingLinks)
					{
						dialogEntryMapping3.Links[outgoingLink.destinationDialogueID + "@" + outgoingLink.destinationConversationID] = (int)outgoingLink.priority;
					}
				}
			}
			Lookup<PersistenceService>().Persist(dictionary, "dialogs_dump", exchange: true);
			return "done";
		}
	}

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
	}

	public override bool Enabled => false;

	public override void OnStart()
	{
		base.OnStart();
		Lookup<SessionTracker>().SessionServices.Add(() => new DialogManagerService());
	}
}
