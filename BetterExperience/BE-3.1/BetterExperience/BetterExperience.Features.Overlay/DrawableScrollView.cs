using UnityEngine;

namespace BetterExperience.Features.Overlay;

public class DrawableScrollView : DrawableContainer<Drawable>, IGuiContainer<Drawable>
{
	private Vector2 scrollPos;

	private float scrolly;

	public bool Autoscroll { get; set; } = true;

	protected override void OnDraw(DrawContext context)
	{
		Rect rect = new Rect(Vector2.zero, base.Children[0].Size);
		float num = rect.height - context.CanvasRect.height;
		if (Autoscroll && scrolly >= 1f)
		{
			scrollPos.y = num * scrolly;
		}
		scrollPos = GUI.BeginScrollView(context.CanvasRect, scrollPos, rect);
		context.VisibleRect = new Rect(scrollPos, base.Size);
		if (num <= 0f)
		{
			scrolly = 1f;
		}
		else
		{
			scrolly = scrollPos.y / num;
		}
		context.CanvasRect = rect;
		base.OnDraw(context);
		GUI.EndScrollView();
	}
}
