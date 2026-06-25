using System;
using System.Collections.Generic;
using BetterExperience.Features.Overlay;
using BetterExperience.GameScopes;
using UnityEngine;

namespace BetterExperience.Features.AlternativeGenetics;

internal class PoolSettingsWindow
{
	private GeneticsFactory factory;

	private GameSession gameSession;

	private VLayout<Drawable> root;

	private Observable Refresh = new Observable();

	private PoolViewer viewer;

	private bool Debug { get; set; }

	public DockingContainer ViewerAnchor { get; private set; }

	public Drawable Drawable => root;

	public PoolSettingsWindow(GeneticsFactory factory, GameSession gameSession)
	{
		PoolSettingsWindow poolSettingsWindow = this;
		this.factory = factory;
		this.gameSession = gameSession;
		root = new VLayout<Drawable>();
		root.Add(new DrawableLabel("Pool management"));
		GridLayout gridLayout = root.Add(new GridLayout(1f, 1f));
		gridLayout.Add(MessageButton("E/D-flag", "Should pool modify visitor?\nDisabled pool produces random genes and ignores ratings"));
		gridLayout.Add(MessageButton("Name", "Pool name"));
		gridLayout.Add(MessageButton("Size", "Current old generation size"));
		gridLayout.Add(MessageButton("Capacity", "Current maximum old generation size"));
		gridLayout.Add(MessageButton("Genes", "Gene count in pool"));
		gridLayout.Add(MessageButton("Unguided", "Unguided gene count"));
		gridLayout.Add(MessageButton("Guidance", "Controls whether genes are volatile or restricted by tuning"));
		gridLayout.Add(MessageButton("Cmds", "Misc commands"), newline: true);
		Dictionary<string, GenePool> pools = new Dictionary<string, GenePool>();
		foreach (PoolingGroup value in factory.Groups.Values)
		{
			foreach (KeyValuePair<string, GenePool> pool2 in value.Pools)
			{
				pools[pool2.Key] = pool2.Value;
			}
		}
		foreach (GenePool pool in pools.Values)
		{
			DrawableButton enabledState = gridLayout.Add(new DrawableButton());
			gridLayout.Add(new DrawableLabel(pool.Data.Settings.Name));
			DrawableLabel size = gridLayout.Add(new DrawableLabel(""));
			DrawableLabel capacity = gridLayout.Add(new DrawableLabel(""));
			DrawableLabel genes = gridLayout.Add(new DrawableLabel(""));
			DrawableLabel unguidedGenes = gridLayout.Add(new DrawableLabel(""));
			DrawableButton guidanceBtn = gridLayout.Add(new DrawableButton(""));
			HLayout<Drawable> hLayout = gridLayout.Add(new HLayout<Drawable>(), newline: true);
			hLayout.Add(new DrawableButton("Reset")).OnClick += delegate
			{
				poolSettingsWindow.OnResetPool(pool);
			};
			hLayout.Button("View").OnClick += delegate
			{
				poolSettingsWindow.OnViewPool(pool);
			};
			hLayout.Button("Restart").OnClick += AskAndRun(() => "Are you sure you want to clear intermediate pools?", pool.Restart);
			guidanceBtn.OnClick += AskAndRun(() => "Are you sure you want to toggle pool guidance?", delegate
			{
				pool.Data.GuidanceDisabled = !pool.Data.GuidanceDisabled;
			});
			enabledState.OnClick += delegate
			{
				poolSettingsWindow.OnToggleEnabled(pool);
			};
			Refresh.Add(delegate
			{
				enabledState.Text = (pool.Data.Enabled ? "enabled" : "disabled");
				size.Text = pool.GetGeneration(GeneGeneration.Old).Count.ToString();
				capacity.Text = pool.Capacity.ToString();
				genes.Text = pool.Data.GeneOrder.Count.ToString();
				unguidedGenes.Text = pool.UnguidedGeneCount().ToString();
				guidanceBtn.Text = (pool.Data.GuidanceDisabled ? "Unguided" : "Guided");
			});
		}
		Refresh.Invoke();
		root.Add(new DrawableLabel("Batch toggle:"));
		HLayout<DrawableButton> hLayout2 = root.Add(new HLayout<DrawableButton>());
		hLayout2.Add(new DrawableButton("Enable All")).OnClick += AskAndRun(() => "Are you sure you want to enable all pools?", delegate
		{
			foreach (GenePool value2 in pools.Values)
			{
				value2.Data.Enabled = true;
			}
		});
		hLayout2.Add(new DrawableButton("Disable All")).OnClick += AskAndRun(() => "Are you sure you want to disable all pools?", delegate
		{
			foreach (GenePool value3 in pools.Values)
			{
				value3.Data.Enabled = false;
			}
		});
		ViewerAnchor = new DockingContainer(Vector2Int.right);
	}

