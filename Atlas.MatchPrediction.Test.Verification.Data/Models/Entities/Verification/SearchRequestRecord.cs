using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification
{
    // ReSharper disable InconsistentNaming

    public class SearchRequestRecord : IModel
    {
        public int Id { get; set; }
        public int VerificationRun_Id { get; set; }
        public int PatientSimulant_Id { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(50)")]
        public string AtlasSearchIdentifier { get; set; }
        
        public bool SearchResultsRetrieved { get; set; }
        public bool? WasSuccessful { get; set; }
        public int? MatchedDonorCount { get; set; }

        public double? MatchingAlgorithmTimeInMs { get; set; }
        public double? MatchPredictionTimeInMs { get; set; }
        public double? OverallSearchTimeInMs { get; set; }
    }

    internal static class SearchRequestRecordBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<SearchRequestRecord> modelBuilder)
        {
            modelBuilder
                .HasOne<VerificationRun>()
                .WithMany()
                .HasForeignKey(r => r.VerificationRun_Id)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder
                .HasOne<Simulant>()
                .WithMany()
                .HasForeignKey(r => r.PatientSimulant_Id);
        }
    }
}
