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
            modelBuilder.Entity<NormalisedHaplotypeFrequency>().SetUpModel();
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<NormalisedHaplotypeFrequency> NormalisedHaplotypeFrequencyPool { get; set; }
    }
}