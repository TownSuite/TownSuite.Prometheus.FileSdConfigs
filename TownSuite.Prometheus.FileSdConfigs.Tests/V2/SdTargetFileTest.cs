using System.Text;
using NUnit.Framework;
using TownSuite.Prometheus.FileSdConfigs.V2;

namespace TownSuite.Prometheus.FileSdConfigs.Tests.V2;

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
            AuthHeader = null,
            IgnoreList = null,
            ServiceListUrl = "https://a.test.site.townsuite.com/listservices",
            ServiceDiscoverUrl = "https://a.test.site.townsuite.com/discover?example="
        });

        var returnValues = new DiscoverValues()
        {
            Version = "1",
            Services = new List<ServiceInfo>()
        };
        returnValues.Services.Add(new ServiceInfo()
        {
            Attributes = new Dictionary<string, string>(),
            Id = "123",
            Labels = new Dictionary<string, string>(),
            Name = "test 1"
        });
        returnValues.Services.Add(new ServiceInfo()
        {
            Attributes = new Dictionary<string, string>(),
            Id = "456",
            Labels = new Dictionary<string, string>(),
            Name = "test 2"
        });
        returnValues.Services[0].Attributes.Add("HealthCheck", "/healthz/ready");
        returnValues.Services[0].Attributes.Add("BaseUrl", "https://service1.test.site.townsuite.com");
        returnValues.Services[0].Attributes.Add("DataCenter", "testing");
        returnValues.Services[0].Labels.Add("Env", "test");
        returnValues.Services[0].Labels.Add("Job", "unittest");
        returnValues.Services[1].Attributes.Add("HealthCheck", "/healthz/live");
        returnValues.Services[1].Attributes.Add("BaseUrl", "https://service2.test.site.townsuite.com");
        returnValues.Services[1].Attributes.Add("DataCenter", "testing2");
        returnValues.Services[1].Labels.Add("Env", "test2");
        returnValues.Services[1].Labels.Add("Job", "unittest2");
        var client = new FakeClient(null,
            returnValues: returnValues, listservicesurl: new string[]
            {
                "Service1.Example",
                "Service2.Example"
            },null);

        var sd = new ServiceDiscovery(client, st.ToArray(), new AppSettings(), null);
        await sd.GenerateTargetFile(ms);

        string actualJson = Encoding.UTF8.GetString(ms.ToArray());


        string expectedJson =
            "[{\"targets\":[\"https://service1.test.site.townsuite.com/healthz/ready\",\"https://service2.test.site.townsuite.com/healthz/live\"],\"labels\":{\"Env\":\"test\",\"Job\":\"unittest\"}},{\"targets\":[\"https://service1.test.site.townsuite.com/healthz/ready\",\"https://service2.test.site.townsuite.com/healthz/live\"],\"labels\":{\"Env\":\"test\",\"Job\":\"unittest\"}}]";
        Assert.That(actualJson, Is.EqualTo(expectedJson));
    }


    [Test]
    public async Task SimpleTest_SuffixHealthChecks()
    {
        using var ms = new MemoryStream();
        var st = new List<Settings>();
        var labels = new Dictionary<string, string>();
        labels.Add("job", "test");
        st.Add(new Settings()
        {
            AuthHeader = null,
            IgnoreList = null,
            ServiceListUrl = "https://a.test.site.townsuite.com/listservices",
            ServiceDiscoverUrl = "https://a.test.site.townsuite.com/discover?example="
        });

        var returnValues = new DiscoverValues()
        {
            Version = "1",
            Services = new List<ServiceInfo>()
        };
        returnValues.Services.Add(new ServiceInfo()
        {
            Attributes = new Dictionary<string, string>(),
            Id = "123",
            Labels = new Dictionary<string, string>(),
            Name = "test 1"
        });
        returnValues.Services.Add(new ServiceInfo()
        {
            Attributes = new Dictionary<string, string>(),
            Id = "456",
            Labels = new Dictionary<string, string>(),
            Name = "test 2"
        });
        returnValues.Services[0].Attributes.Add("HealthCheck", "/healthz/ready");
        returnValues.Services[0].Attributes.Add("HealthCheck_example1", "/healthz/example1");
        returnValues.Services[0].Attributes.Add("HealthCheck_example2", "/healthz/example2");
        returnValues.Services[0].Attributes.Add("BaseUrl", "https://service1.test.site.townsuite.com");
        returnValues.Services[0].Attributes.Add("DataCenter", "testing");
        returnValues.Services[0].Labels.Add("Env", "test");
        returnValues.Services[0].Labels.Add("Job", "unittest");
        returnValues.Services[1].Attributes.Add("HealthCheck", "/healthz/live");
        returnValues.Services[1].Attributes.Add("BaseUrl", "https://service2.test.site.townsuite.com");
        returnValues.Services[1].Attributes.Add("DataCenter", "testing2");
        returnValues.Services[1].Labels.Add("Env", "test2");
        returnValues.Services[1].Labels.Add("Job", "unittest2");
        var client = new FakeClient(null,
            returnValues: returnValues, listservicesurl: new string[]
            {
                "Service1.Example",
                "Service2.Example"
            }, null);

        var sd = new ServiceDiscovery(client, st.ToArray(), new AppSettings(), null);
        await sd.GenerateTargetFile(ms);

        string actualJson = Encoding.UTF8.GetString(ms.ToArray());


        string expectedJson =
            "[{\"targets\":[\"https://service1.test.site.townsuite.com/healthz/ready\",\"https://service1.test.site.townsuite.com/healthz/example1\",\"https://service1.test.site.townsuite.com/healthz/example2\",\"https://service2.test.site.townsuite.com/healthz/live\"],\"labels\":{\"Env\":\"test\",\"Job\":\"unittest\"}},{\"targets\":[\"https://service1.test.site.townsuite.com/healthz/ready\",\"https://service1.test.site.townsuite.com/healthz/example1\",\"https://service1.test.site.townsuite.com/healthz/example2\",\"https://service2.test.site.townsuite.com/healthz/live\"],\"labels\":{\"Env\":\"test\",\"Job\":\"unittest\"}}]";
        Assert.That(actualJson, Is.EqualTo(expectedJson));
    }


    [Test]
    public async Task SimpleTest_ExtraHealthChecks_WithPrefix()
    {
        using var ms = new MemoryStream();
        var st = new List<Settings>();
        var labels = new Dictionary<string, string>();
        labels.Add("job", "test");
        st.Add(new Settings()
        {
            AuthHeader = null,
            IgnoreList = null,
            ServiceListUrl = "https://a.test.site.townsuite.com/listservices",
            ServiceDiscoverUrl = "https://a.test.site.townsuite.com/discover?example="
        });

        var returnValues = new DiscoverValues()
        {
            Version = "1",
            Services = new List<ServiceInfo>()
        };
        returnValues.Services.Add(new ServiceInfo()
        {
            Attributes = new Dictionary<string, string>(),
            Id = "123",
            Labels = new Dictionary<string, string>(),
            Name = "test 1"
        });
        returnValues.Services.Add(new ServiceInfo()
        {
            Attributes = new Dictionary<string, string>(),
            Id = "456",
            Labels = new Dictionary<string, string>(),
            Name = "test 2"
        });
        returnValues.Services[0].Attributes.Add("HealthCheck", "/healthz/ready");
        returnValues.Services[0].Attributes.Add("ExtraHealthCheck", "/healthz/extralookups");
        returnValues.Services[0].Attributes.Add("ExtraHealthCheck_Prefix", "/healthz/ready/extras");
        returnValues.Services[0].Attributes.Add("BaseUrl", "https://service1.test.site.townsuite.com");
        returnValues.Services[0].Attributes.Add("DataCenter", "testing");
        returnValues.Services[0].Labels.Add("Env", "test");
        returnValues.Services[0].Labels.Add("Job", "unittest");
        returnValues.Services[1].Attributes.Add("HealthCheck", "/healthz/live");
        returnValues.Services[1].Attributes.Add("BaseUrl", "https://service2.test.site.townsuite.com");
        returnValues.Services[1].Attributes.Add("DataCenter", "testing2");
        returnValues.Services[1].Labels.Add("Env", "test2");
        returnValues.Services[1].Labels.Add("Job", "unittest2");
        var client = new FakeClient(null,
            returnValues: returnValues, listservicesurl: new string[]
            {
                "Service1.Example",
                "Service2.Example"
            },
            extraEndpoints: new []{"hello/world", "world/hello"});

        var sd = new ServiceDiscovery(client, st.ToArray(), new AppSettings(), null);
        await sd.GenerateTargetFile(ms);

        string actualJson = Encoding.UTF8.GetString(ms.ToArray());


        string expectedJson =
            "[{\"targets\":[\"https://service1.test.site.townsuite.com/healthz/ready\",\"https://service1.test.site.townsuite.com/healthz/ready/extras/hello/world\",\"https://service1.test.site.townsuite.com/healthz/ready/extras/world/hello\",\"https://service2.test.site.townsuite.com/healthz/live\"],\"labels\":{\"Env\":\"test\",\"Job\":\"unittest\"}},{\"targets\":[\"https://service1.test.site.townsuite.com/healthz/ready\",\"https://service1.test.site.townsuite.com/healthz/ready/extras/hello/world\",\"https://service1.test.site.townsuite.com/healthz/ready/extras/world/hello\",\"https://service2.test.site.townsuite.com/healthz/live\"],\"labels\":{\"Env\":\"test\",\"Job\":\"unittest\"}}]";
        Assert.That(actualJson, Is.EqualTo(expectedJson));
    }
}