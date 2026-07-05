using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeepSeekBalance.Models;

public sealed class BalanceResponse
{
    [JsonPropertyName("is_available")]
    public bool IsAvailable { get; set; }

    [JsonPropertyName("balance_infos")]
    public List<BalanceInfo> BalanceInfos { get; set; } = [];
}

public sealed class BalanceInfo
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("total_balance")]
    public string TotalBalance { get; set; } = string.Empty;

    [JsonPropertyName("granted_balance")]
    public string GrantedBalance { get; set; } = string.Empty;

    [JsonPropertyName("topped_up_balance")]
    public string ToppedUpBalance { get; set; } = string.Empty;
}
