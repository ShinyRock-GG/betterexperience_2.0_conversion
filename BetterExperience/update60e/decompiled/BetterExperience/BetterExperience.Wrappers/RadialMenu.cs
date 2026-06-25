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
			THSDonaController dona = new THSDonaController();
			Hooks.onShowed(currentUserData, dona);
			currentUserData.radialItemsData = new List<THSDonaController.RadialItemData>();
			currentUserData.radialItemsData.Add(Item);
			currentUserData.radialsSelected.Add(Item.key);
			currentUserData.radialsSelectedSet.Add(Item.key);
			Item.onSelectedStateChanged(currentUserData, isSelected: true, dona, Item);
			try
			{
				if (Multiselect)
				{
					Hooks.onAccepted(currentUserData, dona);
				}
				else
				{
					Item.onClicked(currentUserData, dona, Item);
				}
			}
			catch (NullReferenceException)
			{
			}
			Hooks.onClosed(currentUserData, dona);
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
		CustomMonobehaviour productor = go.GetComponentInChildren<IModeloDeTHSDonaProductor>() as CustomMonobehaviour;
		root = productor.transform;
	}

	public List<RadialMenuEntry> LoadMenu()
	{
		return LoadMenu(root, null);
	}

	internal List<RadialMenuEntry> LoadMenu(Transform context, RadialMenuEntry parent)
	{
		List<RadialMenuEntry> result = new List<RadialMenuEntry>();
		LoaderDeTHSDona dummy = new LoaderDeTHSDona();
		THSDonaController.CurrentUserData model = context.GetComponent<IModeloDeTHSDonaProductor>().ObtenerModelo();
		bool multiselect = model.config.usaAceptarBoton;
		foreach (object obj in context)
		{
			Transform tobj = (Transform)obj;
			IModeloDeTHSDonaProductorDeItemInfo[] items = tobj.GetComponents<IModeloDeTHSDonaProductorDeItemInfo>();
			IModeloDeTHSDonaProductorDeItemInfo[] array = items;
			foreach (IModeloDeTHSDonaProductorDeItemInfo item in array)
			{
				Behaviour beh = item as Behaviour;
				RadialMenuHooks hooks = new RadialMenuHooks();
				foreach (THSDonaController.RadialItemData rid in item.ObtenerModelos(out hooks.onShowed, out hooks.onClosed, out hooks.onAccepted, out hooks.outOnGoBack, dummy))
				{
					RadialMenuEntry entry = new RadialMenuEntry
					{
						Text = rid.text.ToLower(),
						Item = rid,
						Hooks = hooks,
						Parent = parent,
						Multiselect = multiselect
					};
					if (beh.transform.childCount == 1)
					{
						entry.Children = LoadMenu(beh.transform.GetChild(0), entry);
					}
					result.Add(entry);
				}
			}
		}
		return result;
	}
}
