using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TownSuite.Prometheus.FileSdConfigs.V2;

public class ServiceDiscovery
{
    private readonly Client _client;
    private readonly Settings[] _settings;
    private readonly AppSettings _appSettings;
    private readonly ILogger _logger;
    
    public ServiceDiscovery(Client client, Settings[] settings, AppSettings appSettings, ILogger logger)
    {
        _client = client;
        _settings = settings;
        _appSettings = appSettings;
        _logger = logger;
    }

    public async Task GenerateTargetFile(Stream output)
    {
        var targets = new List<DestFileSdConfig>();
        foreach (var setting in _settings)
        {
            string[] serviceKeys;
            if (setting.ServiceListUrl.StartsWith("http"))
            {
                serviceKeys =
                    await _client.GetJsonFromContent<string[]>(setting.AuthHeader, setting.ServiceListUrl,
                        _appSettings);
            }
            else
            {
                serviceKeys = JsonSerializer.Deserialize<string[]>(System.IO.File.ReadAllText(setting.ServiceListUrl));
            }

            if (serviceKeys == null)
            {
                _logger.LogError($"{setting.ServiceListUrl} ServiceDiscovery return value is is null");
                continue;
            }
            
            foreach (var fullKey in serviceKeys)
            {
                string key = fullKey.Split(".")[0];
                targets.Add(await DestFileSdConfig.Create(key, setting, _client, _appSettings, _logger));
            }
        }

        string jsonString = JsonSerializer.Serialize(targets);

        await using var sw = new StreamWriter(output);
        await sw.WriteAsync(jsonString);
    }
}