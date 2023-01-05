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

using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace TownSuite.Prometheus.FileSdConfigs.V2;

public class DestFileSdConfig
{
    public DestFileSdConfig(IEnumerable<string> targets, Settings setting, Dictionary<string, string> labels)
    {
        foreach (var url in targets)
        {
            if (setting.IgnoreList != null && setting.IgnoreList.Contains(url))
            {
                continue;
            }

            _targets.Add(url);
        }

        if (setting.LowercaseLabels)
        {
            Labels = new Dictionary<string, string>();
            foreach (var item in labels)
            {
                Labels.Add(item.Key.ToLower().Trim(), item.Value.ToLower().Trim());
            }
        }
        else
        {
            Labels = labels;
        }
    }

    readonly List<string> _targets = new List<string>();

    public static async Task<DestFileSdConfig> Create(string key, Settings setting,
        Client client, AppSettings appSettings, ILogger logger)
    {
        List<string> targets = new List<string>();
        Dictionary<string, string> labels = new Dictionary<string, string>();
        var json = await client.GetJsonFromContent<DiscoverValues>(setting.AuthHeader,
            $"{setting.ServiceDiscoverUrl}{key}", appSettings);

        foreach (var instance in json.Services)
        {
            // "/health/ready"
            List<string> healthCheck =  new List<string>();
            // "/heathz/extraendpoints" or "https://example1.townsuite.com/lookup'
            string extraHealthChecksUrlLookup = String.Empty;
            // "https://example1.townsuite.com"
            string baseUrl = String.Empty;

            foreach (var attr in instance.Attributes)
            {
                if (string.Equals(attr.Key, "HealthCheck", StringComparison.InvariantCultureIgnoreCase))
                {
                    healthCheck.Add(attr.Value);
                }
                else if (string.Equals(attr.Key, "ExtraHealthCheck", StringComparison.InvariantCultureIgnoreCase))
                {
                    extraHealthChecksUrlLookup = attr.Value;
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

            if (string.IsNullOrWhiteSpace(baseUrl)) continue;

            if (healthCheck.Any())
            {
                foreach (var endpoint in healthCheck)
                {
                    targets.Add(MakeSafeUrl(baseUrl, endpoint));
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

                    foreach (string endpoint in extraHealthChecks)
                    {
                        targets.Add(MakeSafeUrl(baseUrl, endpoint));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"{finalExtraHealthCheckUrl} Extra Health Check return value is invalid");
                    continue;
                }

            }

            foreach (var l in instance.Labels)
            {
                labels.TryAdd(l.Key, l.Value);
            }
        }


        return new DestFileSdConfig(targets, setting, labels);
    }

    [JsonPropertyName("targets")] public string[] Targets => _targets.ToArray();

    [JsonPropertyName("labels")] public Dictionary<string, string> Labels { get; private init; }

    private static string MakeSafeUrl(string protocolAndDomain, string path)
    {
        protocolAndDomain = protocolAndDomain.TrimEnd('/');
        path = path.TrimStart('/');
        return $"{protocolAndDomain}/{path}";
    }
}