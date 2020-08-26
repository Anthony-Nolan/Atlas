using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models
{
    public class TestHarness : ParentEntityBase
    {
        public int NormalisedPool_Id { get; set; }

        /// <summary>
        /// Did test harness complete generation successfully?
        /// </summary>
        public bool WasCompleted { get; set; }
    }
    
    internal static class TestHarnessBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<TestHarness> modelBuilder)
        {
            modelBuilder
                .Property(t => t.CreatedDateTime)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder
                .HasOne<NormalisedPool>()
                .WithMany()
                .HasForeignKey(t => t.NormalisedPool_Id);
        }
    }
}
