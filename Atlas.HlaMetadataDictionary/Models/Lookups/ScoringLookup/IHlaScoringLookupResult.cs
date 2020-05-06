namespace Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup
{
    /// <summary>
    /// Lookup result with data required to score HLA pairings.
    /// </summary>
    public interface IHlaScoringLookupResult : IHlaLookupResult
    {
        LookupNameCategory LookupNameCategory { get; }
        IHlaScoringInfo HlaScoringInfo { get; }
    }
}
