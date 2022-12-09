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

namespace TownSuite.Prometheus.FileSdConfigs;

public class DestFileSdConfig
{
    public DestFileSdConfig(IEnumerable<string> targets, Settings setting)
    {
        foreach (var url in targets)
        {
            if (setting.IgnoreList != null && setting.IgnoreList.Contains(url))
            {
                continue;
            }

            _targets = targets.ToList();

            Labels = setting.Labels;
        }
    }

    readonly List<string> _targets = new List<string>();

    public static async Task<DestFileSdConfig> Create(IEnumerable<string> retrievedHosts, Settings setting,
        Client client)
    {
        List<string> targets = new List<string>();
        foreach (var url in retrievedHosts)
        {
            if (setting.IgnoreList != null && setting.IgnoreList.Contains(url))
            {
                continue;
            }

            foreach (var path in setting.AppendPaths)
            {
                if (path.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase) ||
                    path.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase))
                {
                    var extraEndpoints = await client.GetJsonListFromContent(setting.AuthHeader, path);
                    foreach (var extraPath in extraEndpoints)
                    {
                        targets.Add(MakeSafeUrl(url, extraPath));
                    }
                }
                else
                {
                    targets.Add(MakeSafeUrl(url, path));
                }
            }
        }

        return new DestFileSdConfig(targets, setting);
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