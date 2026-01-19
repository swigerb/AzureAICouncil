using System.Text.Json;
using System.Text.Json.Serialization;
using AzureAiCouncil.Core.Models;

namespace AzureAiCouncil.Tests;

public sealed class SerializationTests
{
    private static readonly JsonSerializerOptions WireJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
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
}
