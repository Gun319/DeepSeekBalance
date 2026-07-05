using System.Text.Json.Serialization;

namespace DeepSeekBalance.Services;

public interface IConfigService
{
    string? LoadApiKey();
    void SaveApiKey(string apiKey);
    string? LoadUsageToken();
    void SaveUsageToken(string usageToken);
    int LoadRefreshIntervalSeconds();
    void SaveRefreshIntervalSeconds(int seconds);
}

public sealed class ConfigService : IConfigService
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".deepseek-balance");

    private static readonly string ConfigFile = Path.Combine(ConfigDir, "config.json");

    public string? LoadApiKey()
    {
        var config = ReadConfig();
        return config?.ApiKey;
    }

    public void SaveApiKey(string apiKey)
    {
        var config = ReadConfig() ?? new ConfigData();
        config.ApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim();
        WriteConfig(config);
    }

    public string? LoadUsageToken()
    {
        var config = ReadConfig();
        return config?.UsageToken;
    }

    public void SaveUsageToken(string usageToken)
    {
        var config = ReadConfig() ?? new ConfigData();
        config.UsageToken = string.IsNullOrWhiteSpace(usageToken) ? null : usageToken.Trim();
        WriteConfig(config);
    }

    public int LoadRefreshIntervalSeconds()
    {
        var config = ReadConfig();
        return config?.RefreshIntervalSeconds ?? 60;
    }

    public void SaveRefreshIntervalSeconds(int seconds)
    {
        var config = ReadConfig() ?? new ConfigData();
        config.RefreshIntervalSeconds = seconds;
        WriteConfig(config);
    }

    private ConfigData? ReadConfig()
    {
        if (!File.Exists(ConfigFile))
            return null;

        var json = File.ReadAllText(ConfigFile);
        return System.Text.Json.JsonSerializer.Deserialize(json, AppJsonContext.Default.ConfigData);
    }

    private void WriteConfig(ConfigData config)
    {
        Directory.CreateDirectory(ConfigDir);
        var json = System.Text.Json.JsonSerializer.Serialize(config, AppJsonContext.Default.ConfigData);
        File.WriteAllText(ConfigFile, json);
    }

    internal sealed class ConfigData
    {
        [JsonPropertyName("apiKey")]
        public string? ApiKey { get; set; }

        [JsonPropertyName("usageToken")]
        public string? UsageToken { get; set; }

        [JsonPropertyName("refreshIntervalSeconds")]
        public int RefreshIntervalSeconds { get; set; } = 60;
    }
}
