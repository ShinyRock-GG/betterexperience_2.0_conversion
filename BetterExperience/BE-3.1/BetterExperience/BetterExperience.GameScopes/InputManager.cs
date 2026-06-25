using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BetterExperience.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace BetterExperience.GameScopes;

public class InputManager
{
	private class KeyHandleStruct
	{
		public List<InputHandle> Simple { get; } = new List<InputHandle>();

		public List<InputHandle> WithModifiers { get; } = new List<InputHandle>();
	}

	private class FocusTracker
	{
		private GameObject lastObject;

		private bool lastResult;

		private FocusController focusCtl;

		public bool IsTextField()
		{
			GameObject currentSelectedGameObject = EventSystem.current.currentSelectedGameObject;
			if (lastObject == currentSelectedGameObject)
			{
				if (focusCtl == null)
				{
					return lastResult;
				}
				return focusCtl.focusedElement is TextField;
			}
			focusCtl = null;
			lastObject = currentSelectedGameObject;
			if (lastObject != null)
			{
				if (lastObject.TryGetComponent<InputField>(out var _))
				{
					return lastResult = true;
				}
				if (lastObject.TryGetComponent<PanelEventHandler>(out var component2))
				{
					focusCtl = component2.panel.focusController;
					return lastResult = focusCtl.focusedElement is TextField;
				}
				return lastResult = false;
			}
			return lastResult = false;
		}
	}

	private Dictionary<KeyCode, KeyHandleStruct> keyboardListeners = new Dictionary<KeyCode, KeyHandleStruct>();

	private FocusTracker focusTracker = new FocusTracker();

	public bool Enabled { get; set; } = true;

	public IInputHandle KeyboardEvent(KeyboardShortcut shortcut, ScopeSupport scope)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		InputHandle handle = new InputHandle();
		handle.InitKeyboard(shortcut);
		OnHandleCreate(handle);
		scope.OnDispose += delegate
		{
			RemoveHandle(handle);
		};
		return handle;
	}

	public IInputHandle KeyboardEvent(ConfigEntry<KeyboardShortcut> shortcut, ScopeSupport scope)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		InputHandle handle = new InputHandle();
		handle.InitKeyboard(shortcut.Value);
		OnHandleCreate(handle);
		EventHandler eh = delegate
		{
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			OnHandleRemove(handle);
			handle.InitKeyboard(shortcut.Value);
			OnHandleCreate(handle);
		};
		shortcut.SettingChanged += eh;
		scope.OnDispose += delegate
		{
			shortcut.SettingChanged -= eh;
			RemoveHandle(handle);
		};
		return handle;
	}

	private void RemoveHandle(InputHandle handle)
	{
		OnHandleRemove(handle);
	}

	private void OnHandleCreate(InputHandle handle)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		if (handle.Keyboard)
		{
			Dictionary<KeyCode, KeyHandleStruct> dic = keyboardListeners;
			KeyboardShortcut shortcut = handle.Shortcut;
			KeyHandleStruct valueOrAdd = dic.GetValueOrAdd(((KeyboardShortcut)(ref shortcut)).MainKey, () => new KeyHandleStruct());
			if (handle.Modifiers.Count == 0)
			{
				valueOrAdd.Simple.Add(handle);
			}
			else
			{
				valueOrAdd.WithModifiers.Add(handle);
			}
		}
	}

	private void OnHandleRemove(InputHandle handle)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		if (!handle.Keyboard)
		{
			return;
		}
		Dictionary<KeyCode, KeyHandleStruct> dictionary = keyboardListeners;
		KeyboardShortcut shortcut = handle.Shortcut;
		if (dictionary.TryGetValue(((KeyboardShortcut)(ref shortcut)).MainKey, out var value))
		{
			if (handle.Modifiers.Count == 0)
			{
				value.Simple.Remove(handle);
			}
			else
			{
				value.WithModifiers.Remove(handle);
			}
		}
	}

	public void OnUpdate()
	{
		if (Enabled)
		{
			ProcessKeyboardInput();
		}
	}

	private void ProcessKeyboardInput()
	{
		if (focusTracker.IsTextField())
		{
			return;
		}
		foreach (KeyValuePair<KeyCode, KeyHandleStruct> keyboardListener in keyboardListeners)
		{
			KeyCode key = keyboardListener.Key;
			bool key2 = Input.GetKey(key);
			bool keyUp = Input.GetKeyUp(key);
			bool keyDown = Input.GetKeyDown(key);
			bool flag = false;
			foreach (InputHandle withModifier in keyboardListener.Value.WithModifiers)
			{
				bool flag2 = true;
				foreach (KeyCode modifier in withModifier.Modifiers)
				{
					if (!Input.GetKey(modifier))
					{
						flag2 = false;
						break;
					}
				}
				if (flag2)
				{
					withModifier.SetFlags(key2, keyUp, keyDown);
					flag = true;
				}
				else
				{
					withModifier.SetFlags(now: false, up: false, down: false);
				}
			}
			foreach (InputHandle item in keyboardListener.Value.Simple)
			{
				if (!flag)
				{
					item.SetFlags(key2, keyUp, keyDown);
				}
				else
				{
					item.SetFlags(now: false, up: false, down: false);
				}
			}
		}
	}
}
