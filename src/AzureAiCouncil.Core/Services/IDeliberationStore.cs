using AzureAiCouncil.Core.Models;

namespace AzureAiCouncil.Core.Services;

/// <summary>
/// Stores and manages deliberation state.
/// </summary>
public interface IDeliberationStore
{
    /// <summary>
    /// Creates a new deliberation for the given motion.
    /// </summary>
    /// <param name="motion">The motion to deliberate on.</param>
    /// <returns>The newly created deliberation.</returns>
    Deliberation Create(Motion motion);

    /// <summary>
    /// Attempts to retrieve a deliberation by ID.
    /// </summary>
    /// <param name="deliberationId">The deliberation ID.</param>
    /// <param name="deliberation">The retrieved deliberation, if found.</param>
    /// <returns><c>true</c> if found; otherwise <c>false</c>.</returns>
    bool TryGet(string deliberationId, out Deliberation deliberation);

    /// <summary>
    /// Updates the status of a deliberation.
    /// </summary>
    /// <param name="deliberationId">The deliberation ID.</param>
    /// <param name="status">The new status.</param>
    void UpdateStatus(string deliberationId, DeliberationStatus status);

    /// <summary>
    /// Adds a councilor statement to a deliberation.
    /// </summary>
    /// <param name="deliberationId">The deliberation ID.</param>
    /// <param name="statement">The statement to add.</param>
    void AddStatement(string deliberationId, CouncilorStatement statement);

    /// <summary>
    /// Sets the final resolution for a deliberation.
    /// </summary>
    /// <param name="deliberationId">The deliberation ID.</param>
    /// <param name="resolution">The resolution to set.</param>
    void SetResolution(string deliberationId, CouncilResolution resolution);

    /// <summary>
    /// Lists all deliberations.
    /// </summary>
    /// <returns>A read-only list of all deliberations.</returns>
    IReadOnlyList<Deliberation> List();
}
