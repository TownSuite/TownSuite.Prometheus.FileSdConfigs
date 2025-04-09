using System.Text;
using NUnit.Framework;
using TownSuite.Prometheus.FileSdConfigs.V1;

namespace TownSuite.Prometheus.FileSdConfigs.Tests.V1;

public class SdTargetFileTest
{
    [Test]
    public async Task SimpleTest()
    {
        using var ms = new MemoryStream();
        var st = new List<Settings>();
        var labels = new Dictionary<string, string>();
        labels.Add("job", "test");
        st.Add(new Settings()
        {
            AppendPaths = new[] { "/healthz/live", "/healthz/ready" },
            AuthHeader = null,
            IgnoreList = null,
            Labels = labels,
            LookupUrl = "https://a.test.site.townsuite.com/get_endpoints"
        });

        var client = new FakeClient(null,
            returnValues: new string[]
            {
                "https://example.site1.townsuite.com",
                "https://example.site2.townsuite.com"
            }, extraPathsAppendPaths: null);
        var sd = new ServiceDiscovery(client, st.ToArray(), new AppSettings());
        await sd.GenerateTargetFile(ms);

        string actualJson = Encoding.UTF8.GetString(ms.ToArray());


        string expectedJson =
            "[{\"targets\":[\"https://example.site1.townsuite.com/healthz/live\",\"https://example.site1.townsuite.com/healthz/ready\",\"https://example.site2.townsuite.com/healthz/live\",\"https://example.site2.townsuite.com/healthz/ready\"],\"labels\":{\"job\":\"test\"}}]";
        Assert.That(actualJson, Is.EqualTo(expectedJson));
    }

    [Test]
    public async Task AppendPathAsUrlToLookupAppendPaths()
    {
        using var ms = new MemoryStream();
        var st = new List<Settings>();
        var labels = new Dictionary<string, string>();
        labels.Add("job", "test");
        st.Add(new Settings()
        {
            AppendPaths = new[]
                { "/healthz/live", "/healthz/ready", "https://a.test.site.townsuite.com/get_appendpaths" },
            AuthHeader = null,
            IgnoreList = null,
            Labels = labels,
            LookupUrl = "https://a.test.site.townsuite.com/get_endpoints"
        });

        var client = new FakeClient(null,
            returnValues: new string[]
            {
                "https://example.site1.townsuite.com",
                "https://example.site2.townsuite.com",
            },
            extraPathsAppendPaths: new[] { "/hello/world", "/world/hello" });
        var sd = new ServiceDiscovery(client, st.ToArray(), new AppSettings());
        await sd.GenerateTargetFile(ms);

        string actualJson = Encoding.UTF8.GetString(ms.ToArray());


        string expectedJson =
            "[{\"targets\":[\"https://example.site1.townsuite.com/healthz/live\",\"https://example.site1.townsuite.com/healthz/ready\",\"https://example.site1.townsuite.com/hello/world\",\"https://example.site1.townsuite.com/world/hello\",\"https://example.site2.townsuite.com/healthz/live\",\"https://example.site2.townsuite.com/healthz/ready\",\"https://example.site2.townsuite.com/hello/world\",\"https://example.site2.townsuite.com/world/hello\"],\"labels\":{\"job\":\"test\"}}]";
        Assert.That(string.Equals(actualJson, expectedJson), Is.EqualTo(true));
    }
    
    [Test]
    public async Task DnsTargetsTest()
    {
        using var ms = new MemoryStream();
        var st = new List<Settings>();
        var labels = new Dictionary<string, string>();
        labels.Add("job", "test");
        st.Add(new Settings()
        {
            AppendPaths = new[] { "/healthz/live", "/healthz/ready" },
            AuthHeader = null,
            IgnoreList = null,
            Labels = labels,
            LookupUrl = "https://a.test.site.townsuite.com/get_endpoints"
        });
        st.Add(new Settings()
        {
            AppendPaths = new[] { "/healthz/live", "/healthz/ready" },
            AuthHeader = null,
            IgnoreList = null,
            Labels = labels,
            LookupUrl = "https://b.test.site.townsuite.com/get_endpoints"
        });
        
        var client = new FakeClient(null,
            returnValues: new string[]
            {
                "https://example.site1.townsuite.com/healthz/live",
                "https://example.site1.townsuite.com/healthz/ready",
                "https://example.site2.townsuite.com/healthz/live",
                "https://example.site2.townsuite.com/healthz/ready",
            }, extraPathsAppendPaths: null);
        var sd = new ServiceDiscoveryDns(client, st.ToArray(), new AppSettings());
        await sd.GenerateTargetFile(ms);

        string actualJson = Encoding.UTF8.GetString(ms.ToArray());


        string expectedJson =
            "[{\"targets\":[\"https://example.site1.townsuite.com\",\"https://example.site2.townsuite.com\"],\"labels\":{\"job\":\"test\"}}]";
        Assert.That(actualJson, Is.EqualTo(expectedJson));
    }

}