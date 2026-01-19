using System.ComponentModel;
using AzureAiCouncil.Core.Tools;
using Microsoft.Extensions.AI;

namespace AzureAiCouncil.Agents;

public sealed class CouncilTools
{
    public CouncilTools(CostEstimatorTool costEstimator, ArchitecturePatternTool patterns)
    {
        Tools =
        [
            AIFunctionFactory.Create(
                ([Description("Short scenario description for cost estimation")] string scenario,
                 [Description("Budget constraint, if any")] string? budget,
                 [Description("Timeline constraint, if any")] string? timeline) => costEstimator.Estimate(scenario, budget, timeline),
                name: costEstimator.Name,
                description: costEstimator.Description),

            AIFunctionFactory.Create(
                ([Description("Short scenario description for pattern suggestion")] string scenario) => patterns.Suggest(scenario),
                name: patterns.Name,
                description: patterns.Description),
        ];
    }

    public IList<AITool> Tools { get; }
}
