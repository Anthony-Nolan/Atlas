using Atlas.ManualTesting.Common.Contexts;
using Atlas.ManualTesting.Common.Models.Entities;
using Atlas.MatchPrediction.Test.Validation.Data.Models;
using Atlas.MatchPrediction.Test.Validation.Data.Models.Homework;
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
            modelBuilder.Entity<LocusMatchDetails>().SetUpModel();
            modelBuilder.Entity<MatchedDonorProbability>().SetUpModel();

            modelBuilder.Entity<HomeworkSet>().SetUpModel();
            modelBuilder.Entity<PatientDonorPair>().SetUpModel();
            modelBuilder.Entity<ImputationSummary>().SetUpModel();
            modelBuilder.Entity<MatchingGenotypes>().SetUpModel();

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<SubjectInfo> SubjectInfo { get; set; }

        public DbSet<MatchPredictionRequest> MatchPredictionRequests { get; set; }
        public DbSet<MatchPredictionResults> MatchPredictionResults { get; set; }

        public DbSet<TestDonorExportRecord> TestDonorExportRecords { get; set; }
        public DbSet<SearchSet> SearchSets { get; set; }
        public DbSet<ValidationSearchRequestRecord> SearchRequests { get; set; }
        public DbSet<MatchedDonor> MatchedDonors { get; set; }
        public DbSet<LocusMatchDetails> LocusMatchDetails { get; set; }
        public DbSet<MatchedDonorProbability> MatchProbabilities { get; set; }

        public DbSet<HomeworkSet> HomeworkSets { get; set; }
        public DbSet<PatientDonorPair> PatientDonorPairs { get; set; }
        public DbSet<ImputationSummary> ImputationSummaries { get; set; }
        public DbSet<MatchingGenotypes> MatchingGenotypes { get; set; }
    }
}