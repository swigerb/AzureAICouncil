namespace AzureAiCouncil.Agents;

public static class PromptCatalog
{
    public static string PromptsRoot => Path.Combine(AppContext.BaseDirectory, "Prompts");

    public static string Speaker => Path.Combine(PromptsRoot, "Speaker.prompty");
    public static string Chair => Path.Combine(PromptsRoot, "Chair.prompty");

    public static string Vision => Path.Combine(PromptsRoot, "VisionCouncilor.prompty");
    public static string Risk => Path.Combine(PromptsRoot, "RiskCouncilor.prompty");
    public static string Architecture => Path.Combine(PromptsRoot, "ArchitectureCouncilor.prompty");
    public static string Security => Path.Combine(PromptsRoot, "SecurityCouncilor.prompty");
    public static string Finance => Path.Combine(PromptsRoot, "FinanceCouncilor.prompty");

    public static string Clerk => Path.Combine(PromptsRoot, "Clerk.prompty");
}
