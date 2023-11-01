using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.Common.Sql.BulkInsert;

namespace Atlas.ManualTesting.Common.Models.Entities
{
    // ReSharper disable InconsistentNaming

    public class MatchedDonorBase : IBulkInsertModel
    {
        public int Id { get; set; }
        public int SearchRequestRecord_Id { get; set; }
        public int TotalMatchCount { get; set; }
        public int TypedLociCount { get; set; }
        public bool? WasPatientRepresented { get; set; }
        public bool? WasDonorRepresented { get; set; }

        /// <summary>
        /// Serialised copy of the <see cref="Client.Models.Search.Results.Matching.MatchingAlgorithmResult"/>.
        /// </summary>
        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string MatchingResult { get; set; }

        /// <summary>
        /// Serialised copy of the <see cref="Client.Models.Search.Results.MatchPrediction.MatchProbabilityResponse"/>.
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? MatchPredictionResult { get; set; }
    }
}
