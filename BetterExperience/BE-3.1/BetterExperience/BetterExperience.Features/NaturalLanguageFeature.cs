using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using BetterExperience.Features.Console;
using BetterExperience.Features.Overlay;
using BetterExperience.Features.PluginOptions;
using BetterExperience.GameScopes;
using BetterExperience.Wrappers;
using UnityEngine;

namespace BetterExperience.Features;

internal class NaturalLanguageFeature : PluginFeature
{
	private class NatureLanguageProcessor : SessionService
	{
		[ConsoleCommand("Say something", new string[] { "say" })]
		public class CommandContext
		{
			[ConsoleCommandArg(Name = "Text to say", Key = "text", Mode = ConsoleArgMode.Tail)]
			public string Text { get; set; }
		}

		private DrawableTextBox textbox;

		private DockingContainer dock;

		private List<Expression<Action>> userExpressions;

		public NatureLanguageProcessor(List<Expression<Action>> userExpressions)
		{
			this.userExpressions = userExpressions;
		}

		public override void OnInit()
		{
			base.OnInit();
			Lookup<ConsoleService>().RegisterCommand<CommandContext>(CommandExecute, base.Scope);
			Plugin.DoUpdate.Add(HandleInput, base.Scope);
			GridLayout gridLayout = new GridLayout();
			gridLayout.Label("Me:");
			textbox = gridLayout.TextBox();
			textbox.OnSumbit += Textbox_OnSumbit;
			textbox.PreferredSize = new Vector2Int(300, 20);
			gridLayout.Button("Say!").OnClick += NaturalLanguageProcessor_OnClick;
			dock = new DockingContainer(Vector2Int.down, gridLayout);
			Lookup<OverlayService>().AddDrawable(dock, base.Scope);
			dock.Visible = false;
		}

		private void Textbox_OnSumbit()
		{
			GUI.FocusControl(null);
			NaturalLanguageProcessor_OnClick();
		}

		private void NaturalLanguageProcessor_OnClick()
		{
			if (dock.Visible)
			{
				if (base.Session.Guest != null)
				{
					Execute(textbox.Text);
				}
				textbox.Text = "";
				dock.Visible = false;
			}
		}

		private void HandleInput()
		{
			if (Input.GetKeyUp(KeyCode.Return) && base.Session.Guest != null && !dock.Visible)
			{
				dock.Visible = true;
				textbox.RequestFocus = true;
			}
		}

		public void Execute(string expr)
		{
			if (expr != null)
			{
				expr = expr.Trim();
				if (!(expr == ""))
				{
					expr = expr.ToLower();
					List<RadialMenu.RadialMenuEntry> menu = base.Session.Guest.RadialMenu.LoadMenu();
					Execute(expr, menu);
				}
			}
		}

		private void Execute(string expr, List<RadialMenu.RadialMenuEntry> menu)
		{
			List<Expression<Action>> list = FlattenMenuTree(menu);
			HashSet<string> token = Tokenize(expr);
			list.AddRange(userExpressions);
			List<Tuple<int, List<Expression<Action>>>> list2 = ScoreExpressions(token, list);
			if (list2.Count == 0)
			{
				base.Session.Guest.HeadController.Say("What?");
				return;
			}
			Tuple<int, List<Expression<Action>>> tuple = list2[0];
			if (tuple.Item2.Count == 1)
			{
				tuple.Item2[0].UserData();
				return;
			}
			string format = "Can you be more specific? {0}?";
			format = string.Format(format, string.Join((tuple.Item2.Count == 2) ? " or " : ",", tuple.Item2.Select((Expression<Action> x) => x.Text).ToArray()));
			base.Session.Guest.HeadController.Say(format);
		}

		public static HashSet<string> Tokenize(string text)
		{
			return new HashSet<string>(Regex.Replace(text, "[^\\w]", " ").Split(new char[1] { ' ' }));
		}

		public string CommandExecute(CommandContext text)
		{
			Execute(text.Text);
			return "";
		}

		public List<Expression<Action>> FlattenMenuTree(List<RadialMenu.RadialMenuEntry> menus)
		{
			List<Expression<Action>> result = new List<Expression<Action>>();
			FlattenMenu(result, menus);
			return result;
		}

		private void FlattenMenu(List<Expression<Action>> result, List<RadialMenu.RadialMenuEntry> menus)
		{
			foreach (RadialMenu.RadialMenuEntry menu in menus)
			{
				if (menu.Children == null)
				{
					result.Add(CreateExpression(menu));
				}
				else
				{
					FlattenMenu(result, menu.Children);
				}
			}
		}

		private Expression<Action> CreateExpression(RadialMenu.RadialMenuEntry e)
		{
			Expression<Action> expression = new Expression<Action>();
			List<string> list = new List<string>();
			for (RadialMenu.RadialMenuEntry radialMenuEntry = e; radialMenuEntry != null; radialMenuEntry = radialMenuEntry.Parent)
			{
				string text = Regex.Replace(radialMenuEntry.Text, "[^\\w]", " ").Trim().ToLower();
				text = string.Join(" ", text.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
				list.Insert(0, text);
			}
			string text2 = string.Join(" ", list);
			text2 = (expression.Text = text2.Substring(0, 1).ToUpper() + text2.Substring(1));
			expression.Tokens = Tokenize(text2);
			expression.UserData = delegate
			{
				e.EmulateClick();
			};
			return expression;
		}

		private List<Tuple<int, List<Expression<T>>>> ScoreExpressions<T>(HashSet<string> token, List<Expression<T>> expressions)
		{
			Dictionary<int, List<Expression<T>>> dictionary = new Dictionary<int, List<Expression<T>>>();
			foreach (Expression<T> expression in expressions)
			{
				int num = token.Intersect(expression.Tokens).Count();
				if (num > 0)
				{
					if (!dictionary.TryGetValue(num, out var value))
					{
						value = (dictionary[num] = new List<Expression<T>>());
					}
					value.Add(expression);
				}
			}
			List<Tuple<int, List<Expression<T>>>> list2 = new List<Tuple<int, List<Expression<T>>>>();
			foreach (KeyValuePair<int, List<Expression<T>>> item in dictionary)
			{
				list2.Add(new Tuple<int, List<Expression<T>>>(item.Key, item.Value));
			}
			list2.Sort((Tuple<int, List<Expression<T>>> a, Tuple<int, List<Expression<T>>> b) => -a.Item1.CompareTo(b.Item1));
			return list2;
		}
	}

	private class Expression<T>
	{
		public string Text { get; set; }

		public HashSet<string> Tokens { get; set; }

		public T UserData { get; set; }
	}

	private List<Expression<Action>> userExpressions = new List<Expression<Action>>();

	private ConfigEntry<bool> featureEnabled;

	public override bool Enabled => true;

	public override void Configure(ConfigFile config)
	{
		base.Configure(config);
		featureEnabled = config.Bind<bool>("Features", "NaturalLanguageFeature", true, "Natural Language Feature: Enable <enter> menu (restart required)");
	}

	public override void OnInit()
	{
		base.OnInit();
		Lookup<PluginOptionsService>().Expose(featureEnabled, base.Scope);
	}

	public override void OnStart()
	{
		base.OnStart();
		if (featureEnabled.Value)
		{
			Lookup<SessionTracker>().SessionServices.Add(() => new NatureLanguageProcessor(userExpressions));
		}
	}

	public void AddExpression(string command, Action action, ScopeSupport scope)
	{
		Expression<Action> expr = new Expression<Action>();
		expr.Text = command;
		expr.Tokens = NatureLanguageProcessor.Tokenize(command);
		expr.UserData = action;
		userExpressions.Add(expr);
		scope.OnDispose += delegate
		{
			userExpressions.Remove(expr);
		};
	}
}
