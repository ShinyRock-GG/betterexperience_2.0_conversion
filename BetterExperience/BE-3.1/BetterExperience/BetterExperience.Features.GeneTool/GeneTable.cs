using System;
using System.Collections.Generic;
using System.Linq;
using BetterExperience.Features.Overlay;
using BetterExperience.GameScopes;
using BetterExperience.Utils;
using BetterExperience.Wrappers.Pools;
using UnityEngine;

namespace BetterExperience.Features.GeneTool;

internal class GeneTable
{
	private class GeneRow
	{
		internal string id;

		internal Drawable row;

		internal DrawableButton value;

		internal DrawableButton zero;

		internal DrawableButton half;

		internal DrawableButton one;

		internal DrawableButton watchToggle;

		internal bool watched;

		internal float lastValue;

		internal string group;

		internal DrawableButton subtractStep;

		internal DrawableButton addStep;
	}

	private DrawableTextBox filter;

	private string actualFilter;

	private Dictionary<string, GeneRow> rows = new Dictionary<string, GeneRow>();

	private GridLayout grid;

	private float step = 0.1f;

	private DrawableButton stepbtn;

	private DrawableButton stepDownAll;

	private DrawableButton stepUpAll;

	public GameSession Session { get; internal set; }

	public Drawable Root { get; }

	public GeneWatchFeature GeneWatch { get; set; }

	public GeneFactoryInfo GeneFactory { get; internal set; }

	public Dictionary<string, string> Substitutons { get; } = new Dictionary<string, string>();

	public event Action<string> EditGene = delegate
	{
	};

	public event Action<string, float> SetGene = delegate
	{
	};

	public event Action<(string, float)[]> UpdateGenes = delegate
	{
	};

	public GeneTable()
	{
		VLayout<Drawable> vLayout = new VLayout<Drawable>();
		CreateFilter(vLayout);
		CreateControlRow(vLayout);
		DrawableScrollView drawableScrollView = vLayout.ScrollPane();
		drawableScrollView.PreferredSize = new Vector2(780f, 500f);
		grid = drawableScrollView.Grid();
		Root = vLayout;
	}

	private void CreateFilter(VLayout<Drawable> root)
	{
		HLayout<Drawable> builder = root.HLayout();
		builder.Label("Filter:");
		filter = builder.TextBox();
		filter.PreferredSize = new Vector2(300f, 20f);
		filter.OnTextChange += OnFilterChanged;
		builder.Label("Type <gene name> or .<gene group name> or !w for watched");
	}

	private void CreateControlRow(VLayout<Drawable> root)
	{
		HLayout<Drawable> builder = root.HLayout();
		builder.Label("Batch controls:  ");
		builder.Label("step: ");
		stepbtn = builder.Button(step.ToString());
		builder.Label("   ");
		stepDownAll = builder.Button("--");
		stepUpAll = builder.Button("++");
		stepDownAll.OnClick += delegate
		{
			Step(null, -1);
		};
		stepUpAll.OnClick += delegate
		{
			Step(null, 1);
		};
		stepbtn.OnClick += delegate
		{
			Session.Modal.RequestInput("Input new step", step.ToString()).OnResult += delegate(string result)
			{
				if (result != null)
				{
					try
					{
						step = float.Parse(result);
						stepbtn.Text = step.ToString();
					}
					catch (Exception ex)
					{
						new Logger().Error(ex, "failed to parse");
					}
				}
			};
		};
	}

	private void Step(string fixedGene, int dir)
	{
		List<GeneRow> list = new List<GeneRow>();
		if (fixedGene != null)
		{
			if (rows.TryGetValue(fixedGene, out var value))
			{
				list.Add(value);
			}
		}
		else
		{
			foreach (GeneRow value3 in rows.Values)
			{
				if (!value3.row.Transient)
				{
					list.Add(value3);
				}
			}
		}
		List<(string, float)> list2 = new List<(string, float)>();
		foreach (GeneRow item in list)
		{
			float value2 = item.lastValue + step * (float)dir;
			value2 = Mathf.Clamp01(value2);
			if (value2 != item.lastValue)
			{
				list2.Add((item.id, value2));
			}
		}
		if (list2.Count > 0)
		{
			this.UpdateGenes(list2.ToArray());
		}
	}

	private void OnFilterChanged()
	{
		string text = filter.Text;
		if (text.Length < 2)
		{
			text = null;
		}
		if (actualFilter != text)
		{
			actualFilter = text;
			RunFilter((actualFilter != null) ? actualFilter.ToLower() : null);
		}
	}

	private void RunFilter(string filter)
	{
		bool flag = "!w" == filter;
		string text = ((filter != null && filter.StartsWith(".")) ? filter.Substring(1) : null);
		foreach (KeyValuePair<string, GeneRow> row in rows)
		{
			if (flag)
			{
				row.Value.row.Transient = !row.Value.watched;
			}
			else if (text != null)
			{
				row.Value.row.Transient = row.Value.group != null && !row.Value.group.ToLower().Contains(text);
			}
			else
			{
				row.Value.row.Transient = filter != null && !row.Key.ToLower().Contains(filter);
			}
		}
	}

	internal void SetGenes(Dictionary<GeneId, GeneInfoEx>.ValueCollection values)
	{
		foreach (GeneInfoEx item in from x in values.ToList()
			orderby x.Id.ToString()
			select x)
		{
			string gene = item.Id.ToString();
			GeneRow valueOrAdd = rows.GetValueOrAdd(gene, () => CreateGeneRow(gene));
			valueOrAdd.value.Text = item.Value.ToString("F4");
			valueOrAdd.lastValue = item.Value;
			if (valueOrAdd.watched)
			{
				GeneWatch.SetWatchState(gene, item.Value);
			}
		}
	}

	private GeneRow CreateGeneRow(string gene)
	{
		GeneRow row = new GeneRow();
		row.id = gene;
		using (grid.Row())
		{
			if (GeneFactory != null)
			{
				string item = new GeneId(gene).Item1;
				if (GeneFactory.GeneToGroup.TryGetValue(item, out var value))
				{
					row.group = value;
				}
			}
			string text = gene;
			foreach (KeyValuePair<string, string> substituton in Substitutons)
			{
				text = text.Replace(substituton.Key, substituton.Value);
			}
			grid.Label(text);
			row.value = grid.Button("?");
			grid.Label("     ");
			row.zero = grid.Button(">0<");
			row.half = grid.Button(">0.5<");
			row.one = grid.Button(">1<");
			grid.Label("     ");
			row.watchToggle = grid.Button("?");
			grid.Label("     ");
			row.subtractStep = grid.Button("--");
			row.addStep = grid.Button("++");
			row.row = grid.Children[grid.Children.Count - 1];
			row.value.OnClick += delegate
			{
				this.EditGene(gene);
			};
			row.zero.OnClick += delegate
			{
				this.SetGene(gene, 0f);
			};
			row.half.OnClick += delegate
			{
				this.SetGene(gene, 0.5f);
			};
			row.one.OnClick += delegate
			{
				this.SetGene(gene, 1f);
			};
			row.addStep.OnClick += delegate
			{
				Step(gene, 1);
			};
			row.subtractStep.OnClick += delegate
			{
				Step(gene, -1);
			};
			row.watched = GeneWatch.IsWatched(gene);
			Action updateWatchButtonText = delegate
			{
				row.watchToggle.Text = (row.watched ? "[Unwatch]" : "[Watch]");
			};
			updateWatchButtonText();
			row.watchToggle.OnClick += delegate
			{
				row.watched = !row.watched;
				updateWatchButtonText();
				if (row.watched)
				{
					GeneWatch.SetWatchState(gene, row.lastValue);
				}
				else
				{
					GeneWatch.SetWatchState(gene, float.NaN);
				}
			};
			row.row.OffscreenHint = true;
		}
		return row;
	}
}
