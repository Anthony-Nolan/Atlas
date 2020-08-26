using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas.MatchPrediction.Test.Verification.Data.Context
{
    public class MatchPredictionVerificationContext : DbContext
    {
        // ReSharper disable once SuggestBaseTypeForParameter
        public MatchPredictionVerificationContext(DbContextOptions<MatchPredictionVerificationContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NormalisedPool>().SetUpModel();
            modelBuilder.Entity<NormalisedHaplotypeFrequency>().SetUpModel();
            modelBuilder.Entity<TestHarness>().SetUpModel();
            modelBuilder.Entity<Simulant>().SetUpModel();
            modelBuilder.Entity<ExpandedMac>().SetUpModel();
            modelBuilder.Entity<MaskingRecord>().SetUpModel();
            modelBuilder.Entity<TestDonorExportRecord>().SetUpModel();
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<NormalisedPool> NormalisedPool { get; set; }
        public DbSet<NormalisedHaplotypeFrequency> NormalisedHaplotypeFrequencies { get; set; }
        public DbSet<TestHarness> TestHarnesses { get; set; }
        public DbSet<Simulant> Simulants { get; set; }
        public DbSet<ExpandedMac> ExpandedMacs { get; set; }
        public DbSet<MaskingRecord> MaskingRecords { get; set; }
        public DbSet<TestDonorExportRecord> TestDonorExportRecords { get; set; }
    }
}