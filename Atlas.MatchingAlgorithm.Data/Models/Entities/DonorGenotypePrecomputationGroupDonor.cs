#nullable enable

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchingAlgorithm.Data.Models.Entities;

/// <summary>
/// The group → donor contract: the donors that one <see cref="DonorGenotypePrecomputationGroup"/> stands for. Four rows per
/// donor, one per <see cref="AllowedLociKey"/>.
///
/// <para>
/// The clustered key is (<see cref="GroupId"/>, <see cref="DonorId"/>), so a worker reads the donors of its batch with one
/// range seek. A staging row: the stage truncates this table when the run completes.
/// </para>
/// </summary>
public class DonorGenotypePrecomputationGroupDonor
{
    public int GroupId { get; set; }

    /// <summary>The Atlas donor id (<c>Donors.DonorId</c>), as in <see cref="DonorSubjectGenotypeSet.DonorId"/>.</summary>
    public int DonorId { get; set; }
}

public static class DonorGenotypePrecomputationGroupDonorModelBuilder
{
    public static void SetUpModel(this EntityTypeBuilder<DonorGenotypePrecomputationGroupDonor> modelBuilder)
    {
        // Clustered on the group first, so the donors of a batch's contiguous groups are one range of the table.
        modelBuilder.HasKey(x => new { x.GroupId, x.DonorId });
    }
}
