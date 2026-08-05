using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Atlas.MatchPrediction.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Atlas.MatchPrediction.Data.Context;

public class MatchPredictionContext : DbContext
{
    internal const string Schema = "MatchPrediction";

    public MatchPredictionContext(DbContextOptions<MatchPredictionContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<HaplotypeFrequencySet>().SetUpModel();
        modelBuilder.Entity<HaplotypeFrequency>().SetUpModel();

        modelBuilder.Entity<ParallelMatchPredictionRun>()
            .Property(x => x.MatchingAlgorithmElapsedTime)
            .HasConversion<TimeSpanToTicksConverter>()
            .HasColumnType("bigint");

        modelBuilder.Entity<ParallelMatchPredictionRun>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<ParallelMatchPredictionRun>()
            .HasIndex(x => x.Status)
            .HasDatabaseName("IX_ParallelMatchPredictionRuns_Status_Running")
            .HasFilter("[Status] = 'Running'");

        modelBuilder.Entity<ParallelMatchPredictionBatch>()
            .Property(x => x.BatchStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasDefaultValue(ParallelMatchPredictionBatchStatus.Requested);

        // EF serialises the dictionary to/from JSON; the comparer gives it by-value change-tracking semantics.
        modelBuilder.Entity<ParallelMatchPredictionBatch>()
            .Property(x => x.DonorGenotypeCounts)
            .HasConversion(
                counts => JsonSerializer.Serialize(counts, (JsonSerializerOptions)null),
                json => JsonSerializer.Deserialize<Dictionary<int, int>>(json, (JsonSerializerOptions)null),
                new ValueComparer<Dictionary<int, int>>(
                    (left, right) => left == null ? right == null : right != null && left.Count == right.Count && !left.Except(right).Any(),
                    counts => counts == null ? 0 : counts.Aggregate(0, (hash, kvp) => System.HashCode.Combine(hash, kvp.Key, kvp.Value)),
                    counts => counts == null ? null : new Dictionary<int, int>(counts)))
            .HasColumnType("nvarchar(max)");

        base.OnModelCreating(modelBuilder);
    }

    public DbSet<HaplotypeFrequencySet> HaplotypeFrequencySets { get; set; }

    public DbSet<HaplotypeFrequency> HaplotypeFrequencies { get; set; }

    public DbSet<ParallelMatchPredictionRun> ParallelMatchPredictionRuns { get; set; }

    public DbSet<ParallelMatchPredictionBatch> ParallelMatchPredictionBatches { get; set; }
}