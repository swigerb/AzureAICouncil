using System.Diagnostics;

namespace AzureAiCouncil.Core.Telemetry;

public static class CouncilTelemetry
{
    public const string ActivitySourceName = "AzureAiCouncil";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public static class Tags
    {
        public const string DeliberationId = "council.deliberation_id";
        public const string MotionId = "council.motion_id";
        public const string Round = "council.round";
        public const string Councilor = "council.councilor";
    }
}
