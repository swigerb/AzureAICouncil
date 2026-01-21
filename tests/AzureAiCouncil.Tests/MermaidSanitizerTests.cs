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
}
