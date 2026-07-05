using System.Text.Json.Serialization;

namespace DeepSeekBalance.Models;

public sealed class UsageResponse
{
    [JsonPropertyName("models")]
    public List<ModelSummary> Models { get; set; } = [];

    [JsonPropertyName("days")]
    public List<DailyUsage> Days { get; set; } = [];

    [JsonPropertyName("monthCost")]
    public double MonthCost { get; set; }
}

public sealed class ModelSummary
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("totalTokens")]
    public long TotalTokens { get; set; }

    [JsonPropertyName("requestCount")]
    public long RequestCount { get; set; }

    [JsonPropertyName("cacheHitTokens")]
    public long CacheHitTokens { get; set; }

    [JsonPropertyName("cacheMissTokens")]
    public long CacheMissTokens { get; set; }

    [JsonPropertyName("responseTokens")]
    public long ResponseTokens { get; set; }

    [JsonPropertyName("cost")]
    public double Cost { get; set; }
}

public sealed class DailyUsage
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("flashTokens")]
    public long FlashTokens { get; set; }

    [JsonPropertyName("flashCacheHit")]
    public long FlashCacheHit { get; set; }

    [JsonPropertyName("flashCacheMiss")]
    public long FlashCacheMiss { get; set; }

    [JsonPropertyName("flashResponse")]
    public long FlashResponse { get; set; }

    [JsonPropertyName("proTokens")]
    public long ProTokens { get; set; }

    [JsonPropertyName("proCacheHit")]
    public long ProCacheHit { get; set; }

    [JsonPropertyName("proCacheMiss")]
    public long ProCacheMiss { get; set; }

    [JsonPropertyName("proResponse")]
    public long ProResponse { get; set; }

    [JsonPropertyName("totalTokens")]
    public long TotalTokens { get; set; }

    [JsonPropertyName("totalCost")]
    public double TotalCost { get; set; }
}
