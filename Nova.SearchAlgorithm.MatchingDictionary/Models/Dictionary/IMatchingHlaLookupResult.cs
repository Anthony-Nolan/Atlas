using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;

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
