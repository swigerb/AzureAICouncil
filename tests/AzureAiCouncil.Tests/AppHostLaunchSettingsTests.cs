using System.Text.Json;
using Xunit;

namespace AzureAiCouncil.Tests;

public sealed class AppHostLaunchSettingsTests
{
    [Fact]
    public void AppHost_launchSettings_requires_allow_unsecured_transport_when_otlp_is_http()
    {
        var repoRoot = FindRepoRoot();
        var launchSettingsPath = Path.Combine(repoRoot, "src", "AzureAiCouncil.AppHost", "Properties", "launchSettings.json");

        Assert.True(File.Exists(launchSettingsPath), $"Missing launchSettings.json at: {launchSettingsPath}");

        using var doc = JsonDocument.Parse(File.ReadAllText(launchSettingsPath));

        Assert.True(doc.RootElement.TryGetProperty("profiles", out var profiles), "launchSettings.json missing 'profiles'.");

        foreach (var profile in profiles.EnumerateObject())
        {
            if (!profile.Value.TryGetProperty("environmentVariables", out var env))
            {
                continue;
            }

            if (!env.TryGetProperty("ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL", out var otlpEndpointElement))
            {
                continue;
            }

            var otlpEndpoint = otlpEndpointElement.GetString();
            if (string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                continue;
            }

            if (otlpEndpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                Assert.True(
                    env.TryGetProperty("ASPIRE_ALLOW_UNSECURED_TRANSPORT", out var allowUnsecured)
                    && string.Equals(allowUnsecured.GetString(), "true", StringComparison.OrdinalIgnoreCase),
                    $"Profile '{profile.Name}' configures ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL='{otlpEndpoint}' but does not set ASPIRE_ALLOW_UNSECURED_TRANSPORT=true.");
            }
            else if (!otlpEndpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Fail($"Profile '{profile.Name}' has unexpected ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL='{otlpEndpoint}'.");
            }
        }
    }

    private static string FindRepoRoot()
    {
        // Walk up from the test output folder until we find the repo marker file.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "AzureAiCouncil.slnx");
            if (File.Exists(candidate))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repo root containing AzureAiCouncil.slnx.");
    }
}
