using Nova.HLAService.Client.Models;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary.ScoringLookup
{
    /// <summary>
    /// Lookup result with data required to score HLA pairings.
    /// </summary>
    public interface IHlaScoringLookupResult : IHlaLookupResult, IPreCalculatedHlaInfo<IPreCalculatedScoringInfo>
    {
        HlaTypingCategory HlaTypingCategory { get; }
    }
}
