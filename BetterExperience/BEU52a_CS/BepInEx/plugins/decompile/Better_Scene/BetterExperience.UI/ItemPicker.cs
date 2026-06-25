using System.Collections.Generic;
using UnityEngine.UIElements;

namespace BetterExperience.UI;

public class ItemPicker : VisualElement
{
	private CheckListView<string> table = new CheckListView<string>((string x) => x);

	public IReadOnlyList<string> Selection => table.Selection;

	public ItemPicker(IEnumerable<string> items)
	{
		ItemPicker itemPicker = this;
		base.style.flexDirection = FlexDirection.Column;
		List<string> model = new List<string>(items);
		model.Sort();
		table.SetElements(model);
		table.SingleSelection = false;
		VisualElement row = UIBuilder.HLayout((VisualElement)this);
		TextField newTextBox = UIBuilder.StyleFlexGrow<TextField>(UIBuilder.TextBox(row, ""), 1f);
		UIBuilder.Button(row, "Insert").clicked += delegate
		{
			string text = newTextBox.text;
			newTextBox.value = "";
			if (text != null)
			{
				text = text.Trim();
				if (text.Length != 0 && !model.Contains(text))
				{
					model.Add(text);
					model.Sort();
					IReadOnlyList<string> selection = itemPicker.table.Selection;
					itemPicker.table.SetElements(model);
					itemPicker.table.SetSelection(selection);
				}
			}
		};
		UIBuilder.StyleFlexGrow<CheckListView<string>>(table, 1f);
		Add(table);
		newTextBox.RegisterValueChangedCallback(OnTextChanged);
	}

	private void OnTextChanged(ChangeEvent<string> evt)
	{
		if (evt.newValue == null || evt.newValue.Length == 0)
		{
			table.Filter(null);
			return;
		}
		table.Filter((string x) => x.Contains(evt.newValue));
	}

	public void SetSelection(IReadOnlyList<string> items)
	{
		table.SetSelection(items);
	}
}
