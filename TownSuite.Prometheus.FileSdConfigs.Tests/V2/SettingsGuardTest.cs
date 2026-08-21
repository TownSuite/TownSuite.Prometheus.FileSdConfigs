using System.Text;
using NUnit.Framework;
using TownSuite.Prometheus.FileSdConfigs.V2;

namespace TownSuite.Prometheus.FileSdConfigs.Tests.V2;

/// <summary>
/// A malformed SettingsV2 entry used to throw a NullReferenceException out of the poll
/// loop, which crashed the process and took every other output with it.  Bad entries are
/// skipped instead, and the file still gets valid json.
/// </summary>
public class SettingsGuardTest
{
    private static async Task<string> Generate(Settings[] settings)
    {
        var discovered = new DiscoverValues
        {
            Version = "1",
            Services = new List<ServiceInfo>
            {
                new ServiceInfo
                {
                    Id = "123",
                    Name = "test 1",
                    Labels = new Dictionary<string, string> { { "Job", "unittest" } },
                    Attributes = new Dictionary<string, string>
                    {
                        { "BaseUrl", "https://service1.test.site.townsuite.com" },
                        { "HealthCheck", "/healthz/ready" }
                    }
                }
            }
        };

        var client = new FakeClient(null, discovered, new[] { "Service1.Example" }, null);
        var sd = new ServiceDiscovery<DestFileSdConfig>(client, settings, new AppSettings(), null);

        using var ms = new MemoryStream();
        await sd.GenerateTargetFile(ms);
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static Settings Valid => new Settings
    {
        ServiceListUrl = "https://a.test.site.townsuite.com/listservices",
        ServiceDiscoverUrl = "https://a.test.site.townsuite.com/discover?example="
    };

    [Test]
    public async Task NoSources_ProducesAnEmptyButValidFile()
    {
        Assert.That(await Generate(Array.Empty<Settings>()), Is.EqualTo("[]"));
    }

    [Test]
    public async Task NullSourceArray_ProducesAnEmptyButValidFile()
    {
        Assert.That(await Generate(null), Is.EqualTo("[]"));
    }

    [Test]
    public async Task NullEntry_IsSkipped()
    {
        Assert.That(await Generate(new[] { null, Valid }), Does.Contain("/healthz/ready"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task EntryWithoutAServiceListUrl_IsSkipped(string serviceListUrl)
    {
        var broken = new Settings
        {
            ServiceListUrl = serviceListUrl,
            ServiceDiscoverUrl = "https://a.test.site.townsuite.com/discover?example="
        };

        Assert.That(await Generate(new[] { broken, Valid }), Does.Contain("/healthz/ready"));
    }
}
