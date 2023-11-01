using Atlas.ManualTesting.Common.Models.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification
{
    internal static class MatchProbabilityBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<MatchProbability> modelBuilder)
        {
            modelBuilder
                .HasOne<MatchedDonor>()
                .WithMany()
                .HasForeignKey(r => r.MatchedDonor_Id);
        }
    }
}
