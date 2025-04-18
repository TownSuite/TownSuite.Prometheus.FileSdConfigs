﻿/*
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
using Microsoft.Extensions.Logging;
using TownSuite.Prometheus.FileSdConfigs.V2;

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
var appSettings = config.GetRequiredSection("AppSettings").Get<AppSettings>();
var settings = config.GetRequiredSection("Settings").Get<TownSuite.Prometheus.FileSdConfigs.V1.Settings[]>();
var settingsV2 = config.GetRequiredSection("SettingsV2").Get<TownSuite.Prometheus.FileSdConfigs.V2.Settings[]>();


// https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line#non-host-console-app
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddFilter("Microsoft", LogLevel.Warning)
        .AddFilter("System", LogLevel.Warning)
        .AddFilter("TownSuite.Prometheus.FileSdConfigs.Program", LogLevel.Debug)
        .AddConsole();
});
ILogger logger = loggerFactory.CreateLogger<Program>();

try
{
    while (true)
    {
        logger.LogInformation("Process starting");
        using var httpClient = new HttpClient(){
            Timeout = TimeSpan.FromSeconds(appSettings.HttpTimeoutInSeconds)
        };

        await V1(httpClient);
        await V2(httpClient);
        await PrometheusMetrics(httpClient);
        await DnsSD(httpClient);

        logger.LogInformation($"Waiting for {appSettings.DelayInSeconds} seconds.");
        await Task.Delay(TimeSpan.FromSeconds(appSettings.DelayInSeconds));
    }
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Crashing");
    System.Environment.Exit(-1);
}

async Task V1(HttpClient httpClient)
{
    if (string.IsNullOrWhiteSpace(appSettings.OutputPath))
    {
        return;
    }
    
    var client = new Client(httpClient);
    var sd = new TownSuite.Prometheus.FileSdConfigs.V1.ServiceDiscovery(client, settings, appSettings);
    string tempPath = $"{appSettings.OutputPath}.tmp";
    await using (var fs = new FileStream(tempPath, FileMode.Create))
    {
        await sd.GenerateTargetFile(fs);
    }
    File.Move(tempPath, appSettings.OutputPath, true);
}

async Task V2(HttpClient httpClient)
{
    if (string.IsNullOrWhiteSpace(appSettings.OutputPathV2))
    {
        return;
    }

    var client = new Client(httpClient);
    var sd = new TownSuite.Prometheus.FileSdConfigs.V2.ServiceDiscovery<DestFileSdConfig>(client, settingsV2, appSettings, logger);
    string tempPath = $"{appSettings.OutputPathV2}.tmp";
    await using (var fs = new FileStream(tempPath, FileMode.Create))
    {
        await sd.GenerateTargetFile(fs);
    }
    File.Move(tempPath, appSettings.OutputPathV2, true);
}

async Task PrometheusMetrics(HttpClient httpClient)
{
    if (string.IsNullOrWhiteSpace(appSettings.OutputPathPrometheusMetrics))
    {
        return;
    }
    var client = new Client(httpClient);
    var sd = new TownSuite.Prometheus.FileSdConfigs.V2.ServiceDiscovery<PrometheusMetricsDestFileSdConfig>(client, settingsV2, appSettings, logger);
    string tempPath = $"{appSettings.OutputPathPrometheusMetrics}.tmp";
    await using (var fs = new FileStream(tempPath, FileMode.Create))
    {
        await sd.GenerateTargetFile(fs);
    }
    
    File.Move(tempPath, appSettings.OutputPathPrometheusMetrics, true);
}

async Task OpenTelemetryMetrics(HttpClient httpClient)
{
    if (string.IsNullOrWhiteSpace(appSettings.OutputPathOpenTelemetry))
    {
        return;
    }
    
    var client = new Client(httpClient);
    var sd = new TownSuite.Prometheus.FileSdConfigs.V2.ServiceDiscovery<OpenTelemetryDestFileSdConfig>(client, settingsV2, appSettings, logger);
    string tempPath = $"{appSettings.OutputPathOpenTelemetry}.tmp";
    await using (var fs = new FileStream(tempPath, FileMode.Create))
    {
        await sd.GenerateTargetFile(fs);
    }
    File.Move(tempPath, appSettings.OutputPathOpenTelemetry, true);
}

async Task DnsSD(HttpClient httpClient)
{
    if (string.IsNullOrWhiteSpace(appSettings.OutputPathDns))
    {
        return;
    }
    
    var client = new Client(httpClient);
    var sd = new TownSuite.Prometheus.FileSdConfigs.V2.ServiceDiscovery<DnsDestFileSdConfig>(client, settingsV2, appSettings, logger);
    string tempPath = $"{appSettings.OutputPathDns}.tmp";
    await using (var fs = new FileStream(tempPath, FileMode.Create))
    {
        await sd.GenerateTargetFile(fs);
    }
    File.Move(tempPath, appSettings.OutputPathDns, true);
}
