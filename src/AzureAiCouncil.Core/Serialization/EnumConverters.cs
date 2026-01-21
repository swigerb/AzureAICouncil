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

public sealed class CouncilorPositionJsonConverter : JsonConverter<CouncilorPosition>
{
    private static GoNoGo ParseGoNoGo(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return GoNoGo.Mixed;
        return s.Trim().ToLowerInvariant() switch
        {
            "go" => GoNoGo.Go,
            "no_go" => GoNoGo.No_Go,
            "no-go" => GoNoGo.No_Go,
            "nogo" => GoNoGo.No_Go,
            "mixed" => GoNoGo.Mixed,
            _ => s.Contains("no", StringComparison.OrdinalIgnoreCase) ? GoNoGo.No_Go :
                s.Contains("go", StringComparison.OrdinalIgnoreCase) ? GoNoGo.Go :
                GoNoGo.Mixed,
        };
    }

    private static Priority ParsePriority(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return Priority.Medium;
        return s.Trim().ToLowerInvariant() switch
        {
            "low" => Priority.Low,
            "medium" => Priority.Medium,
            "high" => Priority.High,
            _ => Priority.Medium,
        };
    }

    public override CouncilorPosition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.Null)
        {
            return new CouncilorPosition();
        }

    if (reader.TokenType is JsonTokenType.String)
        {
            var s = reader.GetString();
            return new CouncilorPosition
            {
                GoNoGo = ParseGoNoGo(s),
                Priority = Priority.Medium,
            };
        }

        if (reader.TokenType is not JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected position to be object|string|null, but was {reader.TokenType}.");
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var goNoGo = GoNoGo.Mixed;
        var priority = Priority.Medium;

        if (root.TryGetProperty("goNoGo", out var goNoGoEl) && goNoGoEl.ValueKind is JsonValueKind.String)
        {
            goNoGo = ParseGoNoGo(goNoGoEl.GetString());
        }
        else if (root.TryGetProperty("gonogo", out var goNoGoAltEl) && goNoGoAltEl.ValueKind is JsonValueKind.String)
        {
            goNoGo = ParseGoNoGo(goNoGoAltEl.GetString());
        }

        if (root.TryGetProperty("priority", out var priorityEl) && priorityEl.ValueKind is JsonValueKind.String)
        {
            priority = ParsePriority(priorityEl.GetString());
        }

        return new CouncilorPosition
        {
            GoNoGo = goNoGo,
            Priority = priority,
        };
    }

    public override void Write(Utf8JsonWriter writer, CouncilorPosition value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("goNoGo", value.GoNoGo switch
        {
            GoNoGo.Go => "go",
            GoNoGo.No_Go => "no_go",
            _ => "mixed",
        });
        writer.WriteString("priority", value.Priority switch
        {
            Priority.Low => "low",
            Priority.High => "high",
            _ => "medium",
        });
        writer.WriteEndObject();
    }
}
