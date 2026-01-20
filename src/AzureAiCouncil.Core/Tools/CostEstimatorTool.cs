using AzureAiCouncil.Core.Models;

namespace AzureAiCouncil.Core.Tools;

public sealed class CostEstimatorTool : ITool
{
    public string Name => "cost_estimator";

    public string Description => "Estimates likely cost drivers and an overall cost tier.";

    public CostEstimateResult Estimate(string scenario, string? budget, string? timeline)
    {
        var drivers = new List<string>
        {
            "LLM inference (tokens, model tier)",
            "Streaming + real-time fanout", 
            "Telemetry/observability", 
            "Storage/persistence (future)",
        };

        var tier = Tier.Medium;
        if (!string.IsNullOrWhiteSpace(budget) && budget.Contains("low", StringComparison.OrdinalIgnoreCase))
        {
            tier = Tier.Low;
        }

        if (!string.IsNullOrWhiteSpace(scenario) && scenario.Contains("enterprise", StringComparison.OrdinalIgnoreCase))
        {
            tier = Tier.High;
        }

        return new CostEstimateResult
        {
            Scenario = scenario,
            CostTier = tier,
            CostDrivers = drivers,
            Notes = new[] { "This is a mock estimator for v1." },
        };
    }
}

public sealed record CostEstimateResult
{
    public required string Scenario { get; init; }

    public required Tier CostTier { get; init; }

    public IReadOnlyList<string> CostDrivers { get; init; } = [];

    public IReadOnlyList<string> Notes { get; init; } = [];
}
