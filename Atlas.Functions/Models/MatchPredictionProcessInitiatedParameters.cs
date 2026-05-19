using System;

namespace Atlas.Functions.Models;

/// <summary>
/// Parameters passed to the <see cref="DurableFunctions.Search.Activity.SearchActivityFunctions.SendMatchPredictionProcessInitiated"/> activity function.
/// </summary>
public record MatchPredictionProcessInitiatedParameters
{
    public Guid SearchIdentifier { get; init; }
    public Guid? OriginalSearchIdentifier { get; init; }
    public DateTime InitiationTimeUtc { get; init; }
    public bool IsParallelMatchPrediction { get; init; }
}

