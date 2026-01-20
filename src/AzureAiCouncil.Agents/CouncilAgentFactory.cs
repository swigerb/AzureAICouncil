using AzureAiCouncil.Core.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace AzureAiCouncil.Agents;

public interface ICouncilAgentFactory
{
    ChatClientAgent CreateSpeakerAgent();

    ChatClientAgent CreateClerkAgent();

    ChatClientAgent CreateCouncilorAgent(CouncilorKind kind);
}

public sealed class CouncilAgentFactory : ICouncilAgentFactory
{
    private readonly AzureOpenAiOptions _options;
    private readonly CouncilTools _tools;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceProvider _services;

    private readonly ChatClient _chatClient;

    public CouncilAgentFactory(
        IOptions<AzureOpenAiOptions> options,
        CouncilTools tools,
        ILoggerFactory loggerFactory,
        IServiceProvider services)
    {
        _options = options.Value;
        _tools = tools;
        _loggerFactory = loggerFactory;
        _services = services;

        if (!_options.IsConfigured)
        {
            throw new InvalidOperationException("AzureOpenAI is not configured. Set AzureOpenAI:Endpoint, AzureOpenAI:ApiKey, AzureOpenAI:Deployment.");
        }

        var endpoint = new Uri(_options.Endpoint!, UriKind.Absolute);
        if (!endpoint.AbsolutePath.EndsWith("/openai/v1", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = new Uri(endpoint, "/openai/v1");
        }

        var client = new OpenAIClient(new ApiKeyCredential(_options.ApiKey!), new OpenAIClientOptions { Endpoint = endpoint });
        _chatClient = client.GetChatClient(_options.Deployment!);
    }

    public ChatClientAgent CreateSpeakerAgent() => Create(
        name: "speaker",
        description: "Normalizes the user request into a Motion.",
        instructions: "You normalize user requests into a Motion object. Output only valid JSON when asked.",
        tools: []);

    public ChatClientAgent CreateClerkAgent() => Create(
        name: "clerk",
        description: "Synthesizes council deliberation into a final resolution.",
        instructions: "You synthesize multi-agent deliberations into a single resolution. Output only valid JSON when asked.",
        tools: []);

    public ChatClientAgent CreateCouncilorAgent(CouncilorKind kind) => Create(
        name: $"councilor-{kind.ToString().ToLowerInvariant()}",
        description: $"Provides {kind} perspective for Azure AI Council deliberation.",
        instructions: "You are a deliberative councilor in Azure AI Council. Output only valid JSON when asked.",
        tools: _tools.Tools);

    private ChatClientAgent Create(string name, string description, string instructions, IList<AITool> tools)
    {
        // Uses the Microsoft Agent Framework OpenAI extensions: ChatClient.CreateAIAgent(...)
        // The clientFactory wraps the underlying IChatClient with function-invocation support so tools can be auto-invoked.
        return _chatClient.CreateAIAgent(
            instructions: instructions,
            name: name,
            description: description,
            tools: tools,
            clientFactory: chat => chat.AsBuilder().UseFunctionInvocation().Build(),
            loggerFactory: _loggerFactory,
            services: _services);
    }
}
