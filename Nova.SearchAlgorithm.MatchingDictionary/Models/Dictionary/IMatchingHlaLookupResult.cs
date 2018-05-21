using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary
{
    /// <summary>
    /// This interface defines
    /// the properties that make up a matching HLA lookup result.
    /// It serves to separate the info required for matching
    /// from that required for scoring matches.
    /// </summary>
    public interface IMatchingHlaLookupResult : IMatchingPGroups
    {
        MatchLocus MatchLocus { get; }
        string LookupName { get; }
    }
}
