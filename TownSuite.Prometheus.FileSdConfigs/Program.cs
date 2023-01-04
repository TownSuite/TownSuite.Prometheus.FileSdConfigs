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
var settings = config.GetRequiredSection("Settings").Get<TownSuite.Prometheus.FileSdConfigs.V1.Settings[]>();
var settingsV2 = config.GetRequiredSection("SettingsV2").Get<TownSuite.Prometheus.FileSdConfigs.V2.Settings[]>();
string outputpath = config.GetValue<string>("OutputPath");
int delayInSeconds = config.GetValue<int>("DelayInSeconds");

while (true)
{
    Console.WriteLine("Process starting");
    using var httpClient = new HttpClient();

    V1(httpClient);
    V2(httpClient);

    Console.WriteLine($"Waiting for {delayInSeconds} seconds.");
    await Task.Delay(TimeSpan.FromSeconds(delayInSeconds));
}

async Task V1(HttpClient httpClient)
{
    var client = new Client(httpClient);
    var sd = new TownSuite.Prometheus.FileSdConfigs.V1.ServiceDiscovery(client, settings);
    await using var fs = new FileStream(outputpath, FileMode.Create);
    await sd.GenerateTargetFile(fs);
}

async Task V2(HttpClient httpClient)
{
    var client = new Client(httpClient);
    var sd = new TownSuite.Prometheus.FileSdConfigs.V2.ServiceDiscovery(client, settingsV2);
    await using var fs = new FileStream(outputpath, FileMode.Create);
    await sd.GenerateTargetFile(fs);
}
