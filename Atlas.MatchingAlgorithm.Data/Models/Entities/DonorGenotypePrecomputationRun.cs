#nullable enable

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchingAlgorithm.Data.Models.Entities;

/// <summary>
/// One run of the donor genotype precomputation stage of the data refresh: at most one per data refresh record.
///
/// <para>
/// <b>The run row is the stage's resume state.</b> A stage that starts again reads it to know what is left: to build, to
/// dispatch and wait, or nothing. It lives in the transient database that the refresh fills, next to the
/// <see cref="SubjectGenotypeSetValue"/> rows it produces, so the clean-up at the start of every refresh removes it with
/// them.
/// </para>
/// </summary>
public class DonorGenotypePrecomputationRun
{
    public int Id { get; set; }

    /// <summary>
    /// The <c>DataRefreshRecord.Id</c> of the persistent database. Unique. A worker acts on a batch only when its message
    /// names this record, so a message from an earlier refresh cannot write to this one.
    /// </summary>
    public int DataRefreshRecordId { get; set; }

    /// <summary>The matching algorithm HLA nomenclature version of the refresh. The run imputes every value under it.</summary>
    [MaxLength(32)]
    public required string HlaNomenclatureVersion { get; set; }

    public DonorGenotypePrecomputationRunStatus Status { get; set; }

    /// <summary>Fixed when the run is created, so a build that starts again cuts the same batches.</summary>
    public int GroupsPerBatch { get; set; }

    /// <summary>Null until the build is done.</summary>
    public int? TotalGroupCount { get; set; }

    /// <summary>Null until the build is done.</summary>
    public int? TotalBatchCount { get; set; }

    /// <summary>Null until the build is done. Four per donor: one per <see cref="AllowedLociKey"/>.</summary>
    public int? TotalDonorAssignmentCount { get; set; }

    /// <summary>
    /// Null until the build is done. The donors that the build found in <c>Donors</c>: the base of the failed-donor
    /// fraction that the stage compares with its threshold.
    /// </summary>
    public int? TotalDonorCount { get; set; }

    /// <summary>
    /// How many times a manual retry sent the failed batches of the run back to
    /// <see cref="DonorGenotypePrecomputationBatchStatus.Pending"/>.
    /// </summary>
    public int ManualRetryCount { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime StatusDateUtc { get; set; }

    public DateTime? CompletedUtc { get; set; }
}

public static class DonorGenotypePrecomputationRunModelBuilder
{
    public static void SetUpModel(this EntityTypeBuilder<DonorGenotypePrecomputationRun> modelBuilder)
    {
        // Stored as the member name, like AllowedLociKey. The longest name is 21 characters.
        modelBuilder
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        // One run per refresh: a stage that starts again finds its run, and does not create a second one.
        modelBuilder
            .HasIndex(x => x.DataRefreshRecordId)
            .IsUnique();
    }
}
