using Pomufication.Models;

using System.Text.Json;

namespace Pomufication.Services;

public class ConfigService
{
	public PomuConfig Config { get; private set; }

	public ConfigService() { 
		Config = LoadConfig();
	}

	private static PomuConfig LoadConfig()
	{
		if (File.Exists("config.json"))
		{
			var configData = File.ReadAllText("config.json");
			return JsonSerializer.Deserialize<PomuConfig>(configData) ?? new PomuConfig();
		}
		else
		{
			var cfg = new PomuConfig();
			var cfgJson = JsonSerializer.Serialize(cfg);
			File.WriteAllText("config.json", cfgJson);
			return cfg;
		}
	}

	public void SaveConfig()
	{
		var cfgJson = JsonSerializer.Serialize(Config);
		File.WriteAllText("config.json", cfgJson);
	}

	public void SetConfig(PomuConfig config)
	{
		Config = config;
		SaveConfig();
	}
}
