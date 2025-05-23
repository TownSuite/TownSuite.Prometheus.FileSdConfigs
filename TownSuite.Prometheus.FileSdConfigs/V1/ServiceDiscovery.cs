using System.Text;
using System.Text.Json;

namespace TownSuite.Prometheus.FileSdConfigs.V1;

[Obsolete("Use V2 ServiceDiscovery instead")]
public class ServiceDiscovery
{
    private readonly Client _client;
    private readonly Settings[] _settings;
    private readonly AppSettings _appSettings;

    public ServiceDiscovery(Client client, Settings[] settings, AppSettings appSettings)
    {
        _client = client;
        _settings = settings;
        _appSettings = appSettings;
    }

    public async Task GenerateTargetFile(Stream output)
    {
        var targets = new List<DestFileSdConfig>();
        foreach (var setting in _settings)
        {
            if (setting == null) continue;

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

            var processedTargets = await DestFileSdConfig.Create(retrievedHosts, setting, _client, _appSettings);
            if (processedTargets.Targets.Any())
            {
                targets.Add(processedTargets);
            }
        }
        
        string jsonString = JsonSerializer.Serialize(targets, JsonOptions.GetSerilizer);
        await using var sw = new StreamWriter(output);
        await sw.WriteAsync(jsonString);
    }
}