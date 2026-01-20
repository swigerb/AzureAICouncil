using AzureAiCouncil.Core.Tools;

namespace AzureAiCouncil.Tests;

public sealed class ArchitecturePatternToolTests
{
    private readonly ArchitecturePatternTool _tool = new();

    [Fact]
    public void Name_and_Description_are_defined()
    {
        Assert.Equal("architecture_patterns", _tool.Name);
        Assert.False(string.IsNullOrWhiteSpace(_tool.Description));
    }

    [Fact]
    public void Suggest_returns_patterns_for_normal_scenario()
    {
        var result = _tool.Suggest("web api with database");

        Assert.Equal("web api with database", result.Scenario);
        Assert.NotEmpty(result.Patterns);
        Assert.NotEmpty(result.Notes);
    }

    [Fact]
    public void Suggest_returns_base_patterns()
    {
        var result = _tool.Suggest("any scenario");

        Assert.Contains(result.Patterns, p => p.Contains("Monolith", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Patterns, p => p.Contains("Event-driven", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Patterns, p => p.Contains("CQRS", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Patterns, p => p.Contains("RAG", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Suggest_inserts_keep_it_simple_pattern_first_when_scenario_contains_simple()
    {
        var result = _tool.Suggest("simple microservice");

        Assert.StartsWith("Keep it simple", result.Patterns[0]);
        Assert.True(result.Patterns.Count >= 5); // Base 4 + the simple pattern
    }

    [Theory]
    [InlineData("SIMPLE")]
    [InlineData("Simple")]
    [InlineData("a simple api")]
    public void Suggest_handles_case_insensitive_simple_scenarios(string scenario)
    {
        var result = _tool.Suggest(scenario);

        Assert.StartsWith("Keep it simple", result.Patterns[0]);
    }

    [Fact]
    public void Suggest_does_not_include_simple_pattern_for_complex_scenario()
    {
        var result = _tool.Suggest("enterprise distributed system");

        Assert.DoesNotContain(result.Patterns, p => p.StartsWith("Keep it simple"));
    }

    [Fact]
    public void Suggest_includes_notes()
    {
        var result = _tool.Suggest("test scenario");

        Assert.Contains(result.Notes, n => n.Contains("mock", StringComparison.OrdinalIgnoreCase));
    }
}
