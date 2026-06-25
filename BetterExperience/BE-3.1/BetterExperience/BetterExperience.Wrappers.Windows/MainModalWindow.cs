using System.Collections.Generic;
using Assets._ReusableScripts.Memorias.Archivos;
using Assets._ReusableScripts.UI.Modales;
using Assets._ReusableScripts.UI.Modales.Globales;
using Assets.SingletonesAndSystemasGlobales.AbstractLayer.Globales;
using Assets.TValle.IU.Runtime.Drawing.Paneles.Modelos;
using Assets.TValle.IU.Runtime.Modales;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace BetterExperience.Wrappers.Windows;

public class MainModalWindow
{
	public bool Visible
	{
		get
		{
			if (Singleton<ModalWindow>.IsInScene)
			{
				return Singleton<ModalWindow>.instance.isShowing;
			}
			return false;
		}
	}

	public SavingPortraitDialog CurrentSavingPortraitDialog
	{
		get
		{
			if (Singleton<ModalWindow>.IsInScene)
			{
				foreach (GameObject item in Traverse.Create((object)Singleton<ModalWindow>.instance).Field<List<GameObject>>("m_modalesUsando").Value)
				{
					SavingPortraitDialog component = item.GetComponent<SavingPortraitDialog>();
					if (component != null)
					{
						return component;
					}
				}
			}
			return null;
		}
	}

	public MayBeResult<bool> MessageBoxYesNo(string text, params object[] p)
	{
		if (p.Length != 0)
		{
			text = string.Format(text, p);
		}
		MayBeResult<bool> result = new MayBeResult<bool>();
		if (Singleton<ModalWindow>.IsInScene)
		{
			ModalWindow wnd = Singleton<ModalWindow>.instance;
			ConfirmacionMiembros confirmacionMiembros = wnd.MostrarConfirmacion();
			confirmacionMiembros.pregunta.text = text;
			confirmacionMiembros.aceptar.onClick.AddListener(delegate
			{
				result.SetResult(value: true);
			});
			confirmacionMiembros.cancelar.onClick.AddListener(delegate
			{
				result.SetResult(value: false);
			});
			confirmacionMiembros.noMostrarOtraVezToggle.interactable = false;
			result.OnResult += delegate
			{
				wnd.Clear();
			};
		}
		else
		{
			result.SetResult(value: false);
		}
		return result;
	}

	public MayBeResult<object> MessageError(string text)
	{
		MayBeResult<object> result = new MayBeResult<object>();
		if (Singleton<ModalWindow>.IsInScene)
		{
			ModalWindow wnd = Singleton<ModalWindow>.instance;
			ErrorDialog errorDialog = wnd.MostrarErrorDialog();
			errorDialog.pregunta.text = text;
			errorDialog.aceptar.onClick.AddListener(delegate
			{
				result.SetResult(null);
			});
			result.OnResult += delegate
			{
				wnd.Clear();
			};
		}
		else
		{
			result.SetResult(false);
		}
		return result;
	}

	public MayBeResult<string> RequestInput(string text, string def)
	{
		MayBeResult<string> result = new MayBeResult<string>();
		if (Singleton<ModalWindow>.IsInScene)
		{
			ModalWindow wnd = Singleton<ModalWindow>.instance;
			TextInputDialog dialog = wnd.MostrarTextInputDialog();
			dialog.pregunta.text = text;
			dialog.inputField.text = def;
			TMP_InputField.CharacterValidation originalValidation = dialog.inputField.characterValidation;
			dialog.inputField.characterValidation = TMP_InputField.CharacterValidation.None;
			dialog.inputField.characterLimit = 100;
			dialog.aceptar.onClick.AddListener(delegate
			{
				result.SetResult(dialog.inputField.text);
			});
			dialog.cancelar.onClick.AddListener(delegate
			{
				result.SetResult(null);
			});
			result.OnResult += delegate
			{
				dialog.inputField.characterValidation = originalValidation;
				wnd.Clear();
			};
		}
		else
		{
			result.SetResult(null);
		}
		return result;
	}

	public MayBeResult<string> SelectPosePromGallery()
	{
		MayBeResult<string> result = new MayBeResult<string>();
		if (Singleton<ModalWindow>.IsInScene)
		{
			PosePortraitsDialog posePortraitsDialog = Singleton<ModalWindow>.instance.MostrarPosePortraitsDialog();
			((PortraitsModelBase)posePortraitsDialog.panelDePortraits.portraitsModel).staring += delegate(PortraitsModelBase model)
			{
				if (model.protraitsDisponibles.ContieneIndex(model.currentSelected))
				{
					string item = model.protraitsDisponibles[model.currentSelected].item1;
					if (string.IsNullOrWhiteSpace(item))
					{
						result.SetResult(null);
					}
					else
					{
						string data = string.Empty;
						SaveLoadPoses.Cargar(item, out var _, ref data);
						if (data.Length != 0)
						{
							result.SetResult(data);
						}
						else
						{
							result.SetResult(null);
						}
						Singleton<ModalWindow>.instance.Clear();
					}
				}
			};
			((PortraitsModelBase)posePortraitsDialog.panelDePortraits.portraitsModel).canceling += delegate
			{
				Singleton<ModalWindow>.instance.Clear();
				result.SetResult(null);
			};
		}
		else
		{
			result.SetResult(null);
		}
		return result;
	}
}
