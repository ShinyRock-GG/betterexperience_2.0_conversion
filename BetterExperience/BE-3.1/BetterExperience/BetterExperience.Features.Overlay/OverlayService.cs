using System.Collections.Generic;
using BetterExperience.GameScopes;
using UnityEngine;

namespace BetterExperience.Features.Overlay;

public class OverlayService : PluginService
{
	private List<Drawable> drawables;

	private List<TemporaryNotification> notifications;

	private List<Drawable> mainMenuRenderables;

	private List<Drawable> alwaysOnScreenRenderables;

	public VLayout<Drawable> TopRightPane { get; private set; }

	public VLayout<Drawable> BottomLeftPane { get; private set; }

	public VLayout<Drawable> NotificationArea { get; private set; }

	public OverlayService(List<Drawable> drawables, List<TemporaryNotification> notifications, List<Drawable> mainMenuRenderables, List<Drawable> alwaysOnScreenRenderables)
	{
		this.drawables = drawables;
		this.notifications = notifications;
		TopRightPane = new VLayout<Drawable>();
		drawables.Add(new DockingContainer(Plugin.MonkeyMode ? (Vector2Int.up + Vector2Int.left) : (Vector2Int.up + Vector2Int.right), TopRightPane)
		{
			Position = (Plugin.MonkeyMode ? new Vector2(10f, 150f) : new Vector2(0f, 150f)),
			EnableNative = true
		});
		BottomLeftPane = new VLayout<Drawable>();
		BottomLeftPane.PrependMode = true;
		drawables.Add(new DockingContainer(Vector2Int.down + Vector2Int.left, BottomLeftPane)
		{
			EnableNative = true
		});
		BottomLeftPane.Add(new DrawableLabel("")
		{
			PreferredSize = new Vector2(1f, 200f)
		});
		DockingContainer dockingContainer = new DockingContainer(Vector2Int.up + Vector2Int.left)
		{
			Position = new Vector2(90f, Plugin.MonkeyMode ? 30 : 10),
			EnableNative = true
		};
		NotificationArea = dockingContainer.Add(new VLayout<Drawable>());
		drawables.Add(dockingContainer);
		this.mainMenuRenderables = mainMenuRenderables;
		this.alwaysOnScreenRenderables = alwaysOnScreenRenderables;
	}

	public void AddDrawable(Drawable drawable, ScopeSupport scope = null)
	{
		drawables.Add(drawable);
		if (scope != null)
		{
			scope.OnDispose += delegate
			{
				drawables.Remove(drawable);
			};
		}
	}

	public void AddMenuDrawable(Drawable drawable, ScopeSupport scope = null)
	{
		mainMenuRenderables.Add(drawable);
		if (scope != null)
		{
			scope.OnDispose += delegate
			{
				mainMenuRenderables.Remove(drawable);
			};
		}
	}

	public void AddAlwaysDrawable(Drawable drawable, ScopeSupport scope = null)
	{
		alwaysOnScreenRenderables.Add(drawable);
		if (scope != null)
		{
			scope.OnDispose += delegate
			{
				drawables.Remove(drawable);
			};
		}
	}

	public void Notify(TemporaryNotification notification)
	{
		notifications.Add(notification);
	}

	public void InfoMessage(string text, float duration = 15f, float fadeOut = 3f)
	{
		notifications.Add(new TemporaryNotification
		{
			Drawable = new DrawableLabel(text),
			SpecificContainer = NotificationArea,
			Duration = duration,
			FadeOut = 3f
		});
	}
}
