using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.HlaInfo;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary.Lookups
{
    internal class AlleleLookup : MatchingDictionaryLookup
    {
        public AlleleLookup(IMatchingDictionaryRepository dictionaryRepository) : base(dictionaryRepository)
        {
        }

        public override Task<MatchingDictionaryEntry> PerformLookupAsync(MatchLocus matchLocus, string lookupName)
        {
            var allele = new AlleleTyping(PermittedLocusNames.GetMolecularLocusNameFromMatch(matchLocus), lookupName);
            return GetMatchingDictionaryEntry(matchLocus, allele.TwoFieldName, TypingMethod.Molecular);
        }
    }
}
