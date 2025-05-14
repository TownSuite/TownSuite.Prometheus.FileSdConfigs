using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TownSuite.Prometheus.FileSdConfigs.V2;

public class ServiceDiscovery<T> where T : DestFileSdConfig
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

    protected virtual List<DestinationBase> targets { get; set; } = new ();

    public async Task GenerateTargetFile(Stream output)
    {
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

                DestFileSdConfig destImplimentation;
                if (this.GetType() ==
                    typeof(TownSuite.Prometheus.FileSdConfigs.V2.ServiceDiscovery<
                        TownSuite.Prometheus.FileSdConfigs.V2.PrometheusMetricsDestFileSdConfig>))
                {
                    destImplimentation = new PrometheusMetricsDestFileSdConfig(_appSettings, _logger, setting, _client);
                }
                else  if (this.GetType() ==
                          typeof(TownSuite.Prometheus.FileSdConfigs.V2.ServiceDiscovery<
                              TownSuite.Prometheus.FileSdConfigs.V2.OpenTelemetryDestFileSdConfig>))
                {
                    destImplimentation = new OpenTelemetryDestFileSdConfig(_appSettings, _logger, setting, _client);
                }
                else  if (this.GetType() ==
                          typeof(TownSuite.Prometheus.FileSdConfigs.V2.ServiceDiscovery<
                              TownSuite.Prometheus.FileSdConfigs.V2.DnsDestFileSdConfig>))
                {
                    destImplimentation = new DnsDestFileSdConfig(_appSettings, _logger, setting, _client);
                }
                else
                {
                    destImplimentation = new DestFileSdConfig(_appSettings, _logger, setting, _client);
                }

                
                var results = await destImplimentation.Read(key);
                foreach (var target in results)
                {
                    if (target != null && target.Targets.Any())
                    {
                        if (!targets.Contains(target))
                        {
                            targets.Add(target);
                        }
                    }
                }
            }
        }

        string jsonString = JsonSerializer.Serialize(targets, JsonOptions.GetSerilizer);

        await using var sw = new StreamWriter(output);
        await sw.WriteAsync(jsonString);
    }
}