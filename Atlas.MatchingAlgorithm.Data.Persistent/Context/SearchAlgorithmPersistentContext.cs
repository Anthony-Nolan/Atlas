using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Models.ScoringWeightings;
using Microsoft.EntityFrameworkCore;
using static EnumStringValues.EnumExtensions;

namespace Atlas.MatchingAlgorithm.Data.Persistent.Context
{
    public class SearchAlgorithmPersistentContext : DbContext
    {
        internal const string Schema = "MatchingAlgorithmPersistent";
        
        // ReSharper disable once SuggestBaseTypeForParameter
        public SearchAlgorithmPersistentContext(DbContextOptions<SearchAlgorithmPersistentContext> options) : base(options)
        {
        }

        private const int DefaultWeight = 0;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Schema);
            
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            modelBuilder.Entity<GradeWeighting>().HasIndex(w => w.Name).IsUnique();
            modelBuilder.Entity<GradeWeighting>().Property(w => w.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<GradeWeighting>().HasData(SeededMatchGradeWeights());

            modelBuilder.Entity<ConfidenceWeighting>().HasIndex(w => w.Name).IsUnique();
            modelBuilder.Entity<ConfidenceWeighting>().Property(w => w.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<ConfidenceWeighting>().HasData(SeededConfidenceWeights());

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<GradeWeighting> GradeWeightings { get; set; }
        public DbSet<ConfidenceWeighting> ConfidenceWeightings { get; set; }
        public DbSet<DataRefreshRecord> DataRefreshRecords { get; set; }

        private static IEnumerable<GradeWeighting> SeededMatchGradeWeights()
        {
            var grades = EnumerateValues<MatchGrade>();
            return grades.Select((g, i) => new GradeWeighting {Name = g.ToString(), Weight = DefaultWeight, Id = i + 1});
        }

        private static IEnumerable<ConfidenceWeighting> SeededConfidenceWeights()
        {
            var confidences = EnumerateValues<MatchConfidence>();
            return confidences.Select((c, i) => new ConfidenceWeighting {Name = c.ToString(), Weight = DefaultWeight, Id = i + 1});
        }
    }
}