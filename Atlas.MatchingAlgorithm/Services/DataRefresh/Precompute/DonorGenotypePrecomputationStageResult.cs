#nullable enable

using System.Diagnostics.CodeAnalysis;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;

/// <summary>How the donor genotype precomputation stage ended.</summary>
public sealed record DonorGenotypePrecomputationStageResult
{
    private DonorGenotypePrecomputationStageResult(DonorGenotypePrecomputationFailureReport? failureReport)
    {
        FailureReport = failureReport;
    }

    /// <summary>The stage is complete, and the data refresh continues.</summary>
    public static DonorGenotypePrecomputationStageResult Complete { get; } = new((DonorGenotypePrecomputationFailureReport?)null);

    /// <summary>
    /// More donors failed than the threshold allows. The stage is not complete. The run and its staging data stay, and the
    /// data refresh pauses for a manual decision.
    /// </summary>
    public static DonorGenotypePrecomputationStageResult AwaitingManualDecision(DonorGenotypePrecomputationFailureReport failureReport) =>
        new(failureReport);

    /// <summary>Null when the stage is complete.</summary>
    public DonorGenotypePrecomputationFailureReport? FailureReport { get; }

    [MemberNotNullWhen(true, nameof(FailureReport))]
    public bool IsAwaitingManualDecision => FailureReport != null;
}

/// <summary>The failures of a donor genotype precomputation run that is above the threshold.</summary>
/// <param name="MaxFailedDonorFraction">The threshold that the run is above.</param>
/// <param name="ManualRetryCount">How many manual retries the run had before this report.</param>
public sealed record DonorGenotypePrecomputationFailureReport(
    int RunId,
    DonorGenotypePrecomputationFailureSummary FailureSummary,
    double MaxFailedDonorFraction,
    int ManualRetryCount);
