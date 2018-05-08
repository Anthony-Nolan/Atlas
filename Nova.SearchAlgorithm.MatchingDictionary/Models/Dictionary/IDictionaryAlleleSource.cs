using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary
{
    public interface IDictionaryAlleleSource : IMatchingPGroups, IMatchingSerologies
    {
        Allele MatchedOnAllele { get; }
    }
}
