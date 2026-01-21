using System.Text.Json;
using System.Text.Json.Serialization;
using AzureAiCouncil.Core.Models;
using AzureAiCouncil.Core.Serialization;

namespace AzureAiCouncil.Tests;

public sealed class SerializationTests
{
    private static readonly JsonSerializerOptions WireJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly JsonSerializerOptions CustomConverterJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new TierJsonConverter(),
            new RiskToleranceJsonConverter(),
            new GoNoGoJsonConverter(),
            new PriorityJsonConverter(),
        },
    };

    [Fact]
    public void GoNoGo_is_serialized_as_snake_case_no_go()
    {
        var statement = new CouncilorStatement
        {
            Councilor = CouncilorKind.Security,
            Round = 1,
            StanceTitle = "t",
            Answer = "a",
            Rationale = "r",
            Confidence = 0.9,
            Position = new CouncilorPosition
            {
                GoNoGo = GoNoGo.No_Go,
                Priority = Priority.High,
            },
        };

        var json = JsonSerializer.Serialize(statement, WireJson);

        Assert.Contains("\"goNoGo\":\"no_go\"", json);
        Assert.Contains("\"priority\":\"high\"", json);
    }

    [Fact]
    public void RiskTolerance_is_serialized_as_lowercase()
    {
        var motion = new Motion
        {
            MotionId = "m1",
            Question = "q",
            Constraints = new MotionConstraints { RiskTolerance = RiskTolerance.High },
        };

        var json = JsonSerializer.Serialize(motion, WireJson);

        Assert.Contains("\"riskTolerance\":\"high\"", json);
    }

    [Theory]
    [InlineData("go", GoNoGo.Go)]
    [InlineData("no_go", GoNoGo.No_Go)]
    [InlineData("mixed", GoNoGo.Mixed)]
    public void CouncilorPosition_can_be_deserialized_from_string(string position, GoNoGo expected)
    {
        var json = $$"""
        {
          "councilor": "Security",
          "round": 1,
          "stanceTitle": "t",
          "answer": "a",
          "rationale": "r",
          "confidence": 0.9,
          "position": "{{position}}"
        }
        """;

        var statement = JsonSerializer.Deserialize<CouncilorStatement>(json, WireJson);

        Assert.NotNull(statement);
        Assert.Equal(expected, statement.Position.GoNoGo);
    }

    // TierJsonConverter tests
    [Theory]
    [InlineData(Tier.Low, "low")]
    [InlineData(Tier.Medium, "medium")]
    [InlineData(Tier.High, "high")]
    [InlineData(Tier.Unknown, "unknown")]
    public void TierJsonConverter_serializes_correctly(Tier tier, string expected)
    {
        var wrapper = new TierWrapper { Tier = tier };

        var json = JsonSerializer.Serialize(wrapper, CustomConverterJson);

        Assert.Contains($"\"tier\":\"{expected}\"", json);
    }

    [Theory]
    [InlineData("low", Tier.Low)]
    [InlineData("LOW", Tier.Low)]
    [InlineData("medium", Tier.Medium)]
    [InlineData("MEDIUM", Tier.Medium)]
    [InlineData("high", Tier.High)]
    [InlineData("HIGH", Tier.High)]
    [InlineData("unknown", Tier.Unknown)]
    [InlineData("UNKNOWN", Tier.Unknown)]
    [InlineData("invalid", Tier.Unknown)]
    [InlineData("", Tier.Unknown)]
    public void TierJsonConverter_deserializes_correctly(string input, Tier expected)
    {
        var json = $$"""{"tier":"{{input}}"}""";

        var result = JsonSerializer.Deserialize<TierWrapper>(json, CustomConverterJson);

        Assert.NotNull(result);
        Assert.Equal(expected, result.Tier);
    }

    // GoNoGoJsonConverter tests
    [Theory]
    [InlineData(GoNoGo.Go, "go")]
    [InlineData(GoNoGo.No_Go, "no_go")]
    [InlineData(GoNoGo.Mixed, "mixed")]
    public void GoNoGoJsonConverter_serializes_correctly(GoNoGo goNoGo, string expected)
    {
        var wrapper = new GoNoGoWrapper { GoNoGo = goNoGo };

        var json = JsonSerializer.Serialize(wrapper, CustomConverterJson);

        Assert.Contains($"\"goNoGo\":\"{expected}\"", json);
    }

    [Theory]
    [InlineData("go", GoNoGo.Go)]
    [InlineData("GO", GoNoGo.Go)]
    [InlineData("no_go", GoNoGo.No_Go)]
    [InlineData("NO_GO", GoNoGo.No_Go)]
    [InlineData("mixed", GoNoGo.Mixed)]
    [InlineData("MIXED", GoNoGo.Mixed)]
    [InlineData("invalid", GoNoGo.Mixed)]
    public void GoNoGoJsonConverter_deserializes_correctly(string input, GoNoGo expected)
    {
        var json = $$"""{"goNoGo":"{{input}}"}""";

        var result = JsonSerializer.Deserialize<GoNoGoWrapper>(json, CustomConverterJson);

        Assert.NotNull(result);
        Assert.Equal(expected, result.GoNoGo);
    }

    // PriorityJsonConverter tests
    [Theory]
    [InlineData(Priority.Low, "low")]
    [InlineData(Priority.Medium, "medium")]
    [InlineData(Priority.High, "high")]
    public void PriorityJsonConverter_serializes_correctly(Priority priority, string expected)
    {
        var wrapper = new PriorityWrapper { Priority = priority };

        var json = JsonSerializer.Serialize(wrapper, CustomConverterJson);

        Assert.Contains($"\"priority\":\"{expected}\"", json);
    }

    [Theory]
    [InlineData("low", Priority.Low)]
    [InlineData("LOW", Priority.Low)]
    [InlineData("medium", Priority.Medium)]
    [InlineData("MEDIUM", Priority.Medium)]
    [InlineData("high", Priority.High)]
    [InlineData("HIGH", Priority.High)]
    [InlineData("invalid", Priority.Medium)]
    public void PriorityJsonConverter_deserializes_correctly(string input, Priority expected)
    {
        var json = $$"""{"priority":"{{input}}"}""";

        var result = JsonSerializer.Deserialize<PriorityWrapper>(json, CustomConverterJson);

        Assert.NotNull(result);
        Assert.Equal(expected, result.Priority);
    }

    // RiskToleranceJsonConverter tests
    [Theory]
    [InlineData(RiskTolerance.Low, "low")]
    [InlineData(RiskTolerance.Medium, "medium")]
    [InlineData(RiskTolerance.High, "high")]
    [InlineData(RiskTolerance.Unspecified, "unspecified")]
    public void RiskToleranceJsonConverter_serializes_correctly(RiskTolerance risk, string expected)
    {
        var wrapper = new RiskToleranceWrapper { RiskTolerance = risk };

        var json = JsonSerializer.Serialize(wrapper, CustomConverterJson);

        Assert.Contains($"\"riskTolerance\":\"{expected}\"", json);
    }

    [Theory]
    [InlineData("low", RiskTolerance.Low)]
    [InlineData("LOW", RiskTolerance.Low)]
    [InlineData("medium", RiskTolerance.Medium)]
    [InlineData("high", RiskTolerance.High)]
    [InlineData("unspecified", RiskTolerance.Unspecified)]
    [InlineData("invalid", RiskTolerance.Unspecified)]
    public void RiskToleranceJsonConverter_deserializes_correctly(string input, RiskTolerance expected)
    {
        var json = $$"""{"riskTolerance":"{{input}}"}""";

        var result = JsonSerializer.Deserialize<RiskToleranceWrapper>(json, CustomConverterJson);

        Assert.NotNull(result);
        Assert.Equal(expected, result.RiskTolerance);
    }

    // Test wrapper records for isolated converter testing
    private sealed record TierWrapper
    {
        public Tier Tier { get; init; }
    }

    private sealed record GoNoGoWrapper
    {
        public GoNoGo GoNoGo { get; init; }
    }

    private sealed record PriorityWrapper
    {
        public Priority Priority { get; init; }
    }

    private sealed record RiskToleranceWrapper
    {
        public RiskTolerance RiskTolerance { get; init; }
    }
}
