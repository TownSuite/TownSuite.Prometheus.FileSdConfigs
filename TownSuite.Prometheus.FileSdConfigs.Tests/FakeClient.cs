namespace TownSuite.Prometheus.FileSdConfigs.Tests;

class FakeClient : Client
{
    private readonly string[] _returnValues;
    private readonly string[] _extraPathsAppendPaths;

    public FakeClient(HttpClient httpClient, string[] returnValues, string [] extraPathsAppendPaths) : base(httpClient)
    {
        _returnValues = returnValues;
        _extraPathsAppendPaths = extraPathsAppendPaths;
    }

    public override Task<string[]> GetJsonListFromContent(string authHeader, string lookupUrl)
    {

        if (lookupUrl.Contains("get_appendpaths"))
        {
            return Task.FromResult(_extraPathsAppendPaths);
        }
        
        return Task.FromResult(_returnValues);
    }
}