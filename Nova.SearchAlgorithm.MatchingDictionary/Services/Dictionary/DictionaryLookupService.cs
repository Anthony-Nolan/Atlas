using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;

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
                var category = await hlaServiceClient.GetHlaTypingCategory(hlaName);
                var typingMethod = category == HlaTypingCategory.Serology ?
                    TypingMethod.Serology : TypingMethod.Molecular;
                entry = dictionaryRepository.GetDictionaryEntry(matchLocus, hlaName, typingMethod);
            }
            catch (Exception ex)
            {
                throw new MatchingDictionaryException(ex.Message, ex.InnerException);
            }
            
            return entry;
        }
    }
}