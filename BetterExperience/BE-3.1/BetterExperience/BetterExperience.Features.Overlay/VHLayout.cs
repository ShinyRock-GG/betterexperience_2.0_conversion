using BetterExperience.GameScopes;

namespace BetterExperience.Features.Overlay;

public class VHLayout : VLayout<HLayout<Drawable>>, IGuiContainer<Drawable>
{
	public T Add<T>(T drawable, bool newline = false, bool fit = false, ScopeSupport scope = null) where T : Drawable
	{
		if (base.Children.Count == 0)
		{
			NewLine();
		}
		base.Children[base.Children.Count - 1].Add(drawable, scope);
		if (newline)
		{
			NewLine();
		}
		return drawable;
	}

	K IGuiContainer<Drawable>.Add<K>(K value, ScopeSupport scope)
	{
		return Add(value, newline: false, fit: false, scope);
	}

	public virtual void NewLine()
	{
		base.Add(new HLayout<Drawable>());
	}
}
