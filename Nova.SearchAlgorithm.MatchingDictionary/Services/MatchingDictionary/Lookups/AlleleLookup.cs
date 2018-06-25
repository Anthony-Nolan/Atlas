using Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary.Lookups
{
    internal class AlleleLookup : MatchingDictionaryLookup
    {
        public AlleleLookup(IMatchingDictionaryRepository dictionaryRepository) : base(dictionaryRepository)
        {
        }

        public override async Task<MatchingDictionaryEntry> PerformLookupAsync(MatchLocus matchLocus, string lookupName)
        {
            if(!TryGetEntryByLookupName(matchLocus, lookupName, out var entry))
            {
                entry = await GetEntryByTwoFieldVariantOfLookupNameIfExists(matchLocus, lookupName);
            }

            return entry;
        }

        private bool TryGetEntryByLookupName(MatchLocus matchLocus, string lookupName, out MatchingDictionaryEntry entry)
        {
            return TryGetMatchingDictionaryEntry(matchLocus, lookupName, TypingMethod.Molecular, out entry);
        }

        private async Task<MatchingDictionaryEntry> GetEntryByTwoFieldVariantOfLookupNameIfExists(MatchLocus matchLocus, string lookupName)
        {
            var molecularLocus = PermittedLocusNames.GetMolecularLocusNameFromMatchIfExists(matchLocus);
            var alleleTypingFromLookupName = new AlleleTyping(molecularLocus, lookupName);
            return await GetMatchingDictionaryEntryIfExists(matchLocus, alleleTypingFromLookupName.TwoFieldName, TypingMethod.Molecular);
        }
    }
}