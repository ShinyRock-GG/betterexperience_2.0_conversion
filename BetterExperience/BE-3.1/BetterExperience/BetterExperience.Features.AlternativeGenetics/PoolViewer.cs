using System;
using System.Collections.Generic;
using BetterExperience.Features.Overlay;
using BetterExperience.GameScopes;
using BetterExperience.Wrappers.Pools;

namespace BetterExperience.Features.AlternativeGenetics;

internal class PoolViewer
{
	private GenePool pool;

	private GameSession session;

	private GeneSet backupGS;

	public Dictionary<GeneSet, Action<bool>> activeToggle = new Dictionary<GeneSet, Action<bool>>();

	public DrawableWindow Window { get; private set; }

	public PoolViewer(GenePool pool, GameSession session)
	{
		this.pool = pool;
		this.session = session;
		GuestInstance guestInstance = session.Guest.GuestInstance;
		backupGS = pool.ExtractGeneSet(guestInstance.ExtractAll().Values, norate: true);
		CreateGUI();
	}

	private void CreateGUI()
	{
		DrawableWindow drawableWindow = new DrawableWindow(300, 600);
		VLayout<Drawable> builder = drawableWindow.ScrollPane().VLayout();
		builder.Button("Close").OnClick += OnClose;
		builder.Label("Pool viewer: " + pool.Data.Settings.Name);
		builder.Button("Reset").OnClick += delegate
		{
			ApplyGS(backupGS);
		};
		GridLayout gridLayout = builder.Grid();
		int num = 0;
		foreach (GeneSet gs in pool.GetGeneration(GeneGeneration.Old))
		{
			using (gridLayout.Row())
			{
				string text = "geneset-" + num++ + "[" + gs.GenAttempts + "]";
				gridLayout.Button("load").OnClick += delegate
				{
					ApplyGS(gs);
				};
				DrawableLabel label = gridLayout.Label(text);
				gridLayout.Button("delete").OnClick += delegate
				{
					DeleteGS(gs);
				};
				activeToggle[gs] = delegate(bool active)
				{
					if (active)
					{
						label.Text = ">>" + text + "<<";
					}
					else
					{
						label.Text = text;
					}
				};
			}
		}
		Window = drawableWindow;
	}

	private void OnClose()
	{
		Restore();
		backupGS = null;
		Window.Visible = false;
	}

	private void DeleteGS(GeneSet gs)
	{
		session.Modal.MessageBoxYesNo("Are you sure you want to completely remove gene set from pool?").OnResult += delegate(bool result)
		{
			if (result)
			{
				pool.GetGeneration(GeneGeneration.Old).Remove(gs);
				session.Modal.MessageError("Done");
			}
		};
	}

	public void Restore()
	{
		if (backupGS != null)
		{
			ApplyGS(backupGS);
		}
	}

	private void ApplyGS(GeneSet gs)
	{
		if (session.Guest == null || session.Guest.GuestInstance == null)
		{
			return;
		}
		List<GeneInfo> list = new List<GeneInfo>();
		for (int i = 0; i < gs.Vector.Length; i++)
		{
			float num = gs.Vector[i];
			if (!float.IsNaN(num))
			{
				GeneInfo item = new GeneInfo
				{
					Id = new GeneId(pool.Data.GeneOrder[i]),
					Value = num
				};
				list.Add(item);
			}
		}
		session.Guest.GuestInstance.UpdateAll(list);
		session.Guest.SynchronizeCharacterWithInstance();
		foreach (KeyValuePair<GeneSet, Action<bool>> item2 in activeToggle)
		{
			item2.Value(item2.Key == gs);
		}
	}
}
