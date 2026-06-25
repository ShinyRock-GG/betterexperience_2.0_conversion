using BetterExperience.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace BetterExperience.Features.Overlay;

public class UIToolkitInitializer
{
	private UIDocument uiDocument;

	private bool visibility = true;

	public UIDocument InitializeUIToolkit()
	{
		AssetBundle assetBundle = AssetBundle.LoadFromMemory(BetterExperience.Properties.Resources.uitoolkit);
		GameObject gameObject = Object.Instantiate(assetBundle.LoadAsset<GameObject>("UIElements"));
		gameObject.hideFlags |= HideFlags.HideAndDontSave;
		Object.DontDestroyOnLoad(gameObject);
		UIDocument component = gameObject.GetComponent<UIDocument>();
		component.panelSettings.sortingOrder = -100f;
		component.rootVisualElement.pickingMode = PickingMode.Ignore;
		assetBundle.Unload(unloadAllLoadedObjects: false);
		uiDocument = component;
		return component;
	}

	public void FixBattlehub()
	{
		PanelRaycaster[] array = Object.FindObjectsOfType<PanelRaycaster>();
		for (int i = 0; i < array.Length; i++)
		{
			Canvas canvas = array[i].gameObject.AddComponent<Canvas>();
			if (canvas != null)
			{
				canvas.enabled = false;
			}
		}
	}

	internal void SetVisible(bool value)
	{
		if (visibility != value)
		{
			visibility = value;
			if (value)
			{
				uiDocument.rootVisualElement.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
			}
			else
			{
				uiDocument.rootVisualElement.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
			}
		}
	}

	internal void Dispose()
	{
		if (uiDocument != null)
		{
			Object.Destroy(uiDocument.gameObject);
		}
	}
}
