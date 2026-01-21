using System.Diagnostics;
using System.Text.Json;
using AzureAiCouncil.Core.Models;
using AzureAiCouncil.Core.Services;
using AzureAiCouncil.Core.Telemetry;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AzureAiCouncil.Agents;

public sealed class ClerkAgent : IClerkAgent
{
    private readonly ChatClientAgent _agent;
    private readonly PromptRenderer _prompts;
    private readonly ILogger<ClerkAgent> _logger;

    public ClerkAgent(ChatClientAgent agent, PromptRenderer prompts, ILogger<ClerkAgent> logger)
    {
        _agent = agent;
        _prompts = prompts;
        _logger = logger;
    }

    public async Task<CouncilResolution> ProduceResolutionAsync(
        string deliberationId,
        Motion motion,
        IReadOnlyList<CouncilorStatement> transcript,
        CancellationToken cancellationToken)
    {
        using var activity = CouncilTelemetry.ActivitySource.StartActivity("council.clerk", ActivityKind.Internal);
        activity?.SetTag(CouncilTelemetry.Tags.DeliberationId, deliberationId);
        activity?.SetTag(CouncilTelemetry.Tags.MotionId, motion.MotionId);
        activity?.SetTag("council.transcript_count", transcript.Count);

        var resolutionSchema = File.ReadAllText(PromptCatalog.ResolutionJsonSchema);

        var prompt = _prompts.RenderFromFile(PromptCatalog.Clerk, new Dictionary<string, string>
        {
            ["ResolutionJsonSchema"] = resolutionSchema,
        });

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
            return await GetJsonTextAsync(messages, options, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid JSON from clerk; retrying once");
            var repair = new List<ChatMessage>(messages)
            {
                new(ChatRole.User, "Your last response was invalid or did not match the schema (missing or malformed required fields). Output ONLY a single valid CouncilResolution JSON object matching the schema, with non-empty deliberationId, summary, and recommendation. No markdown, no prose.")
            };
            return await GetJsonTextAsync(repair, options, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<string> GetJsonTextAsync(IReadOnlyList<ChatMessage> messages, ChatOptions options, CancellationToken cancellationToken)
    {
        var response = await _agent.RunAsync(messages, thread: null, options: new ChatClientAgentRunOptions(options), cancellationToken).ConfigureAwait(false);
        var text = response.Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Model returned empty response.");
        }

        // Small normalization for accidental code fences.
        text = text.Trim();
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            text = text.Trim('`');
            var idx = text.IndexOf('{');
            if (idx >= 0)
            {
                text = text.Substring(idx);
            }
        }

        // Validate parse early.
        _ = JsonAgentHelpers.ParseJsonOrThrow<JsonElement>(text);

        // Ensure the shape matches CouncilResolution before deserializing into a type with required properties.
        return ExtractAndValidateCouncilResolutionJson(text);
    }

    private static string ExtractAndValidateCouncilResolutionJson(string json)
    {
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Expected a single JSON object.");
        }

        var candidate = doc.RootElement;
        var unwrapped = false;

        // Common wrapper keys used by models.
        foreach (var wrapperKey in new[] { "resolution", "councilResolution", "council_resolution" })
        {
            if (TryGetPropertyIgnoreCase(candidate, wrapperKey, out var wrapped)
                && wrapped.ValueKind == JsonValueKind.Object)
            {
                candidate = wrapped;
                unwrapped = true;
                break;
            }
        }

        var required = new[] { "deliberationId", "summary", "recommendation" };
        var missing = new List<string>();

        foreach (var key in required)
        {
            if (!TryGetPropertyIgnoreCase(candidate, key, out var value)
                || value.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(value.GetString()))
            {
                missing.Add(key);
            }
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException($"CouncilResolution JSON was missing required properties: {string.Join(", ", missing)}");
        }

        // If we unwrapped, return the unwrapped object JSON; otherwise keep the original.
        if (unwrapped)
        {
            return JsonSerializer.Serialize(candidate, JsonAgentHelpers.JsonOptions);
        }

        return json;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement obj, string name, out JsonElement value)
    {
        foreach (var prop in obj.EnumerateObject())
        {
            if (prop.NameEquals(name) || string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
