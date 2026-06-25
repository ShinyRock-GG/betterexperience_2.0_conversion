using System;
using System.Collections.Generic;
using Assets;
using Assets.TValle.IU.Runtime.Interacciones.THS.Donas;
using UnityEngine;

namespace BetterExperience.Wrappers;

public class RadialMenu
{
	public class RadialMenuEntry
	{
		public string Text { get; set; }

		public THSDonaController.RadialItemData Item { get; set; }

		public List<RadialMenuEntry> Children { get; set; }

		public RadialMenuHooks Hooks { get; set; }

		public RadialMenuEntry Parent { get; set; }

		public bool Multiselect { get; set; }

		public void EmulateClick()
		{
			if (Item.onClicked == null)
			{
				return;
			}
			THSDonaController.CurrentUserData currentUserData = new THSDonaController.CurrentUserData();
			THSDonaController tHSDonaController = new THSDonaController();
			Hooks.onShowed(currentUserData, tHSDonaController);
			currentUserData.radialItemsData = new List<THSDonaController.RadialItemData>();
			currentUserData.radialItemsData.Add(Item);
			currentUserData.radialsSelected.Add(Item.key);
			currentUserData.radialsSelectedSet.Add(Item.key);
			Item.onSelectedStateChanged(currentUserData, isSelected: true, tHSDonaController, Item);
			try
			{
				if (Multiselect)
				{
					Hooks.onAccepted(currentUserData, tHSDonaController);
				}
				else
				{
					Item.onClicked(currentUserData, tHSDonaController, Item);
				}
			}
			catch (NullReferenceException)
			{
			}
			Hooks.onClosed(currentUserData, tHSDonaController);
		}
	}

	public class RadialMenuHooks
	{
		public THSDonaController.OnEventoSimpleHandler onShowed;

		public THSDonaController.OnEventoSimpleHandler onClosed;

		public THSDonaController.OnEventoSimpleHandler onAccepted;

		public THSDonaController.OnEventoSimpleHandler outOnGoBack;
	}

	private Logger logger = new Logger();

	private Transform root;

	public RadialMenu(GameObject go)
	{
		CustomMonobehaviour customMonobehaviour = go.GetComponentInChildren<IModeloDeTHSDonaProductor>() as CustomMonobehaviour;
		root = customMonobehaviour.transform;
	}

	public List<RadialMenuEntry> LoadMenu()
	{
		return LoadMenu(root, null);
	}

	internal List<RadialMenuEntry> LoadMenu(Transform context, RadialMenuEntry parent)
	{
		List<RadialMenuEntry> list = new List<RadialMenuEntry>();
		LoaderDeTHSDona caller = new LoaderDeTHSDona();
		bool usaAceptarBoton = context.GetComponent<IModeloDeTHSDonaProductor>().ObtenerModelo().config.usaAceptarBoton;
		foreach (Transform item in context)
		{
			IModeloDeTHSDonaProductorDeItemInfo[] components = item.GetComponents<IModeloDeTHSDonaProductorDeItemInfo>();
			foreach (IModeloDeTHSDonaProductorDeItemInfo obj in components)
			{
				Behaviour behaviour = obj as Behaviour;
				RadialMenuHooks radialMenuHooks = new RadialMenuHooks();
				foreach (THSDonaController.RadialItemData item2 in obj.ObtenerModelos(out radialMenuHooks.onShowed, out radialMenuHooks.onClosed, out radialMenuHooks.onAccepted, out radialMenuHooks.outOnGoBack, caller))
				{
					RadialMenuEntry radialMenuEntry = new RadialMenuEntry
					{
						Text = item2.text.ToLower(),
						Item = item2,
						Hooks = radialMenuHooks,
						Parent = parent,
						Multiselect = usaAceptarBoton
					};
					if (behaviour.transform.childCount == 1)
					{
						radialMenuEntry.Children = LoadMenu(behaviour.transform.GetChild(0), radialMenuEntry);
					}
					list.Add(radialMenuEntry);
				}
			}
		}
		return list;
	}
}
