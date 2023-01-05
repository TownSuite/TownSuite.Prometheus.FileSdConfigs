namespace TownSuite.Prometheus.FileSdConfigs.Tests.V1;

class FakeClient : Client
{
    private readonly string[] _returnValues;
    private readonly string[] _extraPathsAppendPaths;

    public FakeClient(HttpClient httpClient, string[] returnValues, string [] extraPathsAppendPaths) : base(httpClient)
    {
        _returnValues = returnValues;
        _extraPathsAppendPaths = extraPathsAppendPaths;
    }

    public override Task<T> GetJsonFromContent<T>(string authHeader, string lookupUrl, AppSettings appSettings)
    {
        if (lookupUrl.Contains("get_appendpaths"))
        {
            return Task.FromResult( (T)(object)_extraPathsAppendPaths);
        }
        
        return Task.FromResult((T)(object)_returnValues);
    }
}