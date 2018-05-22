using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Data;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary.Lookups
{
    internal class AlleleLookup : MatchingDictionaryLookup
    {
        public AlleleLookup(IMatchingDictionaryRepository dictionaryRepository) : base(dictionaryRepository)
        {
        }

        public override Task<MatchingDictionaryEntry> PerformLookupAsync(MatchLocus matchLocus, string lookupName)
        {
            var allele = new AlleleTyping(LocusNames.GetMolecularLocusNameFromMatch(matchLocus), lookupName);
            return GetDictionaryEntry(matchLocus, allele.TwoFieldName, TypingMethod.Molecular);
        }
    }
}
