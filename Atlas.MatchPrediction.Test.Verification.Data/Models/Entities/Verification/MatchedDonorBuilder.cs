using Atlas.ManualTesting.Common.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification
{
    internal static class MatchedDonorBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<MatchedDonor> modelBuilder)
        {
            modelBuilder
                .HasOne<VerificationSearchRequestRecord>()
                .WithMany()
                .HasForeignKey(r => r.SearchRequestRecord_Id)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder
                .HasIndex(r => new { r.SearchRequestRecord_Id, r.DonorCode, r.TotalMatchCount });
        }
    }
}