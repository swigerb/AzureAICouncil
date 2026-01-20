using System.Diagnostics;

namespace AzureAiCouncil.Core.Telemetry;

/// <summary>
/// OpenTelemetry instrumentation for the Azure AI Council.
/// </summary>
public static class CouncilTelemetry
{
    /// <summary>
    /// The activity source name for council traces.
    /// </summary>
    public const string ActivitySourceName = "AzureAiCouncil";

    /// <summary>
    /// The shared activity source for council operations.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    /// <summary>
    /// Common tag names for council activities.
    /// </summary>
    public static class Tags
    {
        /// <summary>Tag for deliberation ID.</summary>
        public const string DeliberationId = "council.deliberation_id";

        /// <summary>Tag for motion ID.</summary>
        public const string MotionId = "council.motion_id";

        /// <summary>Tag for round number.</summary>
        public const string Round = "council.round";

        /// <summary>Tag for councilor name.</summary>
        public const string Councilor = "council.councilor";
    }
}
