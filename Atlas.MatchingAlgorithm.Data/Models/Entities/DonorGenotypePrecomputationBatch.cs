using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchingAlgorithm.Data.Models.Entities;

/// <summary>
/// One unit of work for a precompute worker: a contiguous range of <see cref="DonorGenotypePrecomputationGroup"/> ids,
/// sent as one message on the requests topic.
///
/// <para>
/// The message holds only the batch's ids. The worker reads everything else from this row and from the group tables,
/// so the message stays small, and the row is the one source of truth about the batch.
/// </para>
/// </summary>
public class DonorGenotypePrecomputationBatch
{
    public int Id { get; set; }

    public int RunId { get; set; }

    /// <summary>The 0-based position of the batch in its run. Unique with <see cref="RunId"/>.</summary>
    public int BatchNumber { get; set; }

    /// <summary>The first group of the range, inclusive.</summary>
    public int FirstGroupId { get; set; }

    /// <summary>The last group of the range, inclusive.</summary>
    public int LastGroupId { get; set; }

    public int GroupCount { get; set; }

    /// <summary>
    /// The donor rows that the batch writes: the sum of <see cref="DonorGenotypePrecomputationGroup.DonorCount"/> over its
    /// groups.
    /// </summary>
    public int DonorAssignmentCount { get; set; }

    public DonorGenotypePrecomputationBatchStatus Status { get; set; }

    /// <summary>
    /// How many times the batch went back to <see cref="DonorGenotypePrecomputationBatchStatus.Pending"/> after a failure or
    /// an abandonment.
    /// </summary>
    public int RetryCount { get; set; }

    [MaxLength(512)]
    public string FailureMessage { get; set; }

    public string FailureException { get; set; }

    /// <summary>
    /// The groups of the batch whose value the worker could not compute. Their donors get no rows, but the batch can
    /// still succeed.
    /// </summary>
    public int FailedGroupCount { get; set; }

    /// <summary>The worker that holds the batch while it is <see cref="DonorGenotypePrecomputationBatchStatus.InProgress"/>.</summary>
    public Guid? LeaseOwner { get; set; }

    /// <summary>
    /// When the claim of <see cref="LeaseOwner"/> lapses. After that, a sweep can mark the batch abandoned, and another
    /// worker can claim it.
    /// </summary>
    public DateTime? LeaseExpiresUtc { get; set; }

    /// <summary>When the latest message for the batch was published.</summary>
    public DateTime? DispatchedUtc { get; set; }

    public DateTime StatusDateUtc { get; set; }

    /// <summary>When the batch reached a terminal status.</summary>
    public DateTime? CompletedUtc { get; set; }
}

public static class DonorGenotypePrecomputationBatchModelBuilder
{
    public static void SetUpModel(this EntityTypeBuilder<DonorGenotypePrecomputationBatch> modelBuilder)
    {
        // Stored as the member name, like AllowedLociKey. The longest name is 21 characters.
        modelBuilder
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        modelBuilder
            .HasIndex(x => new { x.RunId, x.BatchNumber })
            .IsUnique();

        // The stage counts the batches per status while it waits, and each sweep finds the batches in one status.
        modelBuilder
            .HasIndex(x => new { x.RunId, x.Status });
    }
}
