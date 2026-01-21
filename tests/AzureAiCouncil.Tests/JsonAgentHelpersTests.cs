using System.Text.Json;
using AzureAiCouncil.Agents;
using AzureAiCouncil.Core.Models;
using AzureAiCouncil.Core.Services;
using Microsoft.Extensions.AI;

namespace AzureAiCouncil.Tests;

public sealed class JsonAgentHelpersTests
{
    [Fact]
    public void ToJson_serializes_object_as_json()
    {
        var obj = new { Name = "Test", Value = 42 };

        var json = JsonAgentHelpers.ToJson(obj);

        Assert.Contains("\"name\":\"Test\"", json);
        Assert.Contains("\"value\":42", json);
    }

    [Fact]
    public void ToJson_uses_web_defaults_camelCase()
    {
        var obj = new { PascalCase = "value" };

        var json = JsonAgentHelpers.ToJson(obj);

        Assert.Contains("\"pascalCase\"", json);
        Assert.DoesNotContain("\"PascalCase\"", json);
    }

    [Fact]
    public void ToJson_does_not_indent()
    {
        var obj = new { A = 1, B = 2 };

        var json = JsonAgentHelpers.ToJson(obj);

        Assert.DoesNotContain("\n", json);
    }

    [Fact]
    public void ParseJsonOrThrow_deserializes_valid_json()
    {
        var json = """{"name":"Alice","age":30}""";

        var result = JsonAgentHelpers.ParseJsonOrThrow<TestPerson>(json);

        Assert.Equal("Alice", result.Name);
        Assert.Equal(30, result.Age);
    }

    [Fact]
    public void ParseJsonOrThrow_throws_when_deserialization_returns_null()
    {
        var json = "null";

        Assert.Throws<InvalidOperationException>(() =>
            JsonAgentHelpers.ParseJsonOrThrow<TestPerson>(json));
    }

    [Fact]
    public void ParseJsonOrThrow_throws_on_malformed_json()
    {
        var json = "{ invalid json }";

        Assert.Throws<JsonException>(() =>
            JsonAgentHelpers.ParseJsonOrThrow<TestPerson>(json));
    }

    [Fact]
    public void ParseJsonOrThrow_supports_string_enum_values()
    {
        // Model/agent JSON commonly uses enum names rather than numeric values.
        var json = """{"round":1,"councilor":"Vision","state":"running"}""";

        var result = JsonAgentHelpers.ParseJsonOrThrow<CouncilorStatusUpdate>(json);

        Assert.Equal(1, result.Round);
        Assert.Equal(CouncilorKind.Vision, result.Councilor);
        Assert.Equal("running", result.State);
    }

    [Fact]
    public void CreateCouncilJsonOptions_sets_temperature()
    {
        var options = JsonAgentHelpers.CreateCouncilJsonOptions(0.7f);

        Assert.Equal(0.7f, options.Temperature);
    }

    [Fact]
    public void CreateCouncilJsonOptions_sets_json_response_format()
    {
        var options = JsonAgentHelpers.CreateCouncilJsonOptions(0.5f);

        Assert.Equal(ChatResponseFormat.Json, options.ResponseFormat);
    }

    [Fact]
    public void CreateCouncilJsonOptions_with_empty_tools_does_not_set_tools()
    {
        var options = JsonAgentHelpers.CreateCouncilJsonOptions(0.5f, tools: []);

        Assert.Null(options.Tools);
    }

    [Fact]
    public void CreateCouncilJsonOptions_with_tools_sets_tools()
    {
        var tools = new List<AITool> { new TestAITool() };

        var options = JsonAgentHelpers.CreateCouncilJsonOptions(0.5f, tools);

        Assert.NotNull(options.Tools);
        Assert.Single(options.Tools);
    }

    [Theory]
    [InlineData(CouncilorKind.Vision, "Vision")]
    [InlineData(CouncilorKind.Risk, "Risk")]
    [InlineData(CouncilorKind.Architecture, "Architecture")]
    [InlineData(CouncilorKind.Security, "Security")]
    [InlineData(CouncilorKind.Finance, "Finance")]
    public void CouncilorDisplay_returns_expected_names(CouncilorKind kind, string expected)
    {
        var result = JsonAgentHelpers.CouncilorDisplay(kind);

        Assert.Equal(expected, result);
    }

    private sealed record TestPerson(string Name, int Age);

    private sealed class TestAITool : AITool;
}
