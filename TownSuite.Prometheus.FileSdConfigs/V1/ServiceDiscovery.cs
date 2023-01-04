using System.Text;
using System.Text.Json;

namespace TownSuite.Prometheus.FileSdConfigs.V1;

public class ServiceDiscovery
{
    private Client _client;
    private readonly Settings[] _settings;

    public ServiceDiscovery(Client client, Settings[] settings)
    {
        _client = client;
        _settings = settings;
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
                retrievedHosts = await _client.GetJsonFromContent<string[]>(setting.AuthHeader, setting.LookupUrl);
            }
            else
            {
                retrievedHosts = JsonSerializer.Deserialize<string[]>(System.IO.File.ReadAllText(setting.LookupUrl));
            }

            targets.Add(await DestFileSdConfig.Create(retrievedHosts, setting, _client));
        }


        string jsonString = JsonSerializer.Serialize(targets);

        await using var sw = new StreamWriter(output);
        await sw.WriteAsync(jsonString);
    }
}