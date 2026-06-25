using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BetterExperience.Features.Console;
using BetterExperience.GameScopes;
using BetterExperience.Wrappers.Pools;

namespace BetterExperience.Features.AlternativeGenetics;

internal class ConsoleCommands
{
	[ConsoleCommand("Restart alternative genetics", new string[] { "ag", "restart" })]
	public class RestartAG
	{
	}

	[ConsoleCommand("Create new genetic profile", new string[] { "ag", "np" })]
	public class NewProfileCmd
	{
		[ConsoleCommandArg(Key = "from", Name = "source profile")]
		public string SourceProfile { get; set; }

		[ConsoleCommandArg(Key = "of", Name = "group id")]
		public string Group { get; set; }

		[ConsoleCommandArg(Key = "profile_id", Mode = ConsoleArgMode.Tail, Name = "new profile id")]
		public string ProfileId { get; set; }
	}

	[ConsoleCommand("Select genetic profile", new string[] { "ag", "sp" })]
	public class SelectProfileCmd
	{
		[ConsoleCommandArg(Key = "of", Name = "group id")]
		public string Group { get; set; }

		[ConsoleCommandArg(Key = "profile_id", Mode = ConsoleArgMode.Tail, Name = "profile id")]
		public string ProfileId { get; set; }
	}

	[ConsoleCommand("Reset genetic profile", new string[] { "ag", "rp" })]
	public class ResetProfileCmd
	{
		[ConsoleCommandArg(Key = "of", Name = "group id")]
		public string Group { get; set; }
	}

	[ConsoleCommand("Dump current old generations as CSV", new string[] { "ag", "dump" })]
	public class PoolDumpCsv
	{
		[ConsoleCommandArg(Key = "pool", Mode = ConsoleArgMode.Tail, Name = "Pool to compute")]
		public string Pool { get; set; }
	}

	[ConsoleCommand("Score current guest agains pool and export as CSV", new string[] { "ag", "score" })]
	public class GuestScoreQuery
	{
		[ConsoleCommandArg(Key = "pool", Mode = ConsoleArgMode.Tail, Name = "Pool to score against")]
		public string Pool { get; set; }
	}

	[ConsoleCommand("Compute distance distribution between classes and export as CSV", new string[] { "ag", "dist" })]
	public class PoolCrossClassScore
	{
		[ConsoleCommandArg(Key = "pool", Mode = ConsoleArgMode.Tail, Name = "Pool to compute")]
		public string Pool { get; set; }

		[ConsoleCommandArg(Key = "pc", Mode = ConsoleArgMode.KeyValue, Name = "use perceptive mode")]
		public float Perceptive { get; set; }
	}

	private class SimpleCsv
	{
		private int cols;

		private List<string[]> rows = new List<string[]>();

		public string this[int row, int col]
		{
			get
			{
				for (int i = rows.Count; i <= row; i++)
				{
					rows.Add(new string[cols]);
				}
				return rows[row][col];
			}
			set
			{
				for (int i = rows.Count; i <= row; i++)
				{
					rows.Add(new string[cols]);
				}
				rows[row][col] = value;
			}
		}

		public SimpleCsv(int cols)
		{
			this.cols = cols;
		}

		internal void Write(string fname)
		{
			List<string> list = new List<string>();
			foreach (string[] row in rows)
			{
				list.Add(string.Join(";", row));
			}
			File.WriteAllLines(fname, list);
		}
	}

	private AlternativeGeneticsService alternativeGenetics;

	private PersistenceService persistence;

	public ConsoleCommands(AlternativeGeneticsService alternativeGenetics)
	{
		this.alternativeGenetics = alternativeGenetics;
		persistence = alternativeGenetics.Scope.Lookup<PersistenceService>();
	}

	public string RestartAgCmd(RestartAG query)
	{
		if (alternativeGenetics.Session.Guest != null)
		{
			return "Not available during interview";
		}
		alternativeGenetics.Factory.Save();
		ScopeSupport parent = alternativeGenetics.Scope.Parent;
		alternativeGenetics.Scope.Dispose();
		parent.AddService(new AlternativeGeneticsService());
		return "ok";
	}

	public string NewProfile(NewProfileCmd cmd)
	{
		StringBuilder stringBuilder = new StringBuilder();
		ExecCreateProfile(stringBuilder, cmd);
		return stringBuilder.ToString();
	}

