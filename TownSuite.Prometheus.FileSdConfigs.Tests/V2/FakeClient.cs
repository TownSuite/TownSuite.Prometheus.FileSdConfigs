using TownSuite.Prometheus.FileSdConfigs.V2;

namespace TownSuite.Prometheus.FileSdConfigs.Tests.V2;

class FakeClient : Client
{
    private readonly DiscoverValues _returnValues;
    private readonly string[] _listservicesurl;
    private readonly string[] _extraEndpoints;

    public FakeClient(HttpClient httpClient, DiscoverValues returnValues, string[] listservicesurl,
        string[] extraEndpoints) : base(httpClient)
    {
        _returnValues = returnValues;
        _listservicesurl = listservicesurl;
        _extraEndpoints = extraEndpoints;
    }

    public override Task<T> GetJsonFromContent<T>(string authHeader, string lookupUrl, AppSettings appSettings)
    {
        if (lookupUrl.Contains("listservices"))
        {
            return Task.FromResult((T)(object)_listservicesurl);
        }
        else if (lookupUrl.Contains("extralookups"))
        {
            return Task.FromResult((T)(object)_extraEndpoints);
        }

        return Task.FromResult((T)(object)_returnValues);
    }
}