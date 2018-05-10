using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary.Lookups
{
    public class NmdpCodeLookup : MatchingDictionaryLookup
    {
        public NmdpCodeLookup(IMatchedHlaRepository dictionaryRepository) : base(dictionaryRepository)
        {
        }

        public override MatchingDictionaryEntry PerformLookup(string matchLocus, string lookupName)
        {
            // todo: call Nova.Hla expansion endpoint & perform lookups for each allele
            return GetDictionaryEntry(matchLocus, lookupName, TypingMethod.Molecular);
        }
    }
}
