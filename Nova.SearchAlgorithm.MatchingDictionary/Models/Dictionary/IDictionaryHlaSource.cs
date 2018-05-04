using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary
{
    public interface IDictionaryHlaSource
    {
        IEnumerable<string> MatchingPGroups { get; }
        IEnumerable<Serology> MatchingSerologies { get; }
    }

    public interface IDictionarySerologySource : IDictionaryHlaSource
    {
        Serology MatchedOnSerology { get; }
    }

    public interface IDictionaryAlleleSource : IDictionaryHlaSource
    {
        Allele MatchedOnAllele { get; }
    }
}
