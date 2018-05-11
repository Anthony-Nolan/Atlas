using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary
{
    public interface IMatchingHlaLookupResult : IMatchingPGroups
    {
        string MatchLocus { get; }
        string LookupName { get; }
    }
}
