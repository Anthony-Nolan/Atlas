using System.ComponentModel.DataAnnotations;

namespace Atlas.MatchPrediction.Worker.Settings;

public class MatchPredictionWorkerSettings
{
    [Required(AllowEmptyStrings = false)]
    public required string RequestsSubscription { get; set; }

    [Range(1, int.MaxValue)]
    public required int BatchSize { get; set; }
}