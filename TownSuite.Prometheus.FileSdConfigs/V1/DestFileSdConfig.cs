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

using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace TownSuite.Prometheus.FileSdConfigs.V1;

public class DestFileSdConfig
{
    public DestFileSdConfig(IEnumerable<string> targets, SettingsV1 setting)
    {
        foreach (var url in targets)
        {
            if (setting.IgnoreList != null && setting.IgnoreList.Contains(url))
            {
                continue;
            }

            _targets.Add(url);
        }

        Labels = setting.Labels;
    }

    readonly List<string> _targets = new List<string>();

    public static async Task<DestFileSdConfig> Create(IEnumerable<string> retrievedHosts, SettingsV1 setting,
        Client client, AppSettings appSettings)
    {
        List<string> targets = new List<string>();
        bool lookupFailed = false;
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
                    var extraEndpoints =
                        await client.GetJsonFromContent<string[]>(setting.AuthHeader, path, appSettings);
                    if (extraEndpoints == null)
                    {
                        // A non-success response comes back as null, and iterating it was a
                        // NullReferenceException that took the whole process down — and with
                        // it every output file froze, not just this one.
                        // The endpoints are unaccounted for, not gone: keep what this host
                        // declared statically and let the caller keep the previous file.
                        lookupFailed = true;
                        continue;
                    }

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

        return new DestFileSdConfig(targets, setting) { LookupFailed = lookupFailed };
    }

    /// <summary>
    /// True when an AppendPaths URL lookup returned nothing, so the target list is
    /// incomplete rather than authoritative.  Never serialised: it is a verdict about
    /// the pass, not a field of the file_sd entry.
    /// </summary>
    [JsonIgnore]
    public bool LookupFailed { get; private init; }
    
    
    [JsonPropertyName("targets")] public string[] Targets => _targets.ToArray();

    [JsonPropertyName("labels")] public Dictionary<string, string> Labels { get; private init; }

    private static string MakeSafeUrl(string protocolAndDomain, string path)
    {
        protocolAndDomain = protocolAndDomain.TrimEnd('/');
        path = path.TrimStart('/');
        return $"{protocolAndDomain}/{path}";
    }
    
}