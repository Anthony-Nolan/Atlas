using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
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
            if(!GetEntryByLookupNameIfExists(matchLocus, lookupName, out var entry))
            {
                entry = await GetEntryByTwoFieldVariantOfLookupNameIfExists(matchLocus, lookupName);
            }

            return entry ?? throw new InvalidHlaException(matchLocus, lookupName);
        }

        private bool GetEntryByLookupNameIfExists(MatchLocus matchLocus, string lookupName, out MatchingDictionaryEntry entry)
        {
            entry = GetMatchingDictionaryEntryIfExists(matchLocus, lookupName).Result;
            return entry != null;
        }

        private async Task<MatchingDictionaryEntry> GetEntryByTwoFieldVariantOfLookupNameIfExists(MatchLocus matchLocus, string lookupName)
        {
            var molecularLocus = PermittedLocusNames.GetMolecularLocusNameFromMatchIfExists(matchLocus);
            var alleleTypingFromLookupName = new AlleleTyping(molecularLocus, lookupName);

            return await GetMatchingDictionaryEntryIfExists(matchLocus, alleleTypingFromLookupName.TwoFieldName);
        }

        protected override async Task<MatchingDictionaryEntry> GetMatchingDictionaryEntryIfExists(
            MatchLocus matchLocus, string lookupName, TypingMethod typingMethod = TypingMethod.Molecular)
        {
            return await DictionaryRepository.GetMatchingDictionaryEntryIfExists(matchLocus, lookupName, typingMethod);
        }
    }
}