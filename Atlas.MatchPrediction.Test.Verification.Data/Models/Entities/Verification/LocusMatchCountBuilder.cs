using Atlas.ManualTesting.Common.Models.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification
{
    internal static class LocusMatchCountBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<LocusMatchCount> modelBuilder)
        {
            modelBuilder
                .HasOne<MatchedDonor>()
                .WithMany()
                .HasForeignKey(r => r.MatchedDonor_Id);

            modelBuilder.HasIndex(r => new { r.MatchedDonor_Id, r.Locus, r.MatchCount });
        }
    }
}