using Assets.TValle.BeachGirl.UI.Runtime.Globales;
using Assets.TValle.IU.Runtime.Drawing.Abstracts;
using BetterExperience.GameScopes;
using UnityEngine;

namespace BetterExperience.Wrappers.Windows;

internal class GameOptionsWindow : SessionService
{
	private GenericUserPanelBase componentController;

	public PanelGameOptions OptionsComponent { get; private set; }

	public bool Visible
	{
		get
		{
			if (componentController != null)
			{
				return componentController.isShowing;
			}
			return false;
		}
	}

	public Observable OnVisibilityChanged { get; } = new Observable();

	public override void OnInit()
	{
		GameObject gameObject = GameObject.Find("/MainCanvas");
		if (gameObject == null)
		{
			logger.Error("Canvas not found");
		}
		else
		{
			OptionsComponent = gameObject.GetComponentInChildren<PanelGameOptions>();
		}
		if (OptionsComponent == null)
		{
			logger.Error("Options not found");
			return;
		}
		componentController = OptionsComponent.GetComponentInParent<GenericUserPanelBase>();
		if (componentController == null)
		{
			logger.Error("Component controller not found");
		}
	}

	public override void OnStart()
	{
		if (componentController != null)
		{
			componentController.showed += ToggleVisibility;
			componentController.hided += ToggleVisibility;
		}
	}

	public override void OnStop()
	{
		if (componentController != null)
		{
			componentController.showed -= ToggleVisibility;
			componentController.hided -= ToggleVisibility;
		}
	}

	private void ToggleVisibility(GenericUserPanelBase obj)
	{
		OnVisibilityChanged.Invoke();
	}
}
