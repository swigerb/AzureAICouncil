namespace AzureAiCouncil.Core.Models;

public enum CouncilorKind
{
    Vision,
    Risk,
    Architecture,
    Security,
    Finance,
}

public enum Tier
{
    Low,
    Medium,
    High,
    Unknown,
}

public enum RiskTolerance
{
    Low,
    Medium,
    High,
    Unspecified,
}

public enum GoNoGo
{
    Go,
    No_Go,
    Mixed,
}

public enum Priority
{
    Low,
    Medium,
    High,
}
