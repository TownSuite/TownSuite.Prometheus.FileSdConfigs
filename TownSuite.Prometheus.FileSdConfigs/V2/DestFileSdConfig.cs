/*
MIT License

Copyright (c) 2022 TownSuite Municipal Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */

using Microsoft.Extensions.Logging;

namespace TownSuite.Prometheus.FileSdConfigs.V2;

public class DestFileSdConfig
{
    protected readonly AppSettings appSettings;
    protected readonly ILogger logger;

    #region Per Service Loop

    // "/health/ready"
    private SortedSet<string> healthCheck = new();

    // "/heathz/extraendpoints" or "https://example1.townsuite.com/lookup'
    private string extraHealthChecksUrlLookup = String.Empty;

    private string extraHealthChecksPrefix = String.Empty;

    // "https://example1.townsuite.com"
    protected string baseUrl = String.Empty;

    #endregion

    protected readonly Settings setting;
    protected readonly Client client;

    protected List<DestinationBase> results = new();
    protected DestinationBase current;

    public DestFileSdConfig(AppSettings appSettings, ILogger logger,
        Settings setting, Client client)
    {
        this.appSettings = appSettings;
        this.logger = logger;
        this.setting = setting;
        this.client = client;
    }

    protected virtual void AttributeParsing(ServiceInfo serviceInfo)
    {
        foreach (var attr in serviceInfo.Attributes)
        {
            if (string.Equals(attr.Key, "HealthCheck", StringComparison.InvariantCultureIgnoreCase))
            {
                healthCheck.Add(attr.Value);
            }
            else if (string.Equals(attr.Key, "ExtraHealthCheck", StringComparison.InvariantCultureIgnoreCase))
            {
                extraHealthChecksUrlLookup = attr.Value;
            }
            else if (string.Equals(attr.Key, "ExtraHealthCheck_Prefix",
                         StringComparison.InvariantCultureIgnoreCase))
            {
                extraHealthChecksPrefix = attr.Value;
            }
            else if (string.Equals(attr.Key, "BaseUrl", StringComparison.InvariantCultureIgnoreCase))
            {
                baseUrl = attr.Value;
            }
            else if (attr.Key.StartsWith("HealthCheck_", StringComparison.InvariantCultureIgnoreCase))
            {
                healthCheck.Add(attr.Value);
            }
        }
    }

    public virtual async Task<DestinationBase[]> Read(string key)
    {
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
            current = new DestinationBase();
            healthCheck.Clear();
            extraHealthChecksUrlLookup = String.Empty;
            extraHealthChecksPrefix = String.Empty;
            baseUrl = String.Empty;

            AttributeParsing(instance);

            if (string.IsNullOrWhiteSpace(baseUrl)) continue;

            if (healthCheck.Any())
            {
                foreach (var endpoint in healthCheck)
                {
                    AddTarget(MakeSafeUrl(baseUrl, endpoint));
                }
            }

            if (!string.IsNullOrWhiteSpace(extraHealthChecksUrlLookup))
            {
                string finalExtraHealthCheckUrl = extraHealthChecksUrlLookup.StartsWith("http")
                    ? extraHealthChecksUrlLookup
                    : MakeSafeUrl(baseUrl, extraHealthChecksUrlLookup);

                try
                {
                    var extraHealthChecks = await client.GetJsonFromContent<string[]>(setting.AuthHeader,
                        finalExtraHealthCheckUrl, appSettings);

                    if (extraHealthChecks != null)
                    {
                        foreach (string endpoint in extraHealthChecks)
                        {
                            if (!string.IsNullOrWhiteSpace(extraHealthChecksPrefix))
                            {
                                string prefix = MakeSafeUrl(baseUrl, extraHealthChecksPrefix);
                                AddTarget(MakeSafeUrl(prefix, endpoint));
                            }
                            else
                            {
                                AddTarget(MakeSafeUrl(baseUrl, endpoint));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"{finalExtraHealthCheckUrl} Extra Health Check return value is invalid");
                    continue;
                }
            }

            AddLabels(instance);
            results.Add(current);
        }

        return results.ToArray();
    }

    protected virtual void AddTarget(string url)
    {
        if (setting.IgnoreList != null
            && (setting.IgnoreList.Contains(url)
                || setting.IgnoreList.Any(ignoredUrl => url.StartsWith(ignoredUrl))
            )
           )
        {
            return;
        }

        if (!current.Targets.Contains(url) && !current.Targets.Any(t => t.Contains(url)))
        {
            current.Targets.Add(url);
        }
    }

    protected virtual void AddLabels(ServiceInfo serviceInfo)
    {
        if (setting.LowercaseLabels)
        {
            foreach (var item in serviceInfo.Labels)
            {
                current.Labels.TryAdd(item.Key.ToLower().Trim(), item.Value.ToLower().Trim());
            }
        }
        else
        {
            foreach (var l in serviceInfo.Labels)
            {
                current.Labels.TryAdd(l.Key, l.Value);
            }
        }
    }

    protected virtual string MakeSafeUrl(string protocolAndDomain, string path)
    {
        protocolAndDomain = protocolAndDomain.TrimEnd('/');
        path = path.TrimStart('/');
        return $"{protocolAndDomain}/{path}";
    }
}