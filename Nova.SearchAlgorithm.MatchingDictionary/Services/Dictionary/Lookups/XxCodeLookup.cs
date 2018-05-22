using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary.Lookups
{
    internal class XxCodeLookup : MatchingDictionaryLookup
    {
        public XxCodeLookup(IMatchedHlaRepository dictionaryRepository) : base(dictionaryRepository)
        {
        }

        public override Task<MatchingDictionaryEntry> PerformLookupAsync(MatchLocus matchLocus, string lookupName)
        {
            var firstField = lookupName.Split(':')[0];
            return GetDictionaryEntry(matchLocus, firstField, TypingMethod.Molecular);
        }
    }
}
