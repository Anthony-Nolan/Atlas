using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Data;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary
{
    public interface IDictionaryLookupService
    {
        Task<MatchingDictionaryEntry> GetMatchedHla(string matchLocus, string hlaName);
    }
    public class DictionaryLookupService : IDictionaryLookupService
    {
        private readonly IMatchedHlaRepository dictionaryRepository;
        private readonly IHlaServiceClient hlaServiceClient;

        public DictionaryLookupService(IMatchedHlaRepository dictionaryRepository, IHlaServiceClient hlaServiceClient)
        {
            this.dictionaryRepository = dictionaryRepository;
            this.hlaServiceClient = hlaServiceClient;
        }

        public async Task<MatchingDictionaryEntry> GetMatchedHla(string matchLocus, string hlaName)
        {
            try
            {
                var lookupName = hlaName.Trim().TrimStart('*');
                var category = await hlaServiceClient.GetHlaTypingCategory(lookupName);
                
                switch (category)
                {
                    case HlaTypingCategory.Allele:
                        return LookupAllele(matchLocus, lookupName);
                    case HlaTypingCategory.XxCode:
                        return LookupXxCode(matchLocus, lookupName);
                    case HlaTypingCategory.Serology:
                        return GetDictionaryEntry(matchLocus, lookupName, TypingMethod.Serology);
                    default:
                        throw new ArgumentException($"Dictionary lookup cannot be performed for HLA typing category: {category}.");
                }
            }
            catch (Exception ex)
            {
                throw new MatchingDictionaryException(ex.Message, ex);
            }
        }

        private MatchingDictionaryEntry GetDictionaryEntry(string matchLocus, string lookupName, TypingMethod typingMethod)
        {
            var entry = dictionaryRepository.GetDictionaryEntry(matchLocus, lookupName, typingMethod);
            return entry ?? throw new InvalidHlaException(matchLocus, lookupName);
        }

        private MatchingDictionaryEntry LookupAllele(string matchLocus, string lookupName)
        {
            var allele = new Allele(LocusNames.GetMolecularLocusNameFromMatch(matchLocus), lookupName);
            return GetDictionaryEntry(matchLocus, allele.TwoFieldName, TypingMethod.Molecular);
        }

        private MatchingDictionaryEntry LookupXxCode(string matchLocus, string lookupName)
        {
            var firstField = lookupName.Split(':')[0];
            return GetDictionaryEntry(matchLocus, firstField, TypingMethod.Molecular);
        }
    }
}