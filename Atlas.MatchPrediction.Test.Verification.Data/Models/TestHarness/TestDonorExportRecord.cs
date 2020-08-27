using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models.TestHarness
{
    // ReSharper disable InconsistentNaming

    public class TestDonorExportRecord
    {
        public int Id { get; set; }
        public int TestHarness_Id { get; set; }

        /// <summary>
        /// Start datetime of export attempt.
        /// </summary>
        public DateTimeOffset Started { get; set; }

        /// <summary>
        /// Datetime test donors were exported to donor import store.
        /// </summary>
        public DateTimeOffset? Exported { get; set; }

        /// <summary>
        /// Datetime data refresh was marked as completed.
        /// </summary>
        public DateTimeOffset? DataRefreshCompleted { get; set; }

        public int? DataRefreshRecordId { get; set; }

        /// <summary>
        /// Was matching algorithm database successfully refreshed with test donors?
        /// </summary>
        public bool? WasDataRefreshSuccessful { get; set; }
    }

    internal static class TestDonorExportRecordBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<TestDonorExportRecord> modelBuilder)
        {
            modelBuilder
                .Property(t => t.Started)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder
                .HasOne<Models.TestHarness.TestHarness>()
                .WithMany()
                .HasForeignKey(t => t.TestHarness_Id);
        }
    }
}
