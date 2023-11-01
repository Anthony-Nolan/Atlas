using Atlas.ManualTesting.Common.Contexts;
using Atlas.ManualTesting.Common.Models.Entities;
using Atlas.MatchPrediction.Test.Validation.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas.MatchPrediction.Test.Validation.Data.Context
{
    public class MatchPredictionValidationContext : DbContext, IDonorExportData
    {
        // ReSharper disable once SuggestBaseTypeForParameter
        public MatchPredictionValidationContext(DbContextOptions<MatchPredictionValidationContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SubjectInfo>().SetUpModel();
            modelBuilder.Entity<TestDonorExportRecord>();
            modelBuilder.Entity<MatchPredictionRequest>().SetUpModel();
            modelBuilder.Entity<MatchPredictionResults>().SetUpModel();
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<SubjectInfo> SubjectInfo { get; set; }
        public DbSet<TestDonorExportRecord> TestDonorExportRecords { get; set; }
        public DbSet<MatchPredictionRequest> MatchPredictionRequests { get; set; }
        public DbSet<MatchPredictionResults> MatchPredictionResults { get; set; }
    }
}