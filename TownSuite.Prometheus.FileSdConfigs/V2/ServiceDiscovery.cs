using System.Text;
using System.Text.Json;

namespace TownSuite.Prometheus.FileSdConfigs.V2;

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
            string[] serviceKeys;
            if (setting.ServiceListUrl.StartsWith("http"))
            {
                serviceKeys = await _client.GetJsonFromContent<string[]>(setting.AuthHeader, setting.ServiceListUrl);
            }
            else
            {
                serviceKeys = JsonSerializer.Deserialize<string[]>(System.IO.File.ReadAllText(setting.ServiceListUrl));
            }
            
            foreach (var fullKey in serviceKeys)
            {
                string key = fullKey.Split(".")[0];
                targets.Add(await DestFileSdConfig.Create(key, setting, _client));
            }
        }

        string jsonString = JsonSerializer.Serialize(targets);

        await using var sw = new StreamWriter(output);
        await sw.WriteAsync(jsonString);
    }
}