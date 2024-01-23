using Atlas.ManualTesting.Common.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness
{
    public class TestHarness : ParentEntityBase
    {
        public int NormalisedPool_Id { get; set; }

        /// <summary>
        /// Did test harness complete generation successfully?
        /// </summary>
        public bool WasCompleted { get; set; }

        /// <summary>
        /// Record of when the test harness was last exported (`NULL` if it has not yet been exported)
        /// </summary>
        public int? ExportRecord_Id { get; set; }
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
                .HasForeignKey(x => x.NormalisedPool_Id);

            modelBuilder
                .HasOne<TestDonorExportRecord>()
                .WithOne()
                .HasForeignKey<TestHarness>(x => x.ExportRecord_Id);
        }
    }
}
