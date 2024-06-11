using Microsoft.Extensions.Logging;

namespace TownSuite.Prometheus.FileSdConfigs.V2;

public class PrometheusMetricsDestFileSdConfig : DestFileSdConfig
{
    public PrometheusMetricsDestFileSdConfig(AppSettings appSettings, ILogger logger, Settings setting, Client client) : base(
        appSettings, logger, setting, client)
    {
    }

    protected override void AttributeParsing(ServiceInfo serviceInfo)
    {
        string metricsUrl = string.Empty;
        foreach (var attr in serviceInfo.Attributes)
        {
            if (string.Equals(attr.Key, "PrometheusMetricsUrl", StringComparison.InvariantCultureIgnoreCase))
            {
                metricsUrl = attr.Value;
            }
            else if (string.Equals(attr.Key, "BaseUrl", StringComparison.InvariantCultureIgnoreCase))
            {
                baseUrl = attr.Value;
            }
        }

        if (!string.IsNullOrEmpty(metricsUrl))
        {
            AddTarget(MakeSafeUrl(baseUrl, metricsUrl));
        }
    }
}