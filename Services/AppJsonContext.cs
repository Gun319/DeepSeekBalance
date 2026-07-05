using System.Text.Json.Serialization;
using DeepSeekBalance.Models;

namespace DeepSeekBalance.Services;

[JsonSerializable(typeof(ConfigService.ConfigData))]
[JsonSerializable(typeof(BalanceResponse))]
[JsonSerializable(typeof(BalanceInfo))]
[JsonSerializable(typeof(PlatformApiService.AmountResponse))]
[JsonSerializable(typeof(PlatformApiService.AmountData))]
[JsonSerializable(typeof(PlatformApiService.AmountBiz))]
[JsonSerializable(typeof(PlatformApiService.DayUsageData))]
[JsonSerializable(typeof(PlatformApiService.ModelUsageData))]
[JsonSerializable(typeof(PlatformApiService.UsageEntry))]
[JsonSerializable(typeof(PlatformApiService.CostResponse))]
[JsonSerializable(typeof(PlatformApiService.CostData))]
[JsonSerializable(typeof(PlatformApiService.CostBiz))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class AppJsonContext : JsonSerializerContext
{
}
