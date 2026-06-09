using System.ComponentModel.DataAnnotations;

namespace Atlas.MatchPrediction.Worker.Settings;

public class MatchPredictionWorkerSettings
{
    [Required(AllowEmptyStrings = false)]
    public required string RequestsSubscription { get; set; }

    /// <summary>Maximum number of messages the processor will handle concurrently.</summary>
    [Range(1, int.MaxValue)]
    public required int MaxConcurrentCalls { get; set; }

    /// <summary>Number of messages to fetch in advance of processing. Set to 0 to disable prefetch.</summary>
    [Range(0, int.MaxValue)]
    public int PrefetchCount { get; set; }

    /// <summary>Maximum total duration for which the processor will auto-renew a message lock while it is being processed.</summary>
    [Range(1, int.MaxValue)]
    public int MaxAutoLockRenewalMinutes { get; set; } = 5;
}