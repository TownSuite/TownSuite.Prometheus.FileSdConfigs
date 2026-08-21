using System.Text;
using NUnit.Framework;
using TownSuite.Prometheus.FileSdConfigs.V2;

namespace TownSuite.Prometheus.FileSdConfigs.Tests.V2;

/// <summary>
/// The OpenTelemetry output is opt in: an instance is a scrape target only when its
/// discovery entry supplies both BaseUrl and OpenTelemetryUrl.
/// </summary>
public class OpenTelemetryTest
{
    private static Settings[] OneSource(string[] ignoreList = null, bool lowercaseLabels = false)
    {
        return new[]
        {
            new Settings
            {
                AuthHeader = null,
                IgnoreList = ignoreList,
                LowercaseLabels = lowercaseLabels,
                ServiceListUrl = "https://a.test.site.townsuite.com/listservices",
                ServiceDiscoverUrl = "https://a.test.site.townsuite.com/discover?example="
            }
        };
    }

    private static DiscoverValues OneInstance(Dictionary<string, string> attributes)
    {
        return new DiscoverValues
        {
            Version = "1",
            Services = new List<ServiceInfo>
            {
                new ServiceInfo
                {
                    Id = "123",
                    Name = "test 1",
                    Attributes = attributes,
                    Labels = new Dictionary<string, string> { { "Env", "test" }, { "Job", "unittest" } }
                }
            }
        };
    }

    private static async Task<string> Generate(Settings[] settings, DiscoverValues discovered)
    {
        var client = new FakeClient(null, discovered, new[] { "Service1.Example" }, null);
        var sd = new ServiceDiscovery<OpenTelemetryDestFileSdConfig>(client, settings, new AppSettings(), null);

        using var ms = new MemoryStream();
        await sd.GenerateTargetFile(ms);
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    [Test]
    public async Task BaseUrlAndOpenTelemetryUrl_BecomeAScrapeTarget()
    {
        string json = await Generate(OneSource(), OneInstance(new Dictionary<string, string>
        {
            { "BaseUrl", "https://service1.test.site.townsuite.com" },
            { "HealthCheck", "/healthz/ready" },
            { "OpenTelemetryUrl", "/otel/metrics" }
        }));

        Assert.That(json, Is.EqualTo(
            "[{\"targets\":[\"https://service1.test.site.townsuite.com/otel/metrics\"]," +
            "\"labels\":{\"Env\":\"test\",\"Job\":\"unittest\"}}]"));
    }

    [Test]
    public async Task NoOpenTelemetryUrl_ProducesAnEmptyButValidFile()
    {
        string json = await Generate(OneSource(), OneInstance(new Dictionary<string, string>
        {
            { "BaseUrl", "https://service1.test.site.townsuite.com" },
            { "HealthCheck", "/healthz/ready" }
        }));

        Assert.That(json, Is.EqualTo("[]"), "prometheus needs valid json even when nothing opted in");
    }

    [Test]
    public async Task EmptyOpenTelemetryUrl_IsIgnored()
    {
        string json = await Generate(OneSource(), OneInstance(new Dictionary<string, string>
        {
            { "BaseUrl", "https://service1.test.site.townsuite.com" },
            { "OpenTelemetryUrl", "" }
        }));

        Assert.That(json, Is.EqualTo("[]"));
    }

    [Test]
    public async Task OpenTelemetryUrlWithoutABaseUrl_IsDropped()
    {
        string json = await Generate(OneSource(), OneInstance(new Dictionary<string, string>
        {
            { "OpenTelemetryUrl", "/otel/metrics" }
        }));

        Assert.That(json, Is.EqualTo("[]"), "a path with nothing to append it to is not a target");
    }

    [Test]
    public async Task AttributeNameIsCaseInsensitive()
    {
        string json = await Generate(OneSource(), OneInstance(new Dictionary<string, string>
        {
            { "baseurl", "https://service1.test.site.townsuite.com" },
            { "opentelemetryurl", "/otel/metrics" }
        }));

        Assert.That(json, Does.Contain("https://service1.test.site.townsuite.com/otel/metrics"));
    }

    [Test]
    public async Task SlashesAreNormalisedWhenJoiningThePath()
    {
        string json = await Generate(OneSource(), OneInstance(new Dictionary<string, string>
        {
            { "BaseUrl", "https://service1.test.site.townsuite.com/" },
            { "OpenTelemetryUrl", "otel/metrics" }
        }));

        Assert.That(json, Does.Contain("https://service1.test.site.townsuite.com/otel/metrics"));
    }

    [Test]
    public async Task IgnoreList_SuppressesTheTarget()
    {
        string json = await Generate(
            OneSource(ignoreList: new[] { "https://service1" }),
            OneInstance(new Dictionary<string, string>
            {
                { "BaseUrl", "https://service1.test.site.townsuite.com" },
                { "OpenTelemetryUrl", "/otel/metrics" }
            }));

        Assert.That(json, Is.EqualTo("[]"));
    }

    [Test]
    public async Task LowercaseLabels_AppliesToThisOutputToo()
    {
        var settings = OneSource(lowercaseLabels: true);
        string json = await Generate(settings, OneInstance(new Dictionary<string, string>
        {
            { "BaseUrl", "https://service1.test.site.townsuite.com" },
            { "OpenTelemetryUrl", "/otel/metrics" }
        }));

        Assert.That(json, Does.Contain("\"labels\":{\"env\":\"test\",\"job\":\"unittest\"}"));
    }
}
