using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary.Lookups
{
    internal class AlleleLookup : MatchingDictionaryLookup
    {
        public AlleleLookup(IMatchingDictionaryRepository dictionaryRepository) : base(dictionaryRepository)
        {
        }

        public override Task<MatchingDictionaryEntry> PerformLookupAsync(MatchLocus matchLocus, string lookupName)
        {
            var locus = PermittedLocusNames.GetMolecularLocusNameFromMatchIfExists(matchLocus);
            var allele = new AlleleTyping(locus, lookupName);
            return GetMatchingDictionaryEntry(matchLocus, allele.TwoFieldName, TypingMethod.Molecular);
        }
    }
}
