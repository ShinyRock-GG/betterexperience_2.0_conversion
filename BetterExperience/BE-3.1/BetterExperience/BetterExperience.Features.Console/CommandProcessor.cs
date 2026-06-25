using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BetterExperience.GameScopes;
using BetterExperience.Utils;

namespace BetterExperience.Features.Console;

internal class CommandProcessor
{
	private List<Command> commands = new List<Command>();

	internal void Add(Command command, ScopeSupport scope)
	{
		commands.Add(command);
		commands.Sort((Command a, Command b) => a.Prefix[0].CompareTo(b.Prefix[0]));
		if (scope == null)
		{
			return;
		}
		scope.OnDispose += delegate
		{
			commands.Remove(command);
			commands.Sort((Command a, Command b) => a.Prefix[0].CompareTo(b.Prefix[0]));
		};
	}

	internal void Add(Func<string> processor, ScopeSupport scope)
	{
		ConsoleCommandAttribute attribute = processor.Method.GetAttribute<ConsoleCommandAttribute>();
		if (attribute == null)
		{
			new Logger().Info("Method {0} is not annotated", processor.Method);
		}
		Command command = new Command(attribute.Description, attribute.Prefix);
		command.Processor = (Dictionary<string, object> any) => processor();
		Add(command, scope);
	}

	internal void Add<T>(Func<T, string> processor, ScopeSupport scope)
	{
		Type typeFromHandle = typeof(T);
		ConsoleCommandAttribute attribute = typeFromHandle.GetAttribute<ConsoleCommandAttribute>();
		TypedCommand<T> typedCommand = new TypedCommand<T>(processor, attribute.Description, attribute.Prefix);
		PropertyInfo[] properties = typeFromHandle.GetProperties();
		foreach (PropertyInfo propertyInfo in properties)
		{
			ConsoleCommandArg attribute2 = propertyInfo.GetAttribute<ConsoleCommandArg>();
			if (attribute2 != null)
			{
				typedCommand.Param(attribute2.Key, attribute2.Name, propertyInfo.SetValue, propertyInfo.PropertyType, attribute2.Mode);
			}
		}
		Add(typedCommand, scope);
	}

	public CommandProcessor()
	{
		Add(new Command("List commands", "help")
		{
			Args = 
			{
				new CommandArg
				{
					Name = "command",
					Desc = "optional command prefix",
					Type = typeof(string),
					Mode = ConsoleArgMode.Tail
				}
			},
			CustomParser = true,
			Processor = ProcessHelp
		}, null);
	}

	private string ProcessHelp(Dictionary<string, object> arg)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string[] array = null;
		if (arg.ContainsKey("command"))
		{
			string text = arg["command"].ToString().ToLower();
			array = text.Split(new char[1] { ' ' });
			stringBuilder.Append("List of registered commands for prefix ").Append(text).AppendLine(":");
		}
		else
		{
			stringBuilder.AppendLine("List of registered commands:");
		}
		foreach (Command command in commands)
		{
			if (array != null)
			{
				bool flag = false;
				if (array.Length > command.Prefix.Length)
				{
					flag = true;
				}
				else
				{
					for (int i = 0; i < array.Length; i++)
					{
						if (array[i] != command.Prefix[i])
						{
							flag = true;
							break;
						}
					}
				}
				if (flag)
				{
					continue;
				}
			}
			stringBuilder.AppendLine().AppendLine();
			stringBuilder.Append(string.Join(" ", command.Prefix)).Append(" - ").AppendLine(command.Description);
			stringBuilder.Append("Example: ").AppendLine(GetExample(command));
			if (command.Args.Count <= 0)
			{
				continue;
			}
			stringBuilder.AppendLine("Where:");
			foreach (CommandArg arg2 in command.Args)
			{
				stringBuilder.Append("   ").Append(arg2.Name).Append(" - ")
					.Append(arg2.Desc)
					.AppendLine();
			}
		}
		return stringBuilder.ToString();
	}

	public string Run(string cmd)
	{
		string[] array = cmd.Split(new char[1] { ' ' });
		foreach (Command command in commands)
		{
			if (command.Prefix.Length > array.Length)
			{
				continue;
			}
			bool flag = true;
			for (int i = 0; i < command.Prefix.Length; i++)
			{
				if (command.Prefix[i].ToLower() != array[i].ToLower())
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				try
				{
					return ExecCommand(command, array);
				}
				catch (Exception ex)
				{
					new Logger().Error(ex, "Command failed: " + cmd);
					return "OOPS! Exception: " + ex.Message + "\n See log for details";
				}
			}
		}
		return "Unrecognized command. Type help for help";
	}

	private string ExecCommand(Command c, string[] words)
	{
		int num = c.Prefix.Length;
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		Dictionary<string, CommandArg> dictionary2 = c.Args.ToDictionary((CommandArg x) => x.Name.ToLower());
		List<CommandArg> list = c.Args.FindAll((CommandArg x) => x.Mode == ConsoleArgMode.Tail);
		if (list.Count > 1)
		{
			throw new Exception("Too much tails");
		}
		CommandArg commandArg = ((list.Count > 0) ? list[0] : null);
		if (commandArg != null)
		{
			dictionary2.Remove(commandArg.Name.ToLower());
		}
		for (; num < words.Length; num++)
		{
			string text = words[num];
			if (dictionary2.Count == 0)
			{
				break;
			}
			if (!dictionary2.TryGetValue(text.ToLower(), out var value))
			{
				if (commandArg != null)
				{
					break;
				}
				return "Incorrect syntax: unexpected param " + text + "\nExpected syntax: " + GetExample(c);
			}
			switch (value.Mode)
			{
			case ConsoleArgMode.KeyValue:
				if (words.Length <= num + 1)
				{
					return "Incorrect syntax: missing required param <" + text + ">\nExpected syntax: " + GetExample(c);
				}
				try
				{
					object value2 = Convert.ChangeType(words[num + 1], value.Type);
					dictionary[value.Name] = value2;
				}
				catch (Exception ex)
				{
					return "Incorrect syntax: unable to convert param <" + text + ">=<" + words[num + 1] + ":> " + ex.Message + "\nExpected syntax: " + GetExample(c);
				}
				num++;
				break;
			case ConsoleArgMode.Flag:
				dictionary[value.Name] = true;
				break;
			case ConsoleArgMode.Tail:
				throw new Exception();
			}
		}
		if (num < words.Length)
		{
			if (commandArg == null)
			{
				return "Too much args.\nExpected syntax:" + GetExample(c);
			}
			string[] array = new string[words.Length - num];
			Array.Copy(words, num, array, 0, array.Length);
			dictionary[commandArg.Name] = string.Join(" ", array);
		}
		return c.Processor(dictionary);
	}

	private string GetExample(Command c)
	{
		List<string> r = new List<string>();
		r.AddRange(c.Prefix);
		c.Args.ForEach(delegate(CommandArg e)
		{
			if (e.Mode != ConsoleArgMode.Tail)
			{
				r.Add(e.Name);
			}
			r.Add("<" + e.Name + ":" + e.Type.Name + ">");
		});
		return string.Join(" ", r);
	}
}
