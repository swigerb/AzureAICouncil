using AzureAiCouncil.Core.Models;

namespace AzureAiCouncil.Core.Services;

public interface IDeliberationStore
{
    Deliberation Create(Motion motion);

    bool TryGet(string deliberationId, out Deliberation deliberation);

    void UpdateStatus(string deliberationId, DeliberationStatus status);

    void AddStatement(string deliberationId, CouncilorStatement statement);

    void SetResolution(string deliberationId, CouncilResolution resolution);

    IReadOnlyList<Deliberation> List();
}
