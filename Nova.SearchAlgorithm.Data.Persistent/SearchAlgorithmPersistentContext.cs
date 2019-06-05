using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Data.Persistent.Models;
using Nova.SearchAlgorithm.Data.Persistent.Models.ScoringWeightings;

namespace Nova.SearchAlgorithm.Data.Persistent
{
    public interface ISearchAlgorithmPersistentContext : IDisposable
    {
        DbSet<GradeWeighting> GradeWeightings { get; set; }
        DbSet<ConfidenceWeighting> ConfidenceWeightings { get; set; }
    }

    public class SearchAlgorithmPersistentContext : DbContext, ISearchAlgorithmPersistentContext
    {
        public SearchAlgorithmPersistentContext(DbContextOptions<SearchAlgorithmPersistentContext> options) : base(options)
        {
        }

        private const int DefaultWeight = 0;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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
            var grades = Enum.GetValues(typeof(MatchGrade)).Cast<MatchGrade>();
            return grades.Select((g, i) => new GradeWeighting {Name = g.ToString(), Weight = DefaultWeight, Id = i + 1});
        }

        private static IEnumerable<ConfidenceWeighting> SeededConfidenceWeights()
        {
            var confidences = Enum.GetValues(typeof(MatchConfidence)).Cast<MatchGrade>();
            return confidences.Select((c, i) => new ConfidenceWeighting {Name = c.ToString(), Weight = DefaultWeight, Id = i + 1});
        }
    }
}