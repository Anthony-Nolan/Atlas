using System.ComponentModel.DataAnnotations;

namespace Atlas.MatchPrediction.ExternalInterface.Settings;

public class MatchPredictionRequestsSettings
{
    [Required(AllowEmptyStrings = false)]
    public string RequestsTopic { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string ResultsTopic { get; set; }

    /// <summary>Topic to which the ACA Worker publishes batch results for the parallel MPA path.</summary>
    [Required(AllowEmptyStrings = false)]
    public string ParallelResultsTopic { get; set; }

    public int MaxParallelism { get; set; }
}