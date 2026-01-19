namespace AzureAiCouncil.Core.Models;

public sealed record Deliberation
{
    public required string DeliberationId { get; init; }

    public required Motion Motion { get; init; }

    public DeliberationStatus Status { get; set; } = DeliberationStatus.AwaitingCouncil;

    public List<CouncilorStatement> Statements { get; init; } = new();

    public CouncilResolution? Resolution { get; set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAt { get; set; }
}

public enum DeliberationStatus
{
    AwaitingCouncil,
    Round1,
    Round2,
    Round3,
    ResolutionIssued,
    Failed,
}
