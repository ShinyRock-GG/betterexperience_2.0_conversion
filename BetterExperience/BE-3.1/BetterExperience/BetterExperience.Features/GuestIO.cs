using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using BetterExperience.Features.Console;
using BetterExperience.Features.PluginOptions;
using BetterExperience.GameScopes;
using BetterExperience.Utils;
using BetterExperience.Wrappers.Pools;

namespace BetterExperience.Features;

internal class GuestIO : PluginFeature
{
	private class GIOService : SessionService
	{
		private class GIODump : Dictionary<string, Dictionary<string, float>>
		{
		}

		private bool dumpOnArrive;

		private PersistenceService persistence;

		public GIOService(bool dumpOnArrive)
		{
			this.dumpOnArrive = dumpOnArrive;
		}

		public override void OnInit()
		{
			base.OnInit();
			ConsoleService consoleService = Lookup<ConsoleService>();
			consoleService.RegisterCommand(CommandExport, base.Scope);
			consoleService.RegisterCommand(CommandImport, base.Scope);
			consoleService.RegisterCommand(CommandLocation, base.Scope);
			consoleService.RegisterCommand(CommandBackup, base.Scope);
			consoleService.RegisterCommand(CommandReset, base.Scope);
			consoleService.RegisterCommand(CommandEve, base.Scope);
			consoleService.RegisterCommand(CommandRandomize, base.Scope);
			persistence = Lookup<PersistenceService>();
		}

		public override void OnStart()
		{
			if (dumpOnArrive)
			{
				base.Session.OnGuestReady += delegate
				{
					CommandBackup();
				};
			}
		}

		[ConsoleCommand("Reset current guest to standard guest prefab", new string[] { "gio", "eve" })]
		private string CommandEve()
		{
			if (base.Session.Guest == null || base.Session.Guest.GuestInstance == null)
			{
				return "no guest";
			}
			base.Session.Guest.GuestInstance.Reset();
			base.Session.Guest.SynchronizeCharacterWithInstance();
			return "ok";
		}

		[ConsoleCommand("Randomize current guest", new string[] { "gio", "rand" })]
		private string CommandRandomize()
		{
			if (base.Session.Guest == null || base.Session.Guest.GuestInstance == null)
			{
				return "no guest";
			}
			base.Session.Guest.GuestInstance.Randmize();
			base.Session.Guest.SynchronizeCharacterWithInstance();
			return "ok";
		}

		[ConsoleCommand("Backup current guest variables", new string[] { "gio", "backup" })]
		private string CommandBackup()
		{
			return GenericExport(GetBackupFiles());
		}

		[ConsoleCommand("Reset current guest variables from last stored backup", new string[] { "gio", "reset" })]
		private string CommandReset()
		{
			return GenericImport(GetBackupFiles());
		}

		private string[] GetBackupFiles()
		{
			return new string[2] { "guest_last_appearance", "guest_last_personality" };
		}

		private string[] GetManualFiles()
		{
			return new string[2] { "guest_io_appearance", "guest_io_personality" };
		}

		[ConsoleCommand("Print current import/export file paths", new string[] { "gio", "files" })]
		private string CommandLocation()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("Backup:");
			string[] backupFiles = GetBackupFiles();
			stringBuilder.Append("   Appearance: ").AppendLine(backupFiles[0]);
			stringBuilder.Append("   Personality: ").AppendLine(backupFiles[1]);
			stringBuilder.AppendLine("Manual:");
			backupFiles = GetManualFiles();
			stringBuilder.Append("   Appearance: ").AppendLine(backupFiles[0]);
			stringBuilder.Append("   Personality: ").AppendLine(backupFiles[1]);
			return stringBuilder.ToString();
		}

		[ConsoleCommand("Modify current guest's apperance and behaviour with json files", new string[] { "gio", "import" })]
		private string CommandImport()
		{
			return GenericImport(GetManualFiles());
		}

		[ConsoleCommand("Save current guest's apperance and behaviour into json files", new string[] { "gio", "export" })]
		private string CommandExport()
		{
			return GenericExport(GetManualFiles());
		}

