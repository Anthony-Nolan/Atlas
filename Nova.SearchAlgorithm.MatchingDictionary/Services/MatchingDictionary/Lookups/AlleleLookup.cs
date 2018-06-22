using System;
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
            try
            {
                return await GetEntryByLookupNameIfExists(matchLocus, lookupName);
            }
            catch (InvalidHlaException)
            {
                return await GetEntryByTwoFieldVariantOfLookupNameIfExists(matchLocus, lookupName);
            }          
        }

        private Task<MatchingDictionaryEntry> GetEntryByLookupNameIfExists(MatchLocus matchLocus, string lookupName)
        {
            return GetAlleleEntryIfExists(matchLocus, lookupName);
        }

        private Task<MatchingDictionaryEntry> GetEntryByTwoFieldVariantOfLookupNameIfExists(MatchLocus matchLocus, string lookupName)
        {
            var molecularLocus = PermittedLocusNames.GetMolecularLocusNameFromMatchIfExists(matchLocus);
            var alleleTypingFromLookupName = new AlleleTyping(molecularLocus, lookupName);
            return GetAlleleEntryIfExists(matchLocus, alleleTypingFromLookupName.TwoFieldName);
        }

        private Task<MatchingDictionaryEntry> GetAlleleEntryIfExists(MatchLocus matchLocus, string alleleName)
        {
            return GetMatchingDictionaryEntryIfExists(matchLocus, alleleName, TypingMethod.Molecular);
        }
    }
}
