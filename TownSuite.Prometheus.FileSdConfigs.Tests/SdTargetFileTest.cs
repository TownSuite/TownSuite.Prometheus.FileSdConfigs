using System.Text;
using NUnit.Framework;

namespace TownSuite.Prometheus.FileSdConfigs.Tests;

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
        var sd = new ServiceDiscovery(client);
        await sd.GenerateTargetFile(st.ToArray(), ms);

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
        var sd = new ServiceDiscovery(client);
        await sd.GenerateTargetFile(st.ToArray(), ms);

        string actualJson = Encoding.UTF8.GetString(ms.ToArray());


        string expectedJson = "[{\"targets\":[\"https://example.site1.townsuite.com/healthz/live\",\"https://example.site1.townsuite.com/healthz/ready\",\"https://example.site1.townsuite.com/hello/world\",\"https://example.site1.townsuite.com/world/hello\",\"https://example.site2.townsuite.com/healthz/live\",\"https://example.site2.townsuite.com/healthz/ready\",\"https://example.site2.townsuite.com/hello/world\",\"https://example.site2.townsuite.com/world/hello\"],\"labels\":{\"job\":\"test\"}}]";
        Assert.That(string.Equals(actualJson, expectedJson), Is.EqualTo(true));
    }
}