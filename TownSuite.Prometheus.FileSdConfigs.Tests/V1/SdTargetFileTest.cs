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
        var st = new List<SettingsV1>();
        var labels = new Dictionary<string, string>();
        labels.Add("job", "test");
        st.Add(new SettingsV1()
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
        var st = new List<SettingsV1>();
        var labels = new Dictionary<string, string>();
        labels.Add("job", "test");
        st.Add(new SettingsV1()
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
    public async Task AppendPathLookupReturningNothingKeepsStaticPathsAndMarksIncomplete()
    {
        // A non-success response from an AppendPaths URL came back as null and
        // the foreach over it crashed the process, freezing every output file.
        using var ms = new MemoryStream();
        var st = new List<SettingsV1>();
        st.Add(new SettingsV1()
        {
            AppendPaths = new[]
                { "/healthz/live", "https://a.test.site.townsuite.com/get_appendpaths" },
            AuthHeader = null,
            IgnoreList = null,
            Labels = new Dictionary<string, string> { { "job", "test" } },
            LookupUrl = "https://a.test.site.townsuite.com/get_endpoints"
        });

        var client = new FakeClient(null,
            returnValues: new string[] { "https://example.site1.townsuite.com" },
            extraPathsAppendPaths: null);
        var sd = new ServiceDiscovery(client, st.ToArray(), new AppSettings());
        bool complete = await sd.GenerateTargetFile(ms);

        string actualJson = Encoding.UTF8.GetString(ms.ToArray());
        string expectedJson =
            "[{\"targets\":[\"https://example.site1.townsuite.com/healthz/live\"],\"labels\":{\"job\":\"test\"}}]";
        Assert.That(actualJson, Is.EqualTo(expectedJson));
        Assert.That(complete, Is.False, "a failed lookup must keep the previous file, not replace it");
    }

}
