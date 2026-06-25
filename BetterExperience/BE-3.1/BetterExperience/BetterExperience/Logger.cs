using System;
using BepInEx.Logging;

namespace BetterExperience;

public class Logger
{
	public static ManualLogSource LoggerImpl { get; set; }

	public static Logger Global { get; } = new Logger();

	public string Prefix { get; set; }

	public bool EnableInfo { get; set; } = true;

	public static Logger Create<T>()
	{
		return new Logger(typeof(T));
	}

	public Logger()
		: this((string)null)
	{
	}

	public Logger(Type type)
		: this("[" + type.Name + "] ")
	{
	}

	public Logger(string prefix)
	{
		Prefix = prefix;
	}

	public void Info(string format, params object[] args)
	{
		if (EnableInfo && LoggerImpl != null)
		{
			string text = AutoFormat(format, args);
			LoggerImpl.LogMessage((object)text);
		}
	}

	public void Error(string format, params object[] args)
	{
		if (LoggerImpl != null)
		{
			string text = AutoFormat(format, args);
			LoggerImpl.LogError((object)text);
		}
	}

	public void Error(Exception ex, string format, params object[] args)
	{
		if (LoggerImpl != null)
		{
			string text = AutoFormat(format, args);
			if (ex != null)
			{
				string text2 = ex.ToString();
				text = text + "\n" + text2;
			}
			LoggerImpl.LogError((object)text);
		}
	}

	public void Debug(string format, params object[] args)
	{
		if (EnableInfo && LoggerImpl != null)
		{
			string text = AutoFormat(format, args);
			LoggerImpl.LogDebug((object)text);
		}
	}

	public void Warn(string format, params object[] args)
	{
		if (LoggerImpl != null)
		{
			string text = AutoFormat(format, args);
			LoggerImpl.LogWarning((object)text);
		}
	}

	private string AutoFormat(string format, object[] args)
	{
		for (int i = 0; i < args.Length; i++)
		{
			object obj = args[i];
			args[i] = ((obj != null) ? obj.ToString() : "null");
		}
		string text = string.Format(format, args);
		if (Prefix != null)
		{
			text = Prefix + text;
		}
		return text;
	}
}
