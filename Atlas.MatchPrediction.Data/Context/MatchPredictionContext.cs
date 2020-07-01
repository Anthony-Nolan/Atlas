﻿using Atlas.MatchPrediction.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas.MatchPrediction.Data.Context
{
    public class MatchPredictionContext : DbContext
    {
        // ReSharper disable once SuggestBaseTypeForParameter
        public MatchPredictionContext(DbContextOptions<MatchPredictionContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HaplotypeFrequencySet>().SetUpModel();
            modelBuilder.Entity<HaplotypeFrequency>().SetUpModel();
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<HaplotypeFrequencySet> HaplotypeFrequencySets { get; set; }
        public DbSet<HaplotypeFrequency> HaplotypeFrequencies { get; set; }
    }
}
