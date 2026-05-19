using System;

namespace Atlas.Functions.Models;

/// <summary>
/// Identifies a match prediction tracking event by its search and (optional) original search identifiers.
/// Used when calling the batch processing started/ended activity functions.
/// </summary>
public record MatchPredictionSearchIdentifiers
{
    public Guid SearchIdentifier { get; init; }
    public Guid? OriginalSearchIdentifier { get; init; }
}