		private string GenericImport(string[] files)
		{
			if (base.Session.Guest == null)
			{
				return "Import failed: no one to export";
			}
			if (base.Session.Guest.GuestInstance == null)
			{
				return "Import failed: no readable data available";
			}
			StringBuilder stringBuilder = new StringBuilder();
			GIODump gIODump = ReadObject(stringBuilder, files[0]);
			GIODump gIODump2 = ReadObject(stringBuilder, files[1]);
			GuestInstance guestInstance = base.Session.Guest.GuestInstance;
			List<GeneInfo> list = new List<GeneInfo>();
			if (gIODump == null)
			{
				stringBuilder.AppendLine("Missing appearance data");
			}
			else
			{
				foreach (Dictionary<string, float> value2 in gIODump.Values)
				{
					foreach (KeyValuePair<string, float> item in value2)
					{
						list.Add(new GeneInfo
						{
							Id = new GeneId(item.Key),
							Value = item.Value
						});
					}
				}
			}
			if (gIODump2 == null)
			{
				stringBuilder.AppendLine("Missing personality data");
			}
			else
			{
				foreach (Dictionary<string, float> value3 in gIODump2.Values)
				{
					foreach (KeyValuePair<string, float> item2 in value3)
					{
						list.Add(new GeneInfo
						{
							Id = new GeneId(item2.Key),
							Value = item2.Value
						});
					}
				}
			}
			Dictionary<GeneId, GeneInfoEx> actual = base.Session.Guest.GuestInstance.ExtractAll();
			list.RemoveIf((GeneInfo gene) => actual.TryGetValue(gene.Id, out var value) && value.Value == gene.Value);
			if (list.Count == 0)
			{
				stringBuilder.AppendLine("Nothing written");
			}
			else
			{
				guestInstance.UpdateAll(list);
				stringBuilder.Append("Update complete. Updated ").Append(list.Count).AppendLine(" values");
				base.Session.Guest.SynchronizeCharacterWithInstance();
				stringBuilder.AppendFormat("Character synchronized");
			}
			return stringBuilder.ToString();
		}

		private string GenericExport(string[] files)
		{
			if (base.Session.Guest == null)
			{
				return "Export failed: no one to export";
			}
			if (base.Session.Guest.GuestInstance == null)
			{
				return "Export failed: no readable data available";
			}
			StringBuilder stringBuilder = new StringBuilder();
			GuestInstance guestInstance = base.Session.Guest.GuestInstance;
			GIODump appearance = Dump(guestInstance.ExtractAppearance());
			GIODump appearance2 = Dump(guestInstance.ExtractPersonality());
			WriteObject(stringBuilder, appearance, files[0]);
			WriteObject(stringBuilder, appearance2, files[1]);
			return stringBuilder.ToString();
		}

		private void WriteObject(StringBuilder reply, GIODump appearance, string fname)
		{
			persistence.Persist(appearance, fname, exchange: true);
			reply?.Append("Wrote file '").Append(fname).AppendLine("'");
		}

		private GIODump ReadObject(StringBuilder reply, string fname)
		{
			GIODump gIODump = persistence.Persisted(() => (GIODump)null, fname, exchange: true);
			if (gIODump == null)
			{
				reply.Append("File not found: ").AppendLine(fname);
				return null;
			}
			reply.Append("Read file: ").AppendLine(fname);
			return gIODump;
		}

		private GIODump Dump(Dictionary<GeneId, GeneInfoEx> dictionary)
		{
			GIODump gIODump = new GIODump();
			foreach (GeneInfoEx value in dictionary.Values)
			{
				gIODump.GetValueOrAdd(value.Group, () => new Dictionary<string, float>()).Add(value.Id.ToString(), value.Value);
			}
			return gIODump;
		}
	}

	private ConfigEntry<bool> enableGIO;

	private ConfigEntry<bool> enableDumpOnArrive;

	public override bool Enabled => enableGIO.Value;

	public override void Configure(ConfigFile config)
	{
		enableGIO = config.Bind<bool>("Features", "EnableGuestIO", true, "Enable guest IO");
		enableDumpOnArrive = config.Bind<bool>("GuestIO", "DumpOnArrive", true, "Dump guest variables on start of interview");
	}

	public override void OnInit()
	{
		Lookup<PluginOptionsService>().Expose(enableGIO, base.Scope);
		Lookup<PluginOptionsService>().Expose(enableDumpOnArrive, base.Scope);
	}

	public override void OnStart()
	{
		Lookup<SessionTracker>().SessionServices.Add(() => new GIOService(enableDumpOnArrive.Value));
	}
}
