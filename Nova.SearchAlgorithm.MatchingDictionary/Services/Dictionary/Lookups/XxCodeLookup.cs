using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary.Lookups
{
    public class XxCodeLookup : MatchingDictionaryLookup
    {
        public XxCodeLookup(IMatchedHlaRepository dictionaryRepository) : base(dictionaryRepository)
        {
        }

        public override MatchingDictionaryEntry PerformLookup(string matchLocus, string lookupName)
        {
            var firstField = lookupName.Split(':')[0];
            return GetDictionaryEntry(matchLocus, firstField, TypingMethod.Molecular);
        }
    }
}
