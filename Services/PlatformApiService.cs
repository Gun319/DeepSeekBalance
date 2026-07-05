using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace DeepSeekBalance.Services;

public interface IPlatformApiService
{
    Task<(List<Models.ModelSummary> Models, List<Models.DailyUsage> Days, double MonthCost)> GetUsageAsync(
        string usageToken, int month, int year, CancellationToken ct = default);
}

public sealed class PlatformApiService : IPlatformApiService
{
    private readonly HttpClient _httpClient;

    public PlatformApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(List<Models.ModelSummary> Models, List<Models.DailyUsage> Days, double MonthCost)> GetUsageAsync(
        string usageToken, int month, int year, CancellationToken ct = default)
    {
        var amountUrl = $"https://platform.deepseek.com/api/v0/usage/amount?month={month}&year={year}";
        var costUrl = $"https://platform.deepseek.com/api/v0/usage/cost?month={month}&year={year}";

        var amountTask = GetJsonAsync(amountUrl, usageToken, AppJsonContext.Default.AmountResponse, ct);
        var costTask = GetJsonAsync(costUrl, usageToken, AppJsonContext.Default.CostResponse, ct);

        await Task.WhenAll(amountTask, costTask);

        var amount = amountTask.Result;
        var cost = costTask.Result;

        var costTotal = cost.Data.BizData.FirstOrDefault();
        double CostForModel(string model) =>
            costTotal?.Total.Find(m => m.Model == model)?.Usage.SumCost() ?? 0.0;

        var models = new List<Models.ModelSummary>();
        foreach (var mu in amount.Data.BizData.Total)
        {
            var label = mu.Model switch
            {
                "deepseek-v4-flash" => ("flash", "V4 Flash"),
                "deepseek-v4-pro" => ("pro", "V4 Pro"),
                _ => default
            };
            if (label == default) continue;

            var (total, requests, hit, miss, response) = mu.Usage.TokenBreakdown();
            models.Add(new Models.ModelSummary
            {
                Key = label.Item1,
                Name = label.Item2,
                TotalTokens = total,
                RequestCount = requests,
                CacheHitTokens = hit,
                CacheMissTokens = miss,
                ResponseTokens = response,
                Cost = CostForModel(mu.Model)
            });
        }

        var costByDate = new Dictionary<string, double>();
        if (costTotal != null)
        {
            foreach (var day in costTotal.Days)
            {
                costByDate[day.Date] = day.Data.Sum(m => m.Usage.SumCost());
            }
        }

        var days = new List<Models.DailyUsage>();
        foreach (var day in amount.Data.BizData.Days)
        {
            long flash = 0, flashHit = 0, flashMiss = 0, flashResp = 0;
            long pro = 0, proHit = 0, proMiss = 0, proResp = 0;
            long total = 0;

            foreach (var mu in day.Data)
            {
                var (tokens, _, hit, miss, response) = mu.Usage.TokenBreakdown();
                total += tokens;
                switch (mu.Model)
                {
                    case "deepseek-v4-flash":
                        flash += tokens; flashHit += hit; flashMiss += miss; flashResp += response; break;
                    case "deepseek-v4-pro":
                        pro += tokens; proHit += hit; proMiss += miss; proResp += response; break;
                }
            }

            days.Add(new Models.DailyUsage
            {
                Date = day.Date,
                FlashTokens = flash, FlashCacheHit = flashHit, FlashCacheMiss = flashMiss, FlashResponse = flashResp,
                ProTokens = pro, ProCacheHit = proHit, ProCacheMiss = proMiss, ProResponse = proResp,
                TotalTokens = total,
                TotalCost = costByDate.GetValueOrDefault(day.Date, 0.0)
            });
        }

        var monthCost = costTotal?.Total.Sum(m => m.Usage.SumCost()) ?? 0.0;
        return (models, days, monthCost);
    }

    private async Task<T> GetJsonAsync<T>(string url, string token, JsonTypeInfo<T> typeInfo, CancellationToken ct) where T : notnull
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Headers.Add("x-app-version", "1.0.0");
        request.Headers.Add("Accept", "*/*");
        request.Headers.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36");

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync(typeInfo, cancellationToken: ct))!;
    }

    // Internal DTOs for deserializing the platform API response

    internal sealed class UsageEntry
    {
        [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
        [JsonPropertyName("amount")] public string Amount { get; set; } = string.Empty;
    }

    internal sealed class ModelUsageData
    {
        [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
        [JsonPropertyName("usage")] public List<UsageEntry> Usage { get; set; } = [];
    }

    internal sealed class DayUsageData
    {
        [JsonPropertyName("date")] public string Date { get; set; } = string.Empty;
        [JsonPropertyName("data")] public List<ModelUsageData> Data { get; set; } = [];
    }

    internal sealed class AmountBiz
    {
        [JsonPropertyName("total")] public List<ModelUsageData> Total { get; set; } = [];
        [JsonPropertyName("days")] public List<DayUsageData> Days { get; set; } = [];
    }

    internal sealed class AmountData
    {
        [JsonPropertyName("biz_data")] public AmountBiz BizData { get; set; } = new();
    }

    internal sealed class AmountResponse
    {
        [JsonPropertyName("data")] public AmountData Data { get; set; } = new();
    }

    internal sealed class CostBiz
    {
        [JsonPropertyName("total")] public List<ModelUsageData> Total { get; set; } = [];
        [JsonPropertyName("days")] public List<DayUsageData> Days { get; set; } = [];
    }

    internal sealed class CostData
    {
        [JsonPropertyName("biz_data")] public List<CostBiz> BizData { get; set; } = [];
    }

    internal sealed class CostResponse
    {
        [JsonPropertyName("data")] public CostData Data { get; set; } = new();
    }
}

internal static class UsageEntryExtensions
{
    public static (long total, long request, long hit, long miss, long response) TokenBreakdown(this List<PlatformApiService.UsageEntry> usage)
    {
        long total = 0, request = 0, hit = 0, miss = 0, response = 0;
        foreach (var entry in usage)
        {
            var value = (long)Math.Round(double.Parse(entry.Amount));
            switch (entry.Type)
            {
                case "REQUEST": request = value; break;
                case "PROMPT_CACHE_HIT_TOKEN": hit = value; total += value; break;
                case "PROMPT_CACHE_MISS_TOKEN": miss = value; total += value; break;
                case "RESPONSE_TOKEN": response = value; total += value; break;
                case "PROMPT_TOKEN": total += value; break;
            }
        }
        return (total, request, hit, miss, response);
    }

    public static double SumCost(this List<PlatformApiService.UsageEntry> usage)
    {
        return usage
            .Where(e => e.Type != "REQUEST")
            .Sum(e => double.Parse(e.Amount));
    }
}
