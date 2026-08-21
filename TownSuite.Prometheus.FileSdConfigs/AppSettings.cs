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

    /// <summary>
    /// How many consecutive incomplete passes may keep serving the previous target files
    /// before the incomplete list is written anyway.  Zero or less keeps the last good
    /// files for as long as the lookups keep failing.
    /// </summary>
    public int MaxStaleCycles { get; set; }
}