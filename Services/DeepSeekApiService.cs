using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using DeepSeekBalance.Models;

namespace DeepSeekBalance.Services;

public interface IDeepSeekApiService
{
    Task<BalanceResponse?> GetBalanceAsync(string apiKey, CancellationToken ct = default);
}

public sealed class DeepSeekApiService : IDeepSeekApiService
{
    private readonly HttpClient _httpClient;

    public DeepSeekApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<BalanceResponse?> GetBalanceAsync(string apiKey, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/user/balance");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync(AppJsonContext.Default.BalanceResponse, cancellationToken: ct);
    }
}
