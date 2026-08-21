using System.Text;
using NUnit.Framework;
using TownSuite.Prometheus.FileSdConfigs.V2;

namespace TownSuite.Prometheus.FileSdConfigs.Tests.V2;

/// <summary>
/// A failed lookup means the targets are unaccounted for, not that they are gone.
/// GenerateTargetFile reports that so the previous output file can be kept.
/// </summary>
public class DegradedPassTest
{
    /// <summary>Returns null (what Client does for a non success status) or throws, per url.</summary>
    private class FailingClient : Client
    {
        private readonly string _failOn;
        private readonly bool _throw;
        private readonly DiscoverValues _discovered;
        private readonly string[] _serviceKeys;
        private readonly string[] _extraEndpoints;

        public FailingClient(string failOn, bool @throw, DiscoverValues discovered,
            string[] serviceKeys, string[] extraEndpoints = null) : base(null)
        {
            _failOn = failOn;
            _throw = @throw;
            _discovered = discovered;
            _serviceKeys = serviceKeys;
            _extraEndpoints = extraEndpoints;
        }

        public override Task<T> GetJsonFromContent<T>(string authHeader, string lookupUrl, AppSettings appSettings)
        {
            if (_failOn != null && lookupUrl.Contains(_failOn))
            {
                if (_throw) throw new HttpRequestException($"{lookupUrl} unreachable");
                return Task.FromResult(default(T));
            }

            if (lookupUrl.Contains("listservices")) return Task.FromResult((T)(object)_serviceKeys);
            if (lookupUrl.Contains("extralookups")) return Task.FromResult((T)(object)_extraEndpoints);
            return Task.FromResult((T)(object)_discovered);
        }
    }

    private static Settings[] OneSource() => new[]
    {
        new Settings
        {
            ServiceListUrl = "https://a.test.site.townsuite.com/listservices",
            ServiceDiscoverUrl = "https://a.test.site.townsuite.com/discover?example="
        }
    };

    private static DiscoverValues Discovered(Dictionary<string, string> attributes) => new DiscoverValues
    {
        Version = "1",
        Services = new List<ServiceInfo>
        {
            new ServiceInfo
            {
                Id = "123",
                Name = "test 1",
                Labels = new Dictionary<string, string> { { "Job", "unittest" } },
                Attributes = attributes
            }
        }
    };

    private static Dictionary<string, string> StaticAndExtra => new Dictionary<string, string>
    {
        { "BaseUrl", "https://service1.test.site.townsuite.com" },
        { "HealthCheck", "/healthz/ready" },
        { "ExtraHealthCheck", "/healthz/extralookups" }
    };

    private static async Task<(bool complete, string json)> Generate(Client client)
    {
        var sd = new ServiceDiscovery<DestFileSdConfig>(client, OneSource(), new AppSettings(), null);
        using var ms = new MemoryStream();
        bool complete = await sd.GenerateTargetFile(ms);
        return (complete, Encoding.UTF8.GetString(ms.ToArray()));
    }

    [Test]
    public async Task HealthyPass_ReportsComplete()
    {
        var client = new FailingClient(null, false, Discovered(StaticAndExtra),
            new[] { "Service1.Example" }, new[] { "extra/one" });

        var (complete, json) = await Generate(client);

        Assert.Multiple(() =>
        {
            Assert.That(complete, Is.True);
            Assert.That(json, Does.Contain("/healthz/ready").And.Contain("/extra/one"));
        });
    }

    [TestCase(true, TestName = "ServiceListUrl throws")]
    [TestCase(false, TestName = "ServiceListUrl returns nothing")]
    public async Task ServiceListLookupFails_ReportsIncomplete(bool @throw)
    {
        var client = new FailingClient("listservices", @throw, Discovered(StaticAndExtra),
            new[] { "Service1.Example" });

        if (@throw)
        {
            // An exception already leaves the previous file untouched: it unwinds before
            // the temp file is moved over the destination.
            Assert.ThrowsAsync<HttpRequestException>(async () => await Generate(client));
            return;
        }

        var (complete, json) = await Generate(client);

        Assert.Multiple(() =>
        {
            Assert.That(complete, Is.False, "every target from this source is unaccounted for");
            Assert.That(json, Is.EqualTo("[]"));
        });
    }

    [Test]
    public async Task ServiceDiscoverLookupFails_ReportsIncomplete()
    {
        var client = new FailingClient("discover", false, Discovered(StaticAndExtra),
            new[] { "Service1.Example" });

        var (complete, json) = await Generate(client);

        Assert.Multiple(() =>
        {
            Assert.That(complete, Is.False);
            Assert.That(json, Is.EqualTo("[]"));
        });
    }

    [TestCase(true, TestName = "ExtraHealthCheck throws")]
    [TestCase(false, TestName = "ExtraHealthCheck returns nothing")]
    public async Task ExtraHealthCheckFails_KeepsTheStaticEndpoints(bool @throw)
    {
        var client = new FailingClient("extralookups", @throw, Discovered(StaticAndExtra),
            new[] { "Service1.Example" });

        var (complete, json) = await Generate(client);

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("https://service1.test.site.townsuite.com/healthz/ready"),
                "the endpoints the inventory already told us about must survive");
            Assert.That(complete, Is.True,
                "nothing was lost, so a misconfigured extras endpoint should not pin the file forever");
        });
    }

    [TestCase(true, TestName = "ExtraHealthCheck throws")]
    [TestCase(false, TestName = "ExtraHealthCheck returns nothing")]
    public async Task ExtraHealthCheckFails_WithNothingElseToKeep_ReportsIncomplete(bool @throw)
    {
        var extrasOnly = new Dictionary<string, string>
        {
            { "BaseUrl", "https://service1.test.site.townsuite.com" },
            { "ExtraHealthCheck", "/healthz/extralookups" }
        };
        var client = new FailingClient("extralookups", @throw, Discovered(extrasOnly),
            new[] { "Service1.Example" });

        var (complete, json) = await Generate(client);

        Assert.Multiple(() =>
        {
            Assert.That(complete, Is.False, "the whole instance was lost this pass");
            Assert.That(json, Is.EqualTo("[]"));
        });
    }
}
