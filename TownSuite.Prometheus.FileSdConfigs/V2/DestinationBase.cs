using System.Text.Json.Serialization;

namespace TownSuite.Prometheus.FileSdConfigs.V2;

public class DestinationBase
{
    [JsonPropertyName("targets")] public SortedSet<string> Targets { get; private init; } = new();


    [JsonPropertyName("labels")]
    public SortedDictionary<string, string> Labels { get; private init; } = new SortedDictionary<string, string>();

    public override bool Equals(object obj)
    {
        if (obj is not DestinationBase other)
            return false;

        return Targets.SetEquals(other.Targets) && Labels.SequenceEqual(other.Labels);
    }
}