using BetterExperience.Features.Overlay;
using BetterExperience.GameScopes;

namespace BetterExperience.Features.GeneTool;

internal class GeneWatchFeature : PluginService
{
	private class Installer : SessionService
	{
		private GeneWatchFeature service;

		internal Installer(GeneWatchFeature s)
		{
			service = s;
		}

		public override void OnStart()
		{
			base.OnStart();
			VLayout<Drawable> vLayout = new VLayout<Drawable>();
			vLayout.Add(new DrawableLabel(""));
			vLayout.Add(service.layout);
			vLayout.Add(new DrawableLabel(""));
			Lookup<OverlayService>().TopRightPane.Add(vLayout, base.Scope);
			PersistenceService persistence = Lookup<PersistenceService>();
			service.Data = persistence.Persisted(() => new GeneWatchData());
			service.Restore();
			base.Session.PreSave.Add(delegate
			{
				if (service.Dirty)
				{
					persistence.Persist(service.Data);
					service.Dirty = false;
				}
			});
		}
	}

	private GridLayout layout;

	private GeneWatchData Data { get; set; }

	public bool Dirty { get; private set; }

	public override void OnStart()
	{
		layout = CreateGUI();
		layout.Transient = true;
		Lookup<SessionTracker>().SessionServices.Add(() => new Installer(this));
	}

	public bool IsWatched(string gene)
	{
		return Data.Watched.Contains(gene);
	}

	public void Restore()
	{
		foreach (string item in Data.Watched)
		{
			layout.Label(item);
			layout.Label("?");
			layout.NewLine();
		}
	}

	private GridLayout CreateGUI()
	{
		GridLayout gridLayout = new GridLayout();
		gridLayout.Label("Gene Watch");
		gridLayout.Label("");
		gridLayout.NewLine();
		return gridLayout;
	}

	internal void SetWatchState(string geneId, float value)
	{
		int count = Data.Watched.Count;
		if (float.IsNaN(value))
		{
			int num = Data.Watched.IndexOf(geneId);
			if (num >= 0)
			{
				Data.Watched.RemoveAt(num);
				if (layout.Children.Count > 1 + num)
				{
					HLayout<Drawable> child = layout.Children[1 + num];
					layout.Remove(child);
				}
			}
		}
		else
		{
			string text = $"{value:0.000}";
			if (!Data.Watched.Contains(geneId))
			{
				Data.Watched.Add(geneId);
				layout.Label(geneId);
				layout.Label(text);
				layout.NewLine();
			}
			else
			{
				int num2 = Data.Watched.IndexOf(geneId);
				if (layout.Children.Count > 1 + num2 && layout.Children[1 + num2].Children[1] is DrawableLabel drawableLabel)
				{
					drawableLabel.Text = text;
				}
			}
		}
		Dirty |= Data.Watched.Count != count;
		layout.Transient = Data.Watched.Count == 0;
	}
}