	internal void RefreshAll()
	{
		Refresh.Invoke();
	}

	private void OnViewPool(GenePool pool)
	{
		if (gameSession.Guest == null)
		{
			gameSession.Modal.MessageError("Available during interview only");
			return;
		}
		if (viewer != null)
		{
			viewer.Restore();
			ViewerAnchor.Remove(viewer.Window);
			viewer = null;
		}
		viewer = new PoolViewer(pool, gameSession);
		ViewerAnchor.Add(viewer.Window);
		gameSession.Modal.MessageError("Close menu to use viewer UI");
	}

	private Action AskAndRun(Func<string> text, Action action)
	{
		return delegate
		{
			gameSession.Modal.MessageBoxYesNo(text()).OnResult += delegate(bool result)
			{
				if (result)
				{
					action();
					Refresh.Invoke();
					gameSession.Modal.MessageError("Done");
				}
			};
		};
	}

	private void OnToggleEnabled(GenePool pool)
	{
		gameSession.Modal.MessageBoxYesNo("Are you sure you want to " + (pool.Data.Enabled ? "disable" : "enable") + " pool " + pool.Data.Settings.Name + "?").OnResult += delegate(bool result)
		{
			if (result)
			{
				pool.Data.Enabled = !pool.Data.Enabled;
				gameSession.Modal.MessageError("Done");
				Refresh.Invoke();
			}
		};
	}

	private void OnToggleActive(GenePool pool)
	{
		gameSession.Modal.MessageBoxYesNo("Are you sure you want to " + (pool.Data.Active ? "deactivate" : "activate") + " pool " + pool.Data.Settings.Name + "?").OnResult += delegate(bool result)
		{
			if (result)
			{
				pool.Data.Active = !pool.Data.Active;
				gameSession.Modal.MessageError("Done");
				Refresh.Invoke();
			}
		};
	}

	private void OnResetPool(GenePool pool)
	{
		gameSession.Modal.MessageBoxYesNo("Are you sure you want to clear pool " + pool.Data.Settings.Name + "?").OnResult += delegate(bool result)
		{
			if (result)
			{
				pool.Clear();
				gameSession.Modal.MessageError("Done");
				Refresh.Invoke();
			}
		};
	}

	private DrawableButton MessageButton(string text, string message)
	{
		DrawableButton drawableButton = new DrawableButton();
		drawableButton.Text = text;
		drawableButton.OnClick += delegate
		{
			gameSession.Modal.MessageError(message);
		};
		return drawableButton;
	}

	private void OnChangeGuaranteedRandoms(GenePool pool)
	{
		gameSession.Modal.RequestInput("Type minimal random guest count per week (>=0)", pool.GuaranteedRandoms.ToString()).OnResult += delegate(string value)
		{
			if (value != null)
			{
				try
				{
					int num = int.Parse(value);
					if (num < 0)
					{
						gameSession.Modal.MessageError("Input error: positive or zero value expected");
					}
					else
					{
						pool.Data.GuaranteedRandoms = num;
						gameSession.Modal.MessageError("Done");
					}
				}
				catch (Exception ex)
				{
					gameSession.Modal.MessageError("Input error: " + ex.Message);
				}
			}
			Refresh.Invoke();
		};
	}
}
