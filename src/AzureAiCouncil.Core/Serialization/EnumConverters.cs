using System.Text.Json;
using System.Text.Json.Serialization;
using AzureAiCouncil.Core.Models;

namespace AzureAiCouncil.Core.Serialization;

public sealed class TierJsonConverter : JsonConverter<Tier>
{
    public override Tier Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        return s?.ToLowerInvariant() switch
        {
            "low" => Tier.Low,
            "medium" => Tier.Medium,
            "high" => Tier.High,
            "unknown" => Tier.Unknown,
            _ => Tier.Unknown,
        };
    }

    public override void Write(Utf8JsonWriter writer, Tier value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value switch
        {
            Tier.Low => "low",
            Tier.Medium => "medium",
            Tier.High => "high",
            _ => "unknown",
        });
}

public sealed class RiskToleranceJsonConverter : JsonConverter<RiskTolerance>
{
    public override RiskTolerance Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        return s?.ToLowerInvariant() switch
        {
            "low" => RiskTolerance.Low,
            "medium" => RiskTolerance.Medium,
            "high" => RiskTolerance.High,
            "unspecified" => RiskTolerance.Unspecified,
            _ => RiskTolerance.Unspecified,
        };
    }

    public override void Write(Utf8JsonWriter writer, RiskTolerance value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value switch
        {
            RiskTolerance.Low => "low",
            RiskTolerance.Medium => "medium",
            RiskTolerance.High => "high",
            _ => "unspecified",
        });
}

public sealed class GoNoGoJsonConverter : JsonConverter<GoNoGo>
{
    public override GoNoGo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        return s?.ToLowerInvariant() switch
        {
            "go" => GoNoGo.Go,
            "no_go" => GoNoGo.No_Go,
            "mixed" => GoNoGo.Mixed,
            _ => GoNoGo.Mixed,
        };
    }

    public override void Write(Utf8JsonWriter writer, GoNoGo value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value switch
        {
            GoNoGo.Go => "go",
            GoNoGo.No_Go => "no_go",
            _ => "mixed",
        });
}

public sealed class PriorityJsonConverter : JsonConverter<Priority>
{
    public override Priority Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        return s?.ToLowerInvariant() switch
        {
            "low" => Priority.Low,
            "medium" => Priority.Medium,
            "high" => Priority.High,
            _ => Priority.Medium,
        };
    }

    public override void Write(Utf8JsonWriter writer, Priority value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value switch
        {
            Priority.Low => "low",
            Priority.High => "high",
            _ => "medium",
        });
}
