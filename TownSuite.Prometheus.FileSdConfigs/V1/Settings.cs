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

namespace TownSuite.Prometheus.FileSdConfigs.V1;

public class Settings
{
    /// <summary>
    /// A service that can be called to lookup all base urls.
    /// As an example this lookup will return a json array of sites such as
    /// https://example.site1.townsuite.com
    /// https://example.site2.townsuite.com
    /// ......
    /// </summary>
    public string LookupUrl { get; init; }

    public string AuthHeader { get; init; }

    /// <summary>
    /// The endpoints that are used with the base service urls.   They are combined
    /// to form a complete list of end points. The main purpose is to find
    /// A list of end points that can be hooked into prometheus service discovery.
    /// As an example, if the append paths are /healthz/live and /healthz/ready
    /// the resuling output of running the ServiceDiscovery will be like this
    /// https://example.site1.townsuite.com/healthz/live
    /// https://example.site2.townsuite.com/healthz/ready
    /// https://example.site1.townsuite.com/healthz/live
    /// https://example.site2.townsuite.com/healthz/ready
    ///
    /// Alternatively an AppendPath can also be another http address
    /// that can be called that returns a json list of end points.
    /// </summary>
    public string[] AppendPaths { get; init; }

    public Dictionary<string, string> Labels { get; init; }
    public string[] IgnoreList { get; init; }
}