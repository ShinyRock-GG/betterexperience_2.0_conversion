using System;
using BetterExperience.Features.Overlay;
using UnityEngine;

namespace BetterExperience.Features.Console;

internal class ConsoleWindow
{
	private DrawableLabel consoleOutput;

	private DrawableTextBox commandInput;

	private DrawableButton submitBtn;

	private string lastCommand = "";

	public Drawable Drawable { get; private set; }

	public event Action<string> RunCommand = delegate
	{
	};

	public ConsoleWindow()
	{
		Drawable = CreateWindow();
		Drawable.Visible = false;
	}

	private Drawable CreateWindow()
	{
		DrawableWindow drawableWindow = new DrawableWindow(800, 600);
		drawableWindow.Text = "Interactive Console";
		new DrawableScrollView();
		consoleOutput = drawableWindow.Add(new DrawableScrollView
		{
			PreferredSize = new Vector2(drawableWindow.ClientSize.x, drawableWindow.ClientSize.y - 20f)
		}).Add(new DrawableLabel(""));
		commandInput = drawableWindow.Add(new DockingContainer(Vector2Int.down + Vector2Int.left)).Add(new DrawableTextBox());
		commandInput.Position = new Vector2(0f, 0f);
		commandInput.PreferredSize = new Vector2(drawableWindow.ClientSize.x - 50f, 20f);
		commandInput.OnSumbit += OnSubmitCommand;
		commandInput.OnArrowUp += RedoCommand;
		submitBtn = drawableWindow.Add(new DockingContainer(Vector2Int.down + Vector2Int.right)).Add(new DrawableButton());
		submitBtn.PreferredSize = new Vector2(50f, 20f);
		submitBtn.Text = "Run";
		submitBtn.OnClick += OnSubmitCommand;
		return new DockingContainer(Vector2Int.zero, drawableWindow);
	}

	private void OnSubmitCommand()
	{
		if (commandInput.Text != "")
		{
			lastCommand = commandInput.Text;
			this.RunCommand(commandInput.Text);
			commandInput.Text = "";
		}
	}

	public void AddOutput(string text)
	{
		consoleOutput.Text += text;
		consoleOutput.Text += "\n";
	}

	internal void RedoCommand()
	{
		if (commandInput.Text == "")
		{
			commandInput.Text = lastCommand;
		}
	}
}
