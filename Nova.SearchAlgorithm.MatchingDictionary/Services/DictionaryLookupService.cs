using System;
using System.Threading.Tasks;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary.Lookups;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    public interface IDictionaryLookupService
    {
        Task<IMatchingHlaLookupResult> GetMatchingHla(MatchLocus matchLocus, string hlaName);
    }

    /// <summary>
    /// This class is responsible for
    /// determining the dictionary lookup strategy
    /// for submitted HLA types.
    /// </summary>
    public class DictionaryLookupService : IDictionaryLookupService
    {
        private readonly IMatchedHlaRepository dictionaryRepository;
        private readonly IHlaServiceClient hlaServiceClient;

        public DictionaryLookupService(IMatchedHlaRepository dictionaryRepository, IHlaServiceClient hlaServiceClient)
        {
            this.dictionaryRepository = dictionaryRepository;
            this.hlaServiceClient = hlaServiceClient;
        }

        public async Task<IMatchingHlaLookupResult> GetMatchingHla(MatchLocus matchLocus, string hlaName)
        {
            try
            {
                var lookupName = hlaName.Trim().TrimStart('*');
                var category = await hlaServiceClient.GetHlaTypingCategory(lookupName);

                MatchingDictionaryLookup lookup;
                switch (category)
                {
                    case HlaTypingCategory.Allele:
                        lookup = new AlleleLookup(dictionaryRepository);
                        break;
                    case HlaTypingCategory.XxCode:
                        lookup = new XxCodeLookup(dictionaryRepository) ;
                        break;
                    case HlaTypingCategory.Serology:
                        lookup = new SerologyLookup(dictionaryRepository);
                        break;
                    case HlaTypingCategory.NmdpCode:
                        lookup = new NmdpCodeLookup(dictionaryRepository, hlaServiceClient);
                        break;
                    default:
                        throw new ArgumentException($"Dictionary lookup cannot be performed for HLA typing category: {category}.");
                }

                return await lookup.PerformLookupAsync(matchLocus, lookupName);
            }
            catch (Exception ex)
            {
                var msg = $"Failed to get matching HLA for {hlaName} at locus {matchLocus}.";
                throw new MatchingDictionaryException(msg, ex);
            }
        }
    }
}