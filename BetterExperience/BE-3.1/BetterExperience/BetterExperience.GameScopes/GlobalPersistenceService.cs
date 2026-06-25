using System;
using System.IO;
using BepInEx.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BetterExperience.GameScopes;

public class GlobalPersistenceService : PluginFeature, PersistenceService
{
	private ConfigEntry<string> persistenceDir;

	private ConfigEntry<string> exchangeDir;

	public override bool Enabled => true;

	public string Dir => persistenceDir.Value;

	public string ExchangeDir => exchangeDir.Value;

	public override void Configure(ConfigFile config)
	{
		base.Configure(config);
		persistenceDir = config.Bind<string>("Persistence", "PersistenceDir", Path.Combine(".", "Better_Dir"), (ConfigDescription)null);
		exchangeDir = config.Bind<string>("Persistence", "ExchangeDir", Path.Combine(".", "Better_Exchange"), (ConfigDescription)null);
	}

	public override void OnStart()
	{
		base.OnStart();
		new DirectoryInfo(Dir).Create();
		new DirectoryInfo(ExchangeDir).Create();
	}

	public void Persist<T>(T value, string customName, bool exchange)
	{
		string pathForType = GetPathForType(typeof(T), customName, exchange);
		new FileInfo(pathForType).Directory.Create();
		string contents = JsonConvert.SerializeObject(value, new JsonSerializerSettings
		{
			Formatting = Formatting.Indented,
			NullValueHandling = NullValueHandling.Ignore
		});
		File.WriteAllText(pathForType, contents);
	}

	private string GetPathForType(Type value, string customName, bool exchange)
	{
		string path = ((customName == null) ? value.Name : customName) + ".json";
		return Path.Combine(exchange ? exchangeDir.Value : persistenceDir.Value, path);
	}

	public T Persisted<T>(Func<T> factory, string customName, bool exchange)
	{
		string pathForType = GetPathForType(typeof(T), customName, exchange);
		if (File.Exists(pathForType))
		{
			return JsonConvert.DeserializeObject<T>(File.ReadAllText(pathForType));
		}
		logger.Info("Missing file {0}", pathForType);
		return factory();
	}

	public static string Serialize<T>(T value)
	{
		JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
		jsonSerializerSettings.Formatting = Formatting.Indented;
		jsonSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
		jsonSerializerSettings.Converters.Add(new StringEnumConverter());
		return JsonConvert.SerializeObject(value, jsonSerializerSettings);
	}

	public static T Deserialize<T>(string value)
	{
		return JsonConvert.DeserializeObject<T>(value);
	}
}
