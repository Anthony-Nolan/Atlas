using System;
using System.Threading.Tasks;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

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
            MatchingDictionaryEntry entry;
            try
            {
                var categoryResult = hlaServiceClient.GetHlaTypingCategory(hlaName);
                //todo: handle service error - general errors, and specific unknown HLA error

                var category = await categoryResult;
                var typingMethod = category == HlaTypingCategory.Serology ?
                    TypingMethod.Serology : TypingMethod.Molecular;
                entry = dictionaryRepository.GetDictionaryEntry(matchLocus, hlaName, typingMethod);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
            return entry;
        }
    }
}