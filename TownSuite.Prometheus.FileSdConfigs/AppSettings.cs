namespace TownSuite.Prometheus.FileSdConfigs;

public class AppSettings
{
    public int DelayInSeconds { get; set; }
    public string UserAgent { get; set; }
    public string OutputPath { get; set; }
    public string OutputPathV2 { get; set; }
    public string OutputPathPrometheusMetrics { get; set; }
    public string OutputPathOpenTelemetry { get; set; }
    public string OutputPathDns { get; set; }
    public int HttpTimeoutInSeconds { get; set; }
    public bool SkipCertificateValidation { get; set; }
}