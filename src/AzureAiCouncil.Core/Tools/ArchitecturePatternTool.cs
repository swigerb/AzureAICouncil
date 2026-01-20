namespace AzureAiCouncil.Core.Tools;

public sealed class ArchitecturePatternTool : ITool
{
    public string Name => "architecture_patterns";

    public string Description => "Suggests architecture patterns that fit a scenario.";

    public ArchitecturePatternResult Suggest(string scenario)
    {
        var patterns = new List<string>
        {
            "Monolith-first (modular) with clear boundaries",
            "Event-driven workflow orchestration",
            "CQRS for auditability of deliberations",
            "RAG for policy/knowledge grounding",
        };

        if (scenario.Contains("simple", StringComparison.OrdinalIgnoreCase))
        {
            patterns.Insert(0, "Keep it simple: single service + in-memory store");
        }

        return new ArchitecturePatternResult
        {
            Scenario = scenario,
            Patterns = patterns,
            Notes = new[] { "This is a mock pattern suggester for v1." },
        };
    }
}

public sealed record ArchitecturePatternResult
{
    public required string Scenario { get; init; }

    public IReadOnlyList<string> Patterns { get; init; } = [];

    public IReadOnlyList<string> Notes { get; init; } = [];
}
