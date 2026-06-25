using System;
using Assets._ReusableScripts.CuchiCuchi.AI;
using BepInEx.Configuration;
using BetterExperience.Features.Overlay;
using BetterExperience.Features.PluginOptions;
using BetterExperience.GameScopes;

namespace BetterExperience.Features;

internal class EmoSpy : PluginFeature
{
	private class GuestEmotionView : SessionService
	{
		private event Action Update = delegate
		{
		};

		public override void OnStart()
		{
			Lookup<OverlayService>().TopRightPane.Add(CreateGUI(), base.Scope);
		}

		private GridLayout CreateGUI()
		{
			EmocionesFemeninas componentInChildren = base.Session.Guest.Impl.GetComponentInChildren<EmocionesFemeninas>();
			GridLayout gridLayout = new GridLayout();
			GridLayout gridLayout2 = new GridLayout();
			gridLayout.Add(gridLayout2);
			gridLayout.NewLine();
			gridLayout2.Label("Emotions");
			gridLayout2.NewLine();
			int num = 0;
			foreach (Emocion e in componentInChildren.emociones)
			{
				string name = e.GetType().Name;
				gridLayout2.Label(name);
				DrawableLabel value = gridLayout2.Label(e.value.total.ToString());
				Action<Emocion> cb = delegate
				{
					value.Text = e.value.total.ToString();
					this.Update();
				};
				e.afterUpdate += cb;
				base.Scope.OnDispose += delegate
				{
					e.afterUpdate -= cb;
				};
				if (++num % 2 == 0)
				{
					gridLayout2.NewLine();
				}
			}
			gridLayout2 = new GridLayout();
			gridLayout.Add(gridLayout2);
			gridLayout.NewLine();
			return gridLayout;
		}
	}

	private ConfigEntry<bool> enableFeature;

	public override bool Enabled => enableFeature.Value;

	public override void Configure(ConfigFile config)
	{
		enableFeature = config.Bind<bool>("Features", "EnableEmoSpy", false, "Enable EmoSpy: Display numeric emotion values");
	}

	public override void OnInit()
	{
		base.OnInit();
		Lookup<PluginOptionsService>().Expose(enableFeature, base.Scope);
	}

	public override void OnStart()
	{
		Lookup<SessionTracker>().InterviewServices.Add(() => new GuestEmotionView());
	}
}
