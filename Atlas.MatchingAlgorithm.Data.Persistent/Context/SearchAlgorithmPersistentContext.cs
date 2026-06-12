using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Common.Results;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Models.ScoringWeightings;
using Microsoft.EntityFrameworkCore;
using static EnumStringValues.EnumExtensions;

namespace Atlas.MatchingAlgorithm.Data.Persistent.Context
{
    public class SearchAlgorithmPersistentContext : DbContext
    {
        internal const string Schema = "MatchingAlgorithmPersistent";
        
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
            List<GradeWeighting> grades = [
                        new ()
                        {
                            Id = 1,
                            Name = "Mismatch",
                            Weight = 0
                        },
                        new ()
                        {
                            Id = 2,
                            Name = "Broad",
                            Weight = 0
                        },
                        new ()
                        {
                            Id = 3,
                            Name = "Split",
                            Weight = 0
                        },
                        new ()
                        {
                            Id = 4,
                            Name = "Associated",
                            Weight = 0
                        },
                        new ()
                        {
                            Id = 5,
                            Name = "NullMismatch",
                            Weight = 0
                        },
                        new ()
                        {
                            Id = 6,
                            Name = "NullPartial",
                            Weight = 0
                        },
                        new ()
                        {
                            Id = 7,
                            Name = "NullCDna",
                            Weight = 0
                        },
                        new ()
                        {
                            Id = 8,
                            Name = "NullGDna",
                            Weight = 0
                        },
                        new ()
                        {
                            Id = 9,
                            Name = "PGroup",
                            Weight = 0
                        },
                        new ()
                        {
                            Id = 10,
                            Name = "GGroup",
                            Weight = 0
                        },
                        new ()
                        {
                            Id = 11,
                            Name = "Protein",
                            Weight = 0
                        },
                        new ()
                        {
                            Id = 12,
                            Name = "CDna",
                            Weight = 0
                        },
                        new ()
                        {
                            Id = 13,
                            Name = "GDna",
                            Weight = 0
                        },
                        new ()
                        {
                            Id = 14,
                            Name = "Unknown",
                            Weight = 0
                        },
                        new ()
                        {
                            Id = 15,
                            Name = "ExpressingVsNull",
                        }];
            return grades;
        }

        private static IEnumerable<ConfidenceWeighting> SeededConfidenceWeights()
        {
            var confidences = EnumerateValues<MatchConfidence>();
            return confidences.Select((c, i) => new ConfidenceWeighting {Name = c.ToString(), Weight = DefaultWeight, Id = i + 1});
        }
    }
}