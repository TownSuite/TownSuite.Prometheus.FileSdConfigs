namespace TownSuite.Prometheus.FileSdConfigs;

public class AppSettings
{
    public int DelayInSeconds { get; init; }
    public string UserAgent { get; init; }
    public string OutputPath { get; init; }
    public string OutputPathV2 { get; init; }
    public string OutputPathPrometheusMetrics { get; init; }
    public string OutputPathOpenTelemetry { get; init; }
}