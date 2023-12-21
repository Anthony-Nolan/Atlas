using Atlas.ManualTesting.Common.Models.Entities;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification
{
    // ReSharper disable InconsistentNaming

    public class SearchRequestRecord : SearchRequestRecordBase
    {
        public int VerificationRun_Id { get; set; }
        public bool WasMatchPredictionRun { get; set; }
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
                .HasForeignKey(r => r.PatientId);

            modelBuilder.HasIndex(r => r.AtlasSearchIdentifier);
            modelBuilder.HasIndex(r => new { r.VerificationRun_Id, r.PatientId, r.SearchResultsRetrieved });
        }
    }
}
