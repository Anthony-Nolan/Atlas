using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary
{
    public interface IDictionarySource<out THlaType> : IMatchingPGroups, IMatchingSerologies where THlaType : HlaType
    {
        THlaType TypeForDictionary { get; }
    }
}
