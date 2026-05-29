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

        base.OnModelCreating(modelBuilder);
    }

    public DbSet<HaplotypeFrequencySet> HaplotypeFrequencySets { get; set; }

    public DbSet<HaplotypeFrequency> HaplotypeFrequencies { get; set; }

    public DbSet<ParallelMatchPredictionRun> ParallelMatchPredictionRuns { get; set; }

    public DbSet<ParallelMatchPredictionBatch> ParallelMatchPredictionBatches { get; set; }
}