using System;
using System.Threading.Tasks;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary.Lookups;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    /// Determines and executes the dictionary lookup strategy for submitted HLA types.
    /// </summary>
    public interface IMatchingDictionaryLookupService
    {
        Task<IMatchingHlaLookupResult> GetMatchingHla(MatchLocus matchLocus, string hlaName);
    }

    public class MatchingDictionaryLookupService : IMatchingDictionaryLookupService
    {
        private readonly IMatchingDictionaryRepository dictionaryRepository;
        private readonly IHlaServiceClient hlaServiceClient;

        public MatchingDictionaryLookupService(IMatchingDictionaryRepository dictionaryRepository, IHlaServiceClient hlaServiceClient)
        {
            this.dictionaryRepository = dictionaryRepository;
            this.hlaServiceClient = hlaServiceClient;
        }

        public async Task<IMatchingHlaLookupResult> GetMatchingHla(MatchLocus matchLocus, string hlaName)
        {
            if (string.IsNullOrEmpty(hlaName))
            {
                throw new MatchingDictionaryException($"Cannot lookup null or blank HLA (locus was {matchLocus})");
            }

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
                    case HlaTypingCategory.AlleleString:
                        lookup = new AlleleStringLookup(dictionaryRepository);
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