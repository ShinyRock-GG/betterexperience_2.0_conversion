using UnityEngine;
using UnityEngine.UIElements;

namespace BetterExperience.Features.Overlay;

public class DockingContainer : DrawableContainer<Drawable>
{
	private Vector2Int alignment;

	public DockingContainer(Vector2Int alignment)
	{
		this.alignment = alignment;
		base.PreferredSize = Vector2.zero;
		base.NativeComponent = new VisualElement();
		base.NativeComponent.pickingMode = PickingMode.Ignore;
		base.NativeComponent.style.position = new StyleEnum<Position>(UnityEngine.UIElements.Position.Absolute);
	}

	public DockingContainer(Vector2Int alignment, Drawable drawable)
		: this(alignment)
	{
		Add(drawable);
	}

	protected override bool BeforeChildDraw(DrawContext context, Drawable child)
	{
		Rect canvasRect = context.CanvasRect;
		Vector2 size = child.Size;
		if (alignment.x == 0)
		{
			canvasRect.x += (canvasRect.width - size.x) / 2f;
		}
		else if (alignment.x > 0)
		{
			canvasRect.x += canvasRect.width - size.x;
		}
		if (alignment.y == 0)
		{
			canvasRect.y += (canvasRect.height - size.y) / 2f;
		}
		else if (alignment.y < 0)
		{
			canvasRect.y += canvasRect.height - size.y;
		}
		canvasRect.size = size;
		context.CanvasRect = canvasRect;
		if (base.NativeComponent != null)
		{
			base.NativeComponent.style.left = new StyleLength(canvasRect.x + child.Position.x);
			base.NativeComponent.style.top = new StyleLength(canvasRect.y + child.Position.y);
		}
		return base.BeforeChildDraw(context, child);
	}
}
