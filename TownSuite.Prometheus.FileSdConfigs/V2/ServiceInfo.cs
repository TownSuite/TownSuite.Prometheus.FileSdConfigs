namespace TownSuite.Prometheus.FileSdConfigs.V2;

public class ServiceInfo
{
    /// <summary>
    /// Human readable name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Guid
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// ENV, JOB, etc map directly to prometheus
    /// </summary>
    public Dictionary<string, string> Labels { get; set; }

    /// <summary>
    /// IP/url, port, data center, BaseUrl, HealthCheckUrl etc.   Any other details that might need to be tracked.
    /// </summary>
    public Dictionary<string, string> Attributes { get; set; }
}