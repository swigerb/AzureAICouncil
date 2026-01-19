using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using System.ClientModel;

namespace AzureAiCouncil.Agents;

public interface ICouncilChatClientFactory
{
    IChatClient Create();
}

public sealed class CouncilChatClientFactory : ICouncilChatClientFactory
{
    private readonly AzureOpenAiOptions _options;

    public CouncilChatClientFactory(IOptions<AzureOpenAiOptions> options)
    {
        _options = options.Value;
    }

    public IChatClient Create()
    {
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
        var chatClient = client.GetChatClient(_options.Deployment!).AsIChatClient();

        // Enable function invocation pipeline so tools can be auto-invoked when provided.
        return chatClient.AsBuilder().UseFunctionInvocation().Build();
    }
}
