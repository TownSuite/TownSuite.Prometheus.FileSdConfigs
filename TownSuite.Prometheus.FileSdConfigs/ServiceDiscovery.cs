using System.Text;
using System.Text.Json;

namespace TownSuite.Prometheus.FileSdConfigs;

public class ServiceDiscovery
{
    private Client _client;
    public ServiceDiscovery(Client client)
    {
        _client = client;
    }

    public async Task GenerateTargetFile(Settings[] settings, Stream output)
    {
        var targets = new List<DestFileSdConfig>();
        foreach (var setting in settings)
        {
            
            string[] retrievedHosts;
            if (setting.LookupUrl.StartsWith("http"))
            {
                retrievedHosts = await  _client.GetJsonListFromContent(setting.AuthHeader, setting.LookupUrl);
            }
            else
            {
                retrievedHosts = JsonSerializer.Deserialize<string[]>(System.IO.File.ReadAllText(setting.LookupUrl));
            }


            targets.Add(await DestFileSdConfig.Create(retrievedHosts, setting, _client));
        }

        string jsonString = JsonSerializer.Serialize(targets);

        await using var sw = new StreamWriter(output);
        await sw.WriteAsync(jsonString);
    }
}