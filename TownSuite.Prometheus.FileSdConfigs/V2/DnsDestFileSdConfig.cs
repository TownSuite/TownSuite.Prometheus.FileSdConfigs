using Microsoft.Extensions.Logging;

namespace TownSuite.Prometheus.FileSdConfigs.V2;

public class DnsDestFileSdConfig : DestFileSdConfig
{
    private string _key;
    
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
    
    protected override void AddLabels(ServiceInfo serviceInfo)
    {
        current.Labels.TryAdd("job", "dns_prober");
        current.Labels.TryAdd("service", _key);
        
        if (setting.LowercaseLabels)
        {
            foreach (var item in serviceInfo.Labels)
            {
                if (string.Equals(item.Key, "job", StringComparison.InvariantCultureIgnoreCase)
                    || string.Equals(item.Key, "service", StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }
                
                current.Labels.TryAdd(item.Key.ToLower().Trim(), item.Value.ToLower().Trim());
            }
        }
        else
        {
            foreach (var l in serviceInfo.Labels)
            {
                if (string.Equals(l.Key, "job", StringComparison.InvariantCultureIgnoreCase)
                    || string.Equals(l.Key, "service", StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                current.Labels.TryAdd(l.Key, l.Value);
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

    public override async Task<DestinationBase[]> Read(string key)
    {
        _key = key;
        current = new DestinationBase();
        
        DiscoverValues json = null;
        json = await client.GetJsonFromContent<DiscoverValues>(setting.AuthHeader,
            $"{setting.ServiceDiscoverUrl}{key}", appSettings);

        if (json?.Services == null)
        {
            logger.LogError($"{setting.ServiceDiscoverUrl}{key} ServiceDiscovery return value is invalid");
            return new DestinationBase[0];
        }

        foreach (var instance in json.Services)
        {
            baseUrl = String.Empty;
            
            AttributeParsing(instance);
            
            if (string.IsNullOrWhiteSpace(baseUrl)) continue;
            
            AddLabels(instance);
            AddTarget(baseUrl);
            results.Add(current);
        }
        return results.ToArray();
    }
}