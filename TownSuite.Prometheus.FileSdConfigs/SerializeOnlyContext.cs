using System.Text.Json.Serialization;

namespace TownSuite.Prometheus.FileSdConfigs;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization)]
[JsonSerializable(typeof(V1.DestFileSdConfig))]
[JsonSerializable(typeof(V2.DestFileSdConfig))]
[JsonSerializable(typeof(V2.DnsDestFileSdConfig))]
[JsonSerializable(typeof(V2.PrometheusMetricsDestFileSdConfig))]
[JsonSerializable(typeof(V2.OpenTelemetryDestFileSdConfig))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(V2.DiscoverValues))]
internal partial class SerializeOnlyContext : JsonSerializerContext
{
}
