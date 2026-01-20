using AzureAiCouncil.Core.Models;
using AzureAiCouncil.Core.Services;
using AzureAiCouncil.Core.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureAiCouncil.Agents;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureAiCouncilAgents(this IServiceCollection services, IConfiguration configuration)
    {
        var aoai = new AzureOpenAiOptions
        {
            Endpoint = configuration["AzureOpenAI:Endpoint"],
            ApiKey = configuration["AzureOpenAI:ApiKey"],
            Deployment = configuration["AzureOpenAI:Deployment"],
        };

        services.AddSingleton<IOptions<AzureOpenAiOptions>>(Options.Create(aoai));

        services.AddSingleton<PromptRenderer>();

        services.AddSingleton<CostEstimatorTool>();
        services.AddSingleton<ArchitecturePatternTool>();
        services.AddSingleton<CouncilTools>();

        services.AddSingleton<ICouncilAgentFactory, CouncilAgentFactory>();

        services.AddSingleton<ISpeakerAgent>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<AzureOpenAiOptions>>().Value;
            return opts.IsConfigured
                ? new SpeakerAgent(sp.GetRequiredService<ICouncilAgentFactory>().CreateSpeakerAgent(), sp.GetRequiredService<PromptRenderer>(), sp.GetRequiredService<ILogger<SpeakerAgent>>())
                : new MockSpeakerAgent();
        });

        services.AddSingleton<IClerkAgent>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<AzureOpenAiOptions>>().Value;
            return opts.IsConfigured
                ? new ClerkAgent(sp.GetRequiredService<ICouncilAgentFactory>().CreateClerkAgent(), sp.GetRequiredService<PromptRenderer>(), sp.GetRequiredService<ILogger<ClerkAgent>>())
                : new MockClerkAgent();
        });

        services.AddSingleton<ICouncilorAgent>(sp => CreateCouncilor(sp, CouncilorKind.Vision));
        services.AddSingleton<ICouncilorAgent>(sp => CreateCouncilor(sp, CouncilorKind.Risk));
        services.AddSingleton<ICouncilorAgent>(sp => CreateCouncilor(sp, CouncilorKind.Architecture));
        services.AddSingleton<ICouncilorAgent>(sp => CreateCouncilor(sp, CouncilorKind.Security));
        services.AddSingleton<ICouncilorAgent>(sp => CreateCouncilor(sp, CouncilorKind.Finance));

        return services;
    }

    private static ICouncilorAgent CreateCouncilor(IServiceProvider sp, CouncilorKind kind)
    {
        var opts = sp.GetRequiredService<IOptions<AzureOpenAiOptions>>().Value;
        if (!opts.IsConfigured)
        {
            return new MockCouncilorAgent(kind);
        }

        return new CouncilorAgent(
            kind,
            sp.GetRequiredService<ICouncilAgentFactory>().CreateCouncilorAgent(kind),
            sp.GetRequiredService<CouncilTools>(),
            sp.GetRequiredService<PromptRenderer>(),
            sp.GetRequiredService<ILogger<CouncilorAgent>>());
    }
}
