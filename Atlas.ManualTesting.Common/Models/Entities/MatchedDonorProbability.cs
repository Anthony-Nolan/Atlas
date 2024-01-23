using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.ManualTesting.Common.Models.Entities
{
    // ReSharper disable InconsistentNaming

    public class MatchedDonorProbability : MatchProbability
    {
        public int MatchedDonor_Id { get; set; }
    }

    public static class MatchedDonorProbabilityBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<MatchedDonorProbability> modelBuilder)
        {
            modelBuilder
                .HasOne<MatchedDonor>()
                .WithMany()
                .HasForeignKey(t => t.MatchedDonor_Id);

            modelBuilder
                .HasIndex(x => new { x.MatchedDonor_Id, x.Locus, x.MismatchCount });
        }
    }
}