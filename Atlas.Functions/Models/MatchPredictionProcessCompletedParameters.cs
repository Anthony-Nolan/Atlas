using System;
using Atlas.SearchTracking.Common.Models;

namespace Atlas.Functions.Models;

/// <summary>
/// Parameters passed to the <see cref="DurableFunctions.Search.Activity.SearchActivityFunctions.SendMatchPredictionProcessCompleted"/> activity function.
/// </summary>
public record MatchPredictionProcessCompletedParameters
{
    public Guid SearchIdentifier { get; init; }
    public Guid? OriginalSearchIdentifier { get; init; }

    /// <summary>
    /// Explicit success flag. Always set by the caller so completion tracking does not have to infer it
    /// from the (nullable) <see cref="FailureInfo"/>.
    /// </summary>
    public bool IsSuccessful { get; init; }

    /// <summary>
    /// Populated only when <see cref="IsSuccessful"/> is <c>false</c>.
    /// </summary>
    public MatchPredictionFailureInfo FailureInfo { get; init; }

    public int? DonorsPerBatch { get; init; }
    public int? TotalNumberOfBatches { get; init; }
}
