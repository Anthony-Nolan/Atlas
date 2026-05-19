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
    /// Null when the process completed without error.
    /// </summary>
    public MatchPredictionFailureInfo FailureInfo { get; init; }

    public int? DonorsPerBatch { get; init; }
    public int? TotalNumberOfBatches { get; init; }
}

