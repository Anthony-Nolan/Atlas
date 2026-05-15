using System.ComponentModel.DataAnnotations;

namespace Atlas.MatchPrediction.ExternalInterface.Settings
{
    public class MatchPredictionRequestsSettings
    {
        [Required(AllowEmptyStrings = false)]
        public string RequestsTopic { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string ResultsTopic { get; set; }
        public int MaxParallelism { get; set; }
    }
}