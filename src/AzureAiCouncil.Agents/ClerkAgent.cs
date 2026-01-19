using System.Text.Json;
using AzureAiCouncil.Core.Models;
using AzureAiCouncil.Core.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AzureAiCouncil.Agents;

public sealed class ClerkAgent : IClerkAgent
{
    private readonly IChatClient _chat;
    private readonly PromptRenderer _prompts;
    private readonly ILogger<ClerkAgent> _logger;

    public ClerkAgent(IChatClient chat, PromptRenderer prompts, ILogger<ClerkAgent> logger)
    {
        _chat = chat;
        _prompts = prompts;
        _logger = logger;
    }

    public async Task<CouncilResolution> ProduceResolutionAsync(
        string deliberationId,
        Motion motion,
        IReadOnlyList<CouncilorStatement> transcript,
        CancellationToken cancellationToken)
    {
        var prompt = _prompts.RenderFromFile(PromptCatalog.Clerk, new Dictionary<string, string>());

        var input = new
        {
            deliberationId,
            motion,
            transcript,
        };

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You synthesize multi-agent deliberations into a single resolution."),
            new(ChatRole.User, prompt + "\n\nInput:\n" + JsonAgentHelpers.ToJson(input)),
        };

        var options = JsonAgentHelpers.CreateCouncilJsonOptions(0.2f);

        var json = await GetJsonTextWithRepairAsync(messages, options, cancellationToken);
        var resolution = JsonAgentHelpers.ParseJsonOrThrow<CouncilResolution>(json);

        resolution = resolution with { DeliberationId = deliberationId };

        _logger.LogInformation("Clerk produced resolution for {DeliberationId}", deliberationId);
        return resolution;
    }

    private async Task<string> GetJsonTextWithRepairAsync(IReadOnlyList<ChatMessage> messages, ChatOptions options, CancellationToken cancellationToken)
    {
        try
        {
            return await GetJsonTextAsync(messages, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid JSON from clerk; retrying once");
            var repair = new List<ChatMessage>(messages)
            {
                new(ChatRole.User, "Your last response was invalid or did not match the schema. Output ONLY a single valid JSON object that matches the required schema. No markdown, no prose.")
            };
            return await GetJsonTextAsync(repair, options, cancellationToken);
        }
    }

    private async Task<string> GetJsonTextAsync(IReadOnlyList<ChatMessage> messages, ChatOptions options, CancellationToken cancellationToken)
    {
        var response = await _chat.GetResponseAsync(messages, options, cancellationToken);
        var text = response.Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Model returned empty response.");
        }

        text = text.Trim();
        _ = JsonAgentHelpers.ParseJsonOrThrow<JsonElement>(text);
        return text;
    }
}
