/*
MIT License

Copyright (c) 2022 TownSuite Municipal Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using TownSuite.Prometheus.FileSdConfigs;
using Microsoft.Extensions.Hosting;

//using IHost host = Host.CreateDefaultBuilder(args).Build();

// Application code should start here.

//await host.RunAsync();


IConfiguration config = new ConfigurationBuilder()
#if DEBUG
    .AddJsonFile("appsettings.Development.json")
#else
    .AddJsonFile("appsettings.json")
#endif
    .AddEnvironmentVariables()
    .Build();

// Get values from the config given their key and their target type.
var settings = config.GetRequiredSection("Settings").Get<Settings[]>();
string outputpath = config.GetValue<string>("OutputPath");
int delayInSeconds = config.GetValue<int>("DelayInSeconds");

while (true)
{
    Console.WriteLine("Process starting");

    using var client = new HttpClient();

    var targets = new List<DestFileSdConfig>();
    foreach (var setting in settings)
    {
        string[] retrievedHosts;
        if (setting.LookupUrl.StartsWith("http"))
        {
            retrievedHosts = await GetJsonFromContent(setting.LookupUrl, client, setting.AuthHeader);
        }
        else
        {
            retrievedHosts = JsonSerializer.Deserialize<string[]>(System.IO.File.ReadAllText(setting.LookupUrl));
        }


        targets.Add(new DestFileSdConfig(retrievedHosts, setting));
    }

    string jsonString = JsonSerializer.Serialize(targets);
    File.WriteAllText(outputpath, jsonString);

    Console.WriteLine($"Waiting for {delayInSeconds} seconds.");
    await Task.Delay(TimeSpan.FromSeconds(delayInSeconds));
}

static async Task<string[]> GetJsonFromContent(string uri, HttpClient httpClient, string authHeader)
{
    var request = new HttpRequestMessage(HttpMethod.Get, uri);
    if (!string.IsNullOrWhiteSpace(authHeader))
    {
        request.Headers.TryAddWithoutValidation("Authorization", authHeader);
    }

    using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

    if (response.IsSuccessStatusCode)
    {
        return await response.Content.ReadFromJsonAsync<string[]>();
    }

    return null;
}