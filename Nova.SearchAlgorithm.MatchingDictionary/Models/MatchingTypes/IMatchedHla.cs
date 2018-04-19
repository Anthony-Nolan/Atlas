using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes
{
    public interface IMatchedHla
    {
        HlaType HlaType { get; }
        HlaType TypeUsedInMatching { get; }
    }
}
