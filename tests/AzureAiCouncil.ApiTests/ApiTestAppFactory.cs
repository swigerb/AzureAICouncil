using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using AzureAiCouncil.Agents;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AzureAiCouncil.ApiTests;

public sealed class ApiTestAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Ensure tests run in mock-agent mode by default.
            // The API uses the presence of AzureOpenAI settings to decide whether to call Azure OpenAI.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureOpenAI:Endpoint"] = "",
                ["AzureOpenAI:ApiKey"] = "",
                ["AzureOpenAI:Deployment"] = "",
            });
        });
        builder.ConfigureTestServices(services =>
        {
            // Agent registration captures options before the test configuration callback.
            services.AddSingleton<IOptions<AzureOpenAiOptions>>(Options.Create(new AzureOpenAiOptions()));
        });
    }
}
