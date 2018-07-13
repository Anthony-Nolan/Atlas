namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary.ScoringLookup
{
    /// <summary>
    /// Lookup result with data required to score HLA pairings.
    /// </summary>
    public interface IHlaScoringLookupResult<out TScoringInfo> : IHlaLookupResult, IPreCalculatedHlaInfo<TScoringInfo>
        where TScoringInfo : IPreCalculatedScoringInfo
    {
    }
}
