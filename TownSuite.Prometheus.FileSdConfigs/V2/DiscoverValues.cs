namespace TownSuite.Prometheus.FileSdConfigs.V2;

public class DiscoverValues
{
    public string Version { get; init; }
    public List<ServiceInfo> Services { get; init; }
}