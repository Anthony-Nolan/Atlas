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
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            modelBuilder.Entity<HaplotypeFrequencySets>()
                .HasIndex(d => new { d.Ethnicity, d.Registry })
                .HasName("IX_RegistryAndEthnicity")
                .IsUnique()
                .HasFilter("[Active] = 'True'");

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<HaplotypeFrequencySets> HaplotypeFrequencySets { get; set; }
        public DbSet<HaplotypeInfo> HaplotypeInfo { get; set; }
    }
}
