using Microsoft.Extensions.Logging;

namespace TownSuite.Prometheus.FileSdConfigs.V2;

public class DnsDestFileSdConfig : DestFileSdConfig
{
    public DnsDestFileSdConfig(AppSettings appSettings, ILogger logger, Settings setting, Client client) : base(
        appSettings, logger, setting, client)
    {
    }

    protected override void AttributeParsing(ServiceInfo serviceInfo)
    {
        foreach (var attr in serviceInfo.Attributes)
        {
            if (string.Equals(attr.Key, "BaseUrl", StringComparison.InvariantCultureIgnoreCase))
            {
                baseUrl = attr.Value;
            }
        }
    }

    protected override string MakeSafeUrl(string protocolAndDomain, string path)
    {
        string url = base.MakeSafeUrl(protocolAndDomain, path);
        try
        {
            Uri uri = new Uri(url);
            return $"{uri.Scheme}://{uri.Host}";
        }
        catch (UriFormatException)
        {
            return url;
        }
    }

    public override async Task Read(string key)
    {
        DiscoverValues json = null;
        json = await client.GetJsonFromContent<DiscoverValues>(setting.AuthHeader,
            $"{setting.ServiceDiscoverUrl}{key}", appSettings);

        if (json?.Services == null)
        {
            logger.LogError($"{setting.ServiceDiscoverUrl}{key} ServiceDiscovery return value is invalid");
            return;
        }

        foreach (var instance in json.Services)
        {
            baseUrl = String.Empty;

            AttributeParsing(instance);

            if (string.IsNullOrWhiteSpace(baseUrl)) continue;


            AddTarget(baseUrl);
            Labels.TryAdd("job", "dns_prober");
            Labels.TryAdd("service", key);
        }
    }
}