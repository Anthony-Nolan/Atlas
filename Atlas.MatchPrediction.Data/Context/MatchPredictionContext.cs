using System.Collections.Generic;
using System.Text.Json;
using Atlas.MatchPrediction.Data.Models;
using Microsoft.EntityFrameworkCore;
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

        // EF serialises the dictionary to/from JSON. No value comparer: rows are written via ExecuteUpdate (never a
        // tracked in-place update), so change tracking never needs to snapshot or compare this property.
        modelBuilder.Entity<ParallelMatchPredictionBatch>()
            .Property(x => x.DonorGenotypeCounts)
            .HasConversion(
                counts => JsonSerializer.Serialize(counts, (JsonSerializerOptions)null),
                json => JsonSerializer.Deserialize<Dictionary<int, int>>(json, (JsonSerializerOptions)null))
            .HasColumnType("nvarchar(max)");

        base.OnModelCreating(modelBuilder);
    }

    public DbSet<HaplotypeFrequencySet> HaplotypeFrequencySets { get; set; }

    public DbSet<HaplotypeFrequency> HaplotypeFrequencies { get; set; }

    public DbSet<ParallelMatchPredictionRun> ParallelMatchPredictionRuns { get; set; }

    public DbSet<ParallelMatchPredictionBatch> ParallelMatchPredictionBatches { get; set; }
}