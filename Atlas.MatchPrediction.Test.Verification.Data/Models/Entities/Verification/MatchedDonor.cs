using Atlas.ManualTesting.Common.Models.Entities;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification
{
    // ReSharper disable InconsistentNaming

    public class MatchedDonor : MatchedDonorBase
    {
        public int MatchedDonorSimulant_Id { get; set; }
    }

    internal static class MatchedDonorBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<MatchedDonor> modelBuilder)
        {
            modelBuilder
                .HasOne<SearchRequestRecord>()
                .WithMany()
                .HasForeignKey(r => r.SearchRequestRecord_Id)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder
                .HasOne<Simulant>()
                .WithMany()
                .HasForeignKey(r => r.MatchedDonorSimulant_Id);

            modelBuilder
                .HasIndex(r => new { r.SearchRequestRecord_Id, r.MatchedDonorSimulant_Id, r.TotalMatchCount });
        }
    }
}