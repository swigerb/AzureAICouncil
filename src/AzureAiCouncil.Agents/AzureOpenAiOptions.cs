namespace AzureAiCouncil.Agents;

public sealed record AzureOpenAiOptions
{
    public string? Endpoint { get; init; }

    public string? ApiKey { get; init; }

    public string? Deployment { get; init; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint)
        && !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(Deployment);
}
