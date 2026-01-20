using AzureAiCouncil.Core.Models;
using AzureAiCouncil.Core.Tools;

namespace AzureAiCouncil.Tests;

public sealed class CostEstimatorToolTests
{
    private readonly CostEstimatorTool _tool = new();

    [Fact]
    public void Name_and_Description_are_defined()
    {
        Assert.Equal("cost_estimator", _tool.Name);
        Assert.False(string.IsNullOrWhiteSpace(_tool.Description));
    }

    [Fact]
    public void Estimate_returns_medium_tier_by_default()
    {
        var result = _tool.Estimate("standard web api", budget: null, timeline: null);

        Assert.Equal(Tier.Medium, result.CostTier);
        Assert.Equal("standard web api", result.Scenario);
        Assert.NotEmpty(result.CostDrivers);
        Assert.NotEmpty(result.Notes);
    }

    [Fact]
    public void Estimate_returns_low_tier_when_budget_contains_low()
    {
        var result = _tool.Estimate("web service", budget: "low budget project", timeline: null);

        Assert.Equal(Tier.Low, result.CostTier);
    }

    [Theory]
    [InlineData("LOW")]
    [InlineData("Low")]
    [InlineData("very low")]
    public void Estimate_returns_low_tier_for_case_insensitive_low_budget(string budget)
    {
        var result = _tool.Estimate("scenario", budget: budget, timeline: null);

        Assert.Equal(Tier.Low, result.CostTier);
    }

    [Fact]
    public void Estimate_returns_high_tier_when_scenario_contains_enterprise()
    {
        var result = _tool.Estimate("enterprise-scale deployment", budget: null, timeline: null);

        Assert.Equal(Tier.High, result.CostTier);
    }

    [Theory]
    [InlineData("ENTERPRISE")]
    [InlineData("Enterprise")]
    [InlineData("for enterprise")]
    public void Estimate_returns_high_tier_for_case_insensitive_enterprise_scenario(string scenario)
    {
        var result = _tool.Estimate(scenario, budget: null, timeline: null);

        Assert.Equal(Tier.High, result.CostTier);
    }

    [Fact]
    public void Estimate_enterprise_scenario_overrides_low_budget()
    {
        // Enterprise scenario takes precedence (evaluated second)
        var result = _tool.Estimate("enterprise platform", budget: "low", timeline: null);

        Assert.Equal(Tier.High, result.CostTier);
    }

    [Fact]
    public void Estimate_includes_expected_cost_drivers()
    {
        var result = _tool.Estimate("any scenario", budget: null, timeline: null);

        Assert.Contains(result.CostDrivers, d => d.Contains("LLM", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.CostDrivers, d => d.Contains("Streaming", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.CostDrivers, d => d.Contains("Telemetry", StringComparison.OrdinalIgnoreCase));
    }
}
