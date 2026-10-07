#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchingAlgorithm.Data.Models.Entities;

/// <summary>
/// One imputation of a run: the donors that share a typing (only the loci of <see cref="AllowedLociKey"/>), a registry and
/// an ethnicity. They need the same value, so the worker computes it once, from <see cref="RepresentativeDonorId"/>, and
/// gives it to all of them.
///
/// <para>
/// A staging row. The stage truncates this table when the run completes.
/// </para>
/// </summary>
public class DonorGenotypePrecomputationGroup
{
    /// <summary>
    /// Given by the build, not an identity: the groups of one batch have contiguous ids, so a worker reads the groups of
    /// its batch with one range seek.
    /// </summary>
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public int RunId { get; set; }

    public AllowedLociKey AllowedLociKey { get; set; }

    /// <summary>The lowest <c>Donors.DonorId</c> of the group. The worker reads the typing and the codes of this donor.</summary>
    public int RepresentativeDonorId { get; set; }

    public int DonorCount { get; set; }

    /// <summary>
    /// The value of the group, after a worker stored it. Null before that, and while the group has failed. A batch that
    /// runs again skips the groups that have a value.
    /// </summary>
    public int? SubjectGenotypeSetValueId { get; set; }

    /// <summary>Why the worker could not compute the value. Null unless the group failed.</summary>
    [MaxLength(512)]
    public string? FailureMessage { get; set; }
}

public static class DonorGenotypePrecomputationGroupModelBuilder
{
    public static void SetUpModel(this EntityTypeBuilder<DonorGenotypePrecomputationGroup> modelBuilder)
    {
        modelBuilder
            .Property(x => x.AllowedLociKey)
            .HasConversion<string>()
            .HasMaxLength(16);
    }
}
