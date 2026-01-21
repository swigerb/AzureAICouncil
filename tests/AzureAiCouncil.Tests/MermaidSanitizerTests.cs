using AzureAiCouncil.Agents;

namespace AzureAiCouncil.Tests;

public sealed class MermaidSanitizerTests
{
    [Fact]
    public void Sanitize_converts_bracket_labels_to_quoted_labels_and_strips_trailing_semicolons()
    {
        var input = """
```mermaid
graph TD;
  User[Internal User]---AGW[Azure Application Gateway + WAF];
  AGW---AppSvc[Azure App Service (.NET, VNet Integrated)];
  subgraph SecureNetwork[VNet with Subnets and Private Endpoints];
    AGW; AppSvc;
  end;
```
""";

        var output = MermaidSanitizer.Sanitize(input);

        Assert.Contains("User[\"Internal User\"]---AGW[\"Azure Application Gateway + WAF\"]", output);
        Assert.Contains("AppSvc[\"Azure App Service (.NET, VNet Integrated)\"]", output);
        Assert.Contains("subgraph SecureNetwork[\"VNet with Subnets and Private Endpoints\"]", output);

        // Ensure we didn't keep semicolons at line ends.
        Assert.DoesNotContain("graph TD;\n", output);
        Assert.DoesNotContain("end;\n", output);
    }

    [Fact]
    public void Sanitize_removes_nested_single_quotes_and_dangling_terminal_quote()
    {
        var input = """
graph TD
  subgraph Net["'VNet with Private Endpoints'"]
    Firewall["'Azure Firewall'"]
    NSG["'Network Security Groups'"]
  end
  User["'User'"] --> FrontDoor["'Azure Front Door + WAF'"]
  FrontDoor --> AppService["'Azure App Service (.NET)'"]
  AppService --> Redis["'Azure Cache for Redis'"]
  AppService --> SQLDB["'Azure SQL Database / Cosmos DB'"]
  AppService --> KeyVault["'Azure Key Vault'"]
  AppService --> ServiceBus["'Azure Service Bus / Event Grid'"]
  Net --> AppService
  Monitor["'Azure Monitor + Defender for Cloud'"] --> Governance["'Security & Cost Telemetry'"]"
""";

        var output = MermaidSanitizer.Sanitize(input);

        Assert.DoesNotContain("\"]\"", output); // no dangling quote after bracket close
        Assert.Contains("subgraph Net[\"VNet with Private Endpoints\"]", output);
        Assert.Contains("Governance[\"Security & Cost Telemetry\"]", output);
        Assert.DoesNotContain("\"'", output); // inner single quotes removed
        Assert.DoesNotContain("'\"", output);
    }
}
