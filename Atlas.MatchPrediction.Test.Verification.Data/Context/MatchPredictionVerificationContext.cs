﻿using Atlas.ManualTesting.Common.Contexts;
using Atlas.ManualTesting.Common.Models.Entities;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Microsoft.EntityFrameworkCore;

namespace Atlas.MatchPrediction.Test.Verification.Data.Context
{
    public class MatchPredictionVerificationContext : 
        DbContext,
        IDonorExportData,
        ISearchData<VerificationSearchRequestRecord>
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
            modelBuilder.Entity<TestDonorExportRecord>();
            modelBuilder.Entity<VerificationRun>().SetUpModel();
            modelBuilder.Entity<VerificationSearchRequestRecord>().SetUpModel();
            modelBuilder.Entity<MatchedDonor>().SetUpModel();
            modelBuilder.Entity<LocusMatchDetails>().SetUpModel();
            modelBuilder.Entity<MatchedDonorProbability>().SetUpModel();
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<NormalisedPool> NormalisedPool { get; set; }
        public DbSet<NormalisedHaplotypeFrequency> NormalisedHaplotypeFrequencies { get; set; }
        public DbSet<TestHarness> TestHarnesses { get; set; }
        public DbSet<Simulant> Simulants { get; set; }
        public DbSet<ExpandedMac> ExpandedMacs { get; set; }
        public DbSet<MaskingRecord> MaskingRecords { get; set; }
        public DbSet<VerificationRun> VerificationRuns { get; set; }
        public DbSet<TestDonorExportRecord> TestDonorExportRecords { get; set; }
        public DbSet<VerificationSearchRequestRecord> SearchRequests { get; set; }
        public DbSet<MatchedDonor> MatchedDonors { get; set; }
        public DbSet<LocusMatchDetails> LocusMatchDetails { get; set; }
        public DbSet<MatchedDonorProbability> MatchProbabilities { get; set; }
    }
}