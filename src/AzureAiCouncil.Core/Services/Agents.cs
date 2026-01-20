using AzureAiCouncil.Core.Models;

namespace AzureAiCouncil.Core.Services;

/// <summary>
/// Normalizes user input into a well-formed motion.
/// </summary>
public interface ISpeakerAgent
{
    /// <summary>
    /// Normalizes the given motion, clarifying the question and constraints.
    /// </summary>
    /// <param name="motion">The raw motion to normalize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The normalized motion.</returns>
    Task<Motion> NormalizeAsync(Motion motion, CancellationToken cancellationToken);
}

/// <summary>
/// A councilor agent that provides a specific perspective during deliberation.
/// </summary>
public interface ICouncilorAgent
{
    /// <summary>
    /// Gets the kind/role of this councilor.
    /// </summary>
    CouncilorKind Kind { get; }

    /// <summary>
    /// Produces a statement for the given round of deliberation.
    /// </summary>
    /// <param name="motion">The motion being deliberated.</param>
    /// <param name="round">The current round number (1-3).</param>
    /// <param name="goal">The goal for this round.</param>
    /// <param name="priorStatements">Statements from prior rounds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The councilor's statement.</returns>
    Task<CouncilorStatement> ProduceStatementAsync(
        Motion motion,
        int round,
        string goal,
        IReadOnlyList<CouncilorStatement> priorStatements,
        CancellationToken cancellationToken);
}

/// <summary>
/// Synthesizes councilor statements into a final resolution.
/// </summary>
public interface IClerkAgent
{
    /// <summary>
    /// Produces the final resolution based on the full transcript.
    /// </summary>
    /// <param name="deliberationId">The deliberation ID.</param>
    /// <param name="motion">The motion that was deliberated.</param>
    /// <param name="transcript">All councilor statements from all rounds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The final council resolution.</returns>
    Task<CouncilResolution> ProduceResolutionAsync(
        string deliberationId,
        Motion motion,
        IReadOnlyList<CouncilorStatement> transcript,
        CancellationToken cancellationToken);
}
