using System.Text.Json;
using System.Text.Json.Serialization;
using AzureAiCouncil.Core.Models;
using Microsoft.Extensions.AI;

namespace AzureAiCouncil.Agents;

public static class JsonAgentHelpers
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    static JsonAgentHelpers()
    {
        // LLM and UI payloads frequently represent enums as strings (e.g. "Vision").
        // Ensure our shared JSON options can parse/emit string enums consistently.
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public static string ToJson<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    public static T ParseJsonOrThrow<T>(string json)
    {
        var result = JsonSerializer.Deserialize<T>(json, JsonOptions);
        return result ?? throw new InvalidOperationException("Model returned null JSON payload.");
    }

    public static ChatOptions CreateCouncilJsonOptions(float temperature, IList<AITool>? tools = null)
    {
        var options = new ChatOptions
        {
            Temperature = temperature,
            ResponseFormat = ChatResponseFormat.Json,
        };

        if (tools is { Count: > 0 })
        {
            options.Tools = tools;
        }

        // Hint to OpenAI-family endpoints to be strict where supported.
        if (options.AdditionalProperties is not null)
        {
            options.AdditionalProperties["strict"] = true;
        }

        return options;
    }

    public static string CouncilorDisplay(CouncilorKind kind) => kind switch
    {
        CouncilorKind.Vision => "Vision",
        CouncilorKind.Risk => "Risk",
        CouncilorKind.Architecture => "Architecture",
        CouncilorKind.Security => "Security",
        CouncilorKind.Finance => "Finance",
        _ => kind.ToString(),
    };
}
