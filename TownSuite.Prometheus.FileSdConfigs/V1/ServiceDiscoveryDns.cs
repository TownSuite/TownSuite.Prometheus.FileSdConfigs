using System.Text;
using System.Text.Json;

namespace TownSuite.Prometheus.FileSdConfigs.V1;

public class ServiceDiscoveryDns
{
    private readonly Client _client;
    private readonly Settings[] _settings;
    private readonly AppSettings _appSettings;

    public ServiceDiscoveryDns(Client client, Settings[] settings, AppSettings appSettings)
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
            
            var processedTargets =
                DestFileSdConfig.CreateDnsTargets(retrievedHosts, setting, _client, _appSettings, targets.AsReadOnly());

            if (processedTargets.Targets.Any())
            {
                targets.Add(processedTargets);
            }
        }
        
        string jsonString = JsonSerializer.Serialize(targets);

        await using var sw = new StreamWriter(output);
        await sw.WriteAsync(jsonString);
    }
}