using System.Net.Http.Json;

namespace TownSuite.Prometheus.FileSdConfigs;

public class Client
{
    private readonly HttpClient _httpClient;
    public Client(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public virtual async Task<T> GetJsonFromContent<T>(string authHeader, string lookupUrl)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, lookupUrl);
        if (!string.IsNullOrWhiteSpace(authHeader))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authHeader);
        }

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<T>();
        }

        return default(T);
    }
}