#nullable enable

using System.Diagnostics.CodeAnalysis;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh;

/// <summary>How a run of <see cref="IDataRefreshRunner.RefreshData"/> ended, when it did not throw.</summary>
public sealed record DataRefreshRunResult
{
    private DataRefreshRunResult(string? hlaNomenclatureVersion, DonorGenotypePrecomputationFailureReport? pauseReport)
    {
        HlaNomenclatureVersion = hlaNomenclatureVersion;
        PauseReport = pauseReport;
    }

    /// <summary>All stages are complete. The record can be closed as successful.</summary>
    /// <param name="hlaNomenclatureVersion">The HLA nomenclature version of the new data.</param>
    public static DataRefreshRunResult Completed(string hlaNomenclatureVersion) => new(hlaNomenclatureVersion, null);

    /// <summary>
    /// The refresh paused for a manual decision. The dormant database was scaled down, and the paused alert was sent.
    /// The record stays open.
    /// </summary>
    public static DataRefreshRunResult Paused(DonorGenotypePrecomputationFailureReport pauseReport) => new(null, pauseReport);

    /// <summary>The HLA nomenclature version of the new data. Null when the refresh paused.</summary>
    public string? HlaNomenclatureVersion { get; }

    /// <summary>Why the refresh paused. Null when it completed.</summary>
    public DonorGenotypePrecomputationFailureReport? PauseReport { get; }

    [MemberNotNullWhen(true, nameof(PauseReport))]
    [MemberNotNullWhen(false, nameof(HlaNomenclatureVersion))]
    public bool IsPaused => PauseReport != null;
}
