using AzureAiCouncil.Core.Models;
using AzureAiCouncil.Core.Services;

namespace AzureAiCouncil.Agents;

public sealed class MockSpeakerAgent : ISpeakerAgent
{
    public Task<Motion> NormalizeAsync(Motion motion, CancellationToken cancellationToken) =>
        Task.FromResult(motion with { Question = motion.Question.Trim() });
}

public sealed class MockCouncilorAgent(CouncilorKind kind) : ICouncilorAgent
{
    public CouncilorKind Kind { get; } = kind;

    public Task<CouncilorStatement> ProduceStatementAsync(Motion motion, int round, string goal, IReadOnlyList<CouncilorStatement> priorStatements, CancellationToken cancellationToken)
    {
        var statement = new CouncilorStatement
        {
            Councilor = Kind,
            Round = round,
            StanceTitle = $"{Kind} view ({goal})",
            Answer = $"Mock statement for {Kind} in round {round}. Provide AzureOpenAI config to enable real deliberation.",
            Rationale = "Mock agent used because AzureOpenAI is not configured.",
            RisksOrConcerns = new[] { "Model not configured" },
            CostTier = Tier.Unknown,
            ComplexityTier = Tier.Unknown,
            Confidence = 0.2,
            Position = new CouncilorPosition { GoNoGo = GoNoGo.Mixed, Priority = Priority.Medium },
            NotesForCouncil = new[] { "Set AzureOpenAI:Endpoint, AzureOpenAI:ApiKey, AzureOpenAI:Deployment." },
            MermaidHint = Kind == CouncilorKind.Architecture ? "graph LR; Client-->API; API-->Council;" : null,
        };

        return Task.FromResult(statement);
    }
}

public sealed class MockClerkAgent : IClerkAgent
{
    public Task<CouncilResolution> ProduceResolutionAsync(string deliberationId, Motion motion, IReadOnlyList<CouncilorStatement> transcript, CancellationToken cancellationToken)
    {
        var resolution = new CouncilResolution
        {
            DeliberationId = deliberationId,
            Summary = "Mock resolution (AzureOpenAI not configured).",
            Recommendation = "Configure Azure OpenAI and rerun.",
            Pros = new[] { "Project scaffolding is in place" },
            Cons = new[] { "No LLM calls yet" },
            Scores = new CouncilResolutionScores { Risk = Tier.Unknown, Cost = Tier.Unknown, Complexity = Tier.Unknown, Confidence = 0.2 },
            WhenThisIsAGoodIdea = new[] { "Validating the API/streaming UX" },
            WhenToAvoid = new[] { "When you need real deliberation quality" },
            SuggestedNextSteps = new[] { "Set AzureOpenAI config", "Restart AppHost", "Submit a motion" },
            MermaidArchitectureSnippet = "",
        };

        return Task.FromResult(resolution);
    }
}
