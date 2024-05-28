using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.SearchTracking.Data.Models
{
    public class SearchRequestMatchPredictionTiming
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("SearchRequest")]
        public int SearchRequestId { get; set; }

        public SearchRequest SearchRequest { get; set; }

        [Required]
        public DateTime InitiationTimeUtc { get; set; }

        [Required]
        public DateTime StartTimeUtc { get; set; }

        public DateTime? PrepareBatches_StartTimeUtc { get; set; }

        public DateTime? PrepareBatches_EndTimeUtc { get; set; }

        public DateTime? AlgorithmCore_RunningBatches_StartTimeUtc { get; set; }

        public DateTime? AlgorithmCore_RunningBatches_EndTimeUtc { get; set; }

        public DateTime? PersistingResults_StartTimeUtc { get; set; }

        public DateTime? PersistingResults_EndTimeUtc { get; set; }

        public DateTime? CompletionTimeUtc { get; set; }
    }
}
