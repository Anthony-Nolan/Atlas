using Microsoft.EntityFrameworkCore;
using System.Linq;
using Atlas.MatchPrediction.Data.Models;

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
            modelBuilder.Entity<HaplotypeFrequencySet>()
                .HasIndex(d => new { Ethnicity = d.EthnicityCode, Registry = d.RegistryCode })
                .HasName("IX_RegistryAndEthnicity")
                .IsUnique()
                .HasFilter("[Active] = 'True'");

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<HaplotypeFrequencySet> HaplotypeFrequencySets { get; set; }
        public DbSet<HaplotypeFrequency> HaplotypeFrequencies { get; set; }
    }
}
