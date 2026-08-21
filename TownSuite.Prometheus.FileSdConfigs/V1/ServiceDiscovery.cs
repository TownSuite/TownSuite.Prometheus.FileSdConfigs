using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TownSuite.Prometheus.FileSdConfigs.V1;

[Obsolete("Use V2 ServiceDiscovery instead")]
public class ServiceDiscovery
{
    private readonly Client _client;
    private readonly SettingsV1[] _settings;
    private readonly AppSettings _appSettings;
    private readonly ILogger _logger;

    public ServiceDiscovery(Client client, SettingsV1[] settings, AppSettings appSettings, ILogger logger = null)
    {
        _client = client;
        _settings = settings;
        _appSettings = appSettings;
        _logger = logger;
    }

    /// <returns>False when a lookup failed and the target list written is incomplete.</returns>
    public async Task<bool> GenerateTargetFile(Stream output)
    {
        bool complete = true;
        var targets = new List<DestFileSdConfig>();
        foreach (var setting in _settings ?? Array.Empty<SettingsV1>())
        {
            if (setting == null || string.IsNullOrWhiteSpace(setting.LookupUrl))
            {
                _logger?.LogError("A Settings entry has no LookupUrl and was skipped");
                complete = false;
                continue;
            }

            string[] retrievedHosts;
            if (setting.LookupUrl.StartsWith("http"))
            {
                retrievedHosts =
                    await _client.GetJsonFromContent<string[]>(setting.AuthHeader, setting.LookupUrl, _appSettings);
            }
            else
            {
                retrievedHosts = JsonSerializer.Deserialize<string[]>(System.IO.File.ReadAllText(setting.LookupUrl));
            }

            if (retrievedHosts == null)
            {
                // The hosts are unaccounted for, not gone.
                _logger?.LogError($"{setting.LookupUrl} lookup returned nothing");
                complete = false;
                continue;
            }

            var processedTargets = await DestFileSdConfig.Create(retrievedHosts, setting, _client, _appSettings);
            if (processedTargets.Targets.Any())
            {
                targets.Add(processedTargets);
            }
        }
        
        string jsonString = JsonSerializer.Serialize(targets, JsonOptions.GetSerilizer);
        await using var sw = new StreamWriter(output);
        await sw.WriteAsync(jsonString);

        return complete;
    }
}