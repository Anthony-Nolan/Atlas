using Atlas.ManualTesting.Common.Contexts;
using Atlas.ManualTesting.Common.Models.Entities;
using Atlas.MatchPrediction.Test.Validation.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas.MatchPrediction.Test.Validation.Data.Context
{
    public class MatchPredictionValidationContext : DbContext, IDonorExportData, ISearchData<ValidationSearchRequestRecord>
    {
        // ReSharper disable once SuggestBaseTypeForParameter
        public MatchPredictionValidationContext(DbContextOptions<MatchPredictionValidationContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SubjectInfo>().SetUpModel();

            modelBuilder.Entity<MatchPredictionRequest>().SetUpModel();
            modelBuilder.Entity<MatchPredictionResults>().SetUpModel();

            modelBuilder.Entity<TestDonorExportRecord>();
            modelBuilder.Entity<SearchSet>().SetUpModel();
            modelBuilder.Entity<ValidationSearchRequestRecord>().SetUpModel();
            modelBuilder.Entity<MatchedDonor>().SetUpModel();
            modelBuilder.Entity<LocusMatchCount>().SetUpModel();
            modelBuilder.Entity<MatchedDonorProbability>().SetUpModel();

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<SubjectInfo> SubjectInfo { get; set; }

        public DbSet<MatchPredictionRequest> MatchPredictionRequests { get; set; }
        public DbSet<MatchPredictionResults> MatchPredictionResults { get; set; }

        public DbSet<TestDonorExportRecord> TestDonorExportRecords { get; set; }
        public DbSet<SearchSet> SearchSets { get; set; }
        public DbSet<ValidationSearchRequestRecord> SearchRequests { get; set; }
        public DbSet<MatchedDonor> MatchedDonors { get; set; }
        public DbSet<LocusMatchCount> MatchCounts { get; set; }
        public DbSet<MatchedDonorProbability> MatchProbabilities { get; set; }
    }
}