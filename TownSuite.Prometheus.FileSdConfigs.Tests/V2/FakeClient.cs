using TownSuite.Prometheus.FileSdConfigs.V2;

namespace TownSuite.Prometheus.FileSdConfigs.Tests.V2;

class FakeClient : Client
{
    private readonly DiscoverValues _returnValues;
    private readonly string[] _listservicesurl;

    public FakeClient(HttpClient httpClient, DiscoverValues returnValues, string[] listservicesurl) : base(httpClient)
    {
        _returnValues = returnValues;
        _listservicesurl = listservicesurl;
    }

    public override Task<T> GetJsonFromContent<T>(string authHeader, string lookupUrl, AppSettings appSettings)
    {
        if (lookupUrl.Contains("listservices"))
        {
            return Task.FromResult((T)(object)_listservicesurl);
        }

        return Task.FromResult((T)(object)_returnValues);
    }
}