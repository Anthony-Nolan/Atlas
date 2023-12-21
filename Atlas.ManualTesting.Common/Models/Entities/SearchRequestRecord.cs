using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.Common.Sql.BulkInsert;

namespace Atlas.ManualTesting.Common.Models.Entities
{
    // ReSharper disable InconsistentNaming

    public class SearchRequestRecord : IBulkInsertModel
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DonorMismatchCount { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(50)")]
        public string AtlasSearchIdentifier { get; set; }

        public bool SearchResultsRetrieved { get; set; }
        public bool? WasSuccessful { get; set; }
        public int? MatchedDonorCount { get; set; }
    }
}
