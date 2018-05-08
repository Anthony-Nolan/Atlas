using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary
{
    public interface IDictionarySerologySource : IMatchingPGroups, IMatchingSerologies
    {
        Serology MatchedOnSerology { get; }
    }
}
