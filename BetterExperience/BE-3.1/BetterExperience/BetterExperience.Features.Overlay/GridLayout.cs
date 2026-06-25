using System;
using UnityEngine;

namespace BetterExperience.Features.Overlay;

public class GridLayout : VHLayout
{
	private float colspacing;

	public GridLayout(float rowspacing = 0f, float colspacing = 0f)
	{
		base.Spacing = rowspacing;
		this.colspacing = colspacing;
	}

	public override void Fit()
	{
		foreach (HLayout<Drawable> child in base.Children)
		{
			if (!child.Transient)
			{
				child.PreferredSizing = null;
			}
		}
		base.Fit();
		float[] array = new float[0];
		foreach (HLayout<Drawable> child2 in base.Children)
		{
			if (!child2.Transient)
			{
				if (child2.ElementSizing.Length > array.Length)
				{
					Array.Resize(ref array, child2.ElementSizing.Length);
				}
				for (int i = 0; i < child2.ElementSizing.Length; i++)
				{
					array[i] = Mathf.Max(array[i], child2.ElementSizing[i]);
				}
			}
		}
		foreach (HLayout<Drawable> child3 in base.Children)
		{
			child3.PreferredSizing = array;
			child3.UpdateNativeComponent();
		}
		base.Fit();
		base.Dirty = false;
	}

	public override void NewLine()
	{
		base.NewLine();
		base.Children[base.Children.Count - 1].Spacing = colspacing;
	}
}