	private void ExecCreateProfile(StringBuilder reply, NewProfileCmd cmd)
	{
		if (cmd.ProfileId == null)
		{
			reply.AppendLine("Missing mandatory profile name");
			return;
		}
		if (cmd.Group == null)
		{
			reply.AppendLine("Missing mandatory group id");
			return;
		}
		if (!alternativeGenetics.Factory.Groups.TryGetValue(cmd.Group, out var _))
		{
			reply.Append("No group found: ").Append(cmd.Group).AppendLine();
			return;
		}
		if (alternativeGenetics.Factory.LoadGroupProfile(cmd.Group, cmd.ProfileId) != null)
		{
			reply.Append("Profile already exists: ").AppendLine(cmd.ProfileId);
			return;
		}
		PoolingGroupData poolingGroupData = null;
		if (cmd.SourceProfile != null)
		{
			poolingGroupData = alternativeGenetics.Factory.LoadGroupProfile(cmd.Group, cmd.SourceProfile);
			if (poolingGroupData == null)
			{
				reply.Append("No source profile found: ").AppendLine(cmd.SourceProfile);
				return;
			}
		}
		if (poolingGroupData == null)
		{
			poolingGroupData = alternativeGenetics.Factory.LoadGroupProfile(cmd.Group, "default");
			foreach (KeyValuePair<string, GenePoolData> pool in poolingGroupData.Pools)
			{
				foreach (KeyValuePair<GeneGeneration, List<GeneSet>> generation in pool.Value.Generations)
				{
					generation.Value.Clear();
				}
				pool.Value.Epoch = 0;
				pool.Value.Error = 0f;
			}
		}
		poolingGroupData.Settings.Profile = cmd.ProfileId;
		alternativeGenetics.Factory.SaveProfile(cmd.Group, poolingGroupData);
		reply.AppendLine("Profile created");
	}

	public string SelectProfile(SelectProfileCmd cmd)
	{
		StringBuilder stringBuilder = new StringBuilder();
		ExecSelectProfile(stringBuilder, cmd);
		return stringBuilder.ToString();
	}

	private void ExecSelectProfile(StringBuilder reply, SelectProfileCmd cmd)
	{
		if (alternativeGenetics.Session.Guest != null)
		{
			reply.AppendLine("Not available during interview");
			return;
		}
		if (cmd.ProfileId == null)
		{
			reply.AppendLine("Missing mandatory profile name");
			return;
		}
		if (cmd.Group == null)
		{
			reply.AppendLine("Missing mandatory group id");
			return;
		}
		if (!alternativeGenetics.Factory.Groups.TryGetValue(cmd.Group, out var value))
		{
			reply.Append("No group found: ").Append(cmd.Group).AppendLine();
			return;
		}
		if (value.Data.Settings.Profile == cmd.ProfileId)
		{
			reply.AppendLine("Already selected");
			return;
		}
		if (alternativeGenetics.Factory.LoadGroupProfile(cmd.Group, cmd.ProfileId) == null)
		{
			reply.Append("Profile does not exist: ").AppendLine(cmd.ProfileId);
			return;
		}
		alternativeGenetics.Factory.SetProfile(cmd.Group, cmd.ProfileId);
		alternativeGenetics.Factory.Save();
		ScopeSupport parent = alternativeGenetics.Scope.Parent;
		alternativeGenetics.Scope.Dispose();
		parent.AddService(new AlternativeGeneticsService());
		reply.AppendLine("Profile set");
	}

	public string ResetProfile(ResetProfileCmd cmd)
	{
		StringBuilder stringBuilder = new StringBuilder();
		ExecResetProfile(stringBuilder, cmd);
		return stringBuilder.ToString();
	}

	private void ExecResetProfile(StringBuilder reply, ResetProfileCmd cmd)
	{
		if (alternativeGenetics.Session.Guest != null)
		{
			reply.AppendLine("Not available during interview");
			return;
		}
		if (!alternativeGenetics.Factory.Groups.TryGetValue(cmd.Group, out var value))
		{
			reply.Append("No group found: ").Append(cmd.Group).AppendLine();
			return;
		}
		foreach (KeyValuePair<string, GenePool> pool in value.Pools)
		{
			pool.Value.Clear();
		}
		reply.AppendLine("Reset");
	}

