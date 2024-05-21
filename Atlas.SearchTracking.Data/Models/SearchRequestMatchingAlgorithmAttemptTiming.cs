using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.SearchTracking.Data.Models
{
    public class SearchRequestMatchingAlgorithmAttemptTiming
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("SearchRequest")]
        public int SearchRequestId { get; set; }

        public SearchRequest SearchRequest { get; set; }

        [Required]
        public byte AttemptNumber { get; set; }

        [Required]
        public DateTime InitiationTimeUtc { get; set; }

        [Required]
        public DateTime StartTimeUtc { get; set; }

        public DateTime? AlgorithmCore_Matching_StartTimeUtc { get; set; }

        public DateTime? AlgorithmCore_Matching_EndTimeUtc { get; set; }

        public DateTime? AlgorithmCore_Scoring_StartTimeUtc { get; set; }

        public DateTime? AlgorithmCore_Scoring_EndTimeUtc { get; set; }

        public DateTime? PersistingResults_StartTimeUtc { get; set; }

        public DateTime? PersistingResults_EndTimeUtc { get; set; }

        public DateTime? CompletionTimeUtc { get; set; }
    }

    internal static class SearchRequestMatchingAlgorithmAttemptTimingModelBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<SearchRequestMatchingAlgorithmAttemptTiming> modelBuilder)
        {
            modelBuilder.HasIndex(d => new { d.SearchRequestId, d.AttemptNumber })
                .HasDatabaseName("IX_SearchRequestId_And_AttemptNumber")
                .IsUnique();
        }
    }
}
