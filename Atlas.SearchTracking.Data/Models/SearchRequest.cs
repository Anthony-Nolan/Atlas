using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.SearchTracking.Data.Models
{
    public class SearchRequest
    {
        public int Id { get; set; }

        [Required]
        public Guid SearchRequestId { get; set; }

        public bool IsRepeatSearch { get; set; }

        public Guid? OriginalSearchRequestId { get; set; }

        public DateTime? RepeatSearchCutOffDate { get; set; }

        [Required]
        public string RequestJson { get; set; }

        [Required]
        [MaxLength(10)]
        public string SearchCriteria { get; set; }

        [Required]
        [MaxLength(10)]
        public string DonorType { get; set; }

        [Required]
        public DateTime RequestTimeUTC { get; set; }

        public bool? MatchingAlgorithm_IsSuccessful { get; set; }

        public string? MatchingAlgorithm_FailureInfo_Json { get; set; }

        public byte? MatchingAlgorithm_TotalAttemptsNumber { get; set; }

        public int? MatchingAlgorithm_NumberOfMatching { get; set; }

        public int? MatchingAlgorithm_NumberOfResults { get; set; }

        public int? RepeatSearch_AddedResultCount { get; set; }

        public int? RepeatSearch_RemovedResultCount { get; set; }

        public int? RepeatSearch_UpdatedResultCount { get; set; }

        [MaxLength(10)]
        public string? MatchingAlgorithm_HlaNomenclatureVersion { get; set; }

        public bool? MatchingAlgorithm_ResultsSent { get; set; }

        public DateTime? MatchingAlgorithm_ResultsSentTimeUTC { get; set; }

        public bool? MatchPrediction_IsSuccessful { get; set; }

        public string? MatchPrediction_FailureInfo_Json { get; set; }

        public int? MatchPrediction_DonorsPerBatch { get; set; }

        public int? MatchPrediction_TotalNumberOfBatches { get; set; }

        public bool? ResultsSent { get; set; }

        public DateTime? ResultsSentTimeUTC { get; set; }

        public SearchRequestMatchPredictionTiming? SearchRequestMatchPredictionTiming { get; set; }

        public ICollection<SearchRequestMatchingAlgorithmAttemptTiming>? SearchRequestMatchingAlgorithmAttemptTimings { get; set; }
    }

    internal static class SearchRequestModelBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<SearchRequest> modelBuilder)
        {
            modelBuilder.HasIndex(d => d.SearchRequestId)
                .HasDatabaseName("IX_SearchRequestId")
                .IsUnique();
        }
    }
}