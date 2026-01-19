using AzureAiCouncil.Core.Models;

namespace AzureAiCouncil.Core.Services;

public interface ISpeakerAgent
{
    Task<Motion> NormalizeAsync(Motion motion, CancellationToken cancellationToken);
}

public interface ICouncilorAgent
{
    CouncilorKind Kind { get; }

    Task<CouncilorStatement> ProduceStatementAsync(Motion motion, int round, string goal, IReadOnlyList<CouncilorStatement> priorStatements, CancellationToken cancellationToken);
}

public interface IClerkAgent
{
    Task<CouncilResolution> ProduceResolutionAsync(string deliberationId, Motion motion, IReadOnlyList<CouncilorStatement> transcript, CancellationToken cancellationToken);
}
