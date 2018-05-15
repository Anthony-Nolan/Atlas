using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary
{
    public interface IMatchingHlaLookupResult : IMatchingPGroups
    {
        MatchLocus MatchLocus { get; }
        string LookupName { get; }
    }
}