	public string PoolDumpCmd(PoolDumpCsv query)
	{
		if (!alternativeGenetics.Factory.GetPools().TryGetValue(query.Pool, out var value))
		{
			return "No such pool " + query.Pool;
		}
		List<GeneSet> generation = value.GetGeneration(GeneGeneration.Old);
		SimpleCsv simpleCsv = new SimpleCsv(value.Data.GeneOrder.Count + 1);
		simpleCsv[0, 0] = "index";
		for (int i = 0; i < value.Data.GeneOrder.Count; i++)
		{
			simpleCsv[0, i + 1] = value.Data.GeneOrder[i];
		}
		for (int j = 0; j < generation.Count; j++)
		{
			GeneSet geneSet = generation[j];
			simpleCsv[j + 1, 0] = "old_" + j + "_" + (int)(geneSet.Rating.Average() * 10f);
			for (int k = 0; k < value.Data.GeneOrder.Count; k++)
			{
				if (geneSet.Vector.Length > k)
				{
					simpleCsv[j + 1, k + 1] = $"{geneSet.Vector[k]:0.000}";
				}
			}
		}
		GeneSet[] specialDumpVectors = value.GetSpecialDumpVectors();
		for (int l = 0; l < specialDumpVectors.Length; l++)
		{
			GeneSet geneSet2 = specialDumpVectors[l];
			simpleCsv[l + 1 + generation.Count, 0] = "spec_" + geneSet2.Id;
			for (int m = 0; m < value.Data.GeneOrder.Count; m++)
			{
				if (geneSet2.Vector.Length > m)
				{
					simpleCsv[l + 1 + generation.Count, m + 1] = $"{geneSet2.Vector[m]:0.000}";
				}
			}
		}
		string text = Path.Combine(persistence.ExchangeDir, "gene_dump_" + query.Pool + ".csv");
		simpleCsv.Write(text);
		return "Saved to " + text;
	}

	public string ScoreQuery(GuestScoreQuery query)
	{
		if (alternativeGenetics.Session.Guest == null)
		{
			return "No guest";
		}
		Dictionary<GeneId, GeneInfoEx> dictionary = alternativeGenetics.Session.Guest.GuestInstance.ExtractAll();
		if (!alternativeGenetics.Factory.GetPools().TryGetValue(query.Pool, out var value))
		{
			return "No such pool " + query.Pool;
		}
		List<GeneSet> generation = value.GetGeneration(GeneGeneration.Old);
		GeneSet geneSet = value.ExtractGeneSet(dictionary.Values, norate: true);
		SimpleCsv simpleCsv = new SimpleCsv(generation.Count + 1);
		simpleCsv[0, 0] = "score";
		for (int i = 0; i < generation.Count; i++)
		{
			float num = value.SimilarityDistance(generation[i], geneSet);
			simpleCsv[0, 1 + i] = $"{num:0.000}";
		}
		for (int j = 0; j < geneSet.Vector.Length; j++)
		{
			simpleCsv[1 + j, 0] = value.Data.GeneOrder[j];
			for (int k = 0; k < generation.Count; k++)
			{
				simpleCsv[1 + j, 1 + k] = $"{Math.Abs(generation[k].Vector[j] - geneSet.Vector[j]):0.00000}";
			}
		}
		string text = Path.Combine(persistence.ExchangeDir, "gene_delta_" + query.Pool + ".csv");
		simpleCsv.Write(text);
		return "Saved to " + text;
	}

	public string DistributionQuery(PoolCrossClassScore query)
	{
		if (!alternativeGenetics.Factory.GetPools().TryGetValue(query.Pool, out var value))
		{
			return "No such pool " + query.Pool;
		}
		List<GeneSet> generation = value.GetGeneration(GeneGeneration.Old);
		PerceptionScoring perceptionScoring = new PerceptionScoring(query.Perceptive, value);
		SimpleCsv simpleCsv = new SimpleCsv(generation.Count);
		for (int i = 0; i < generation.Count; i++)
		{
			for (int j = 0; j < generation.Count; j++)
			{
				float num = ((query.Perceptive > 0f) ? perceptionScoring.Score(generation[i], new GeneSet[1] { generation[j] }.ToList())[0] : value.SimilarityDistance(generation[i], generation[j]));
				simpleCsv[i, j] = $"{num:0.000}";
			}
		}
		string text = Path.Combine(persistence.ExchangeDir, "gene_crossdist_" + query.Pool + ".csv");
		simpleCsv.Write(text);
		return "Saved to " + text;
	}
}
