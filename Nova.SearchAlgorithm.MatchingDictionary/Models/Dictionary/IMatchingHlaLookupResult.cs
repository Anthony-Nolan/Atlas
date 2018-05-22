using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary
{
    /// <summary>
    /// Properties that make up a matching HLA lookup result.
    /// Allows separation of the info required for matching
    /// from that required for scoring matches.
    /// </summary>
    public interface IMatchingHlaLookupResult : IMatchingPGroups
    {
        MatchLocus MatchLocus { get; }
        string LookupName { get; }
    }
}
