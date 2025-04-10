using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace TownSuite.Prometheus.FileSdConfigs;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(V1.DestFileSdConfig))]
[JsonSerializable(typeof(List<V1.DestFileSdConfig>))]
[JsonSerializable(typeof(string[]))]
internal partial class SerializeOnlyContextV1 : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(V2.DestFileSdConfig))]
[JsonSerializable(typeof(List<V2.DestFileSdConfig>))]
[JsonSerializable(typeof(V2.DnsDestFileSdConfig))]
[JsonSerializable(typeof(List<V2.DnsDestFileSdConfig>))]
[JsonSerializable(typeof(List<V2.PrometheusMetricsDestFileSdConfig>))]
[JsonSerializable(typeof(V2.PrometheusMetricsDestFileSdConfig))]
[JsonSerializable(typeof(V2.OpenTelemetryDestFileSdConfig))]
[JsonSerializable(typeof(List<V2.OpenTelemetryDestFileSdConfig>))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(V2.DiscoverValues))]
internal partial class SerializeOnlyContextV2 : JsonSerializerContext
{
}

public static class JsonOptions
{
    public static JsonSerializerOptions GetSerilizer
    {
        get => new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver =
                JsonTypeInfoResolver.Combine(SerializeOnlyContextV1.Default, SerializeOnlyContextV2.Default)
        };
    }
}



