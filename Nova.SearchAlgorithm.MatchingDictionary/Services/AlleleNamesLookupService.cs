using Nova.HLAService.Client.Models;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    public interface IAlleleNamesLookupService
    {
        Task<IEnumerable<string>> GetCurrentAlleleNames(MatchLocus matchLocus, string alleleLookupName);
    }

    public class AlleleNamesLookupService : LookupServiceBase<string>, IAlleleNamesLookupService
    {
        private readonly IAlleleNamesLookupRepository alleleNamesLookupRepository;
        private readonly IHlaCategorisationService hlaCategorisationService;

        public AlleleNamesLookupService(
            IAlleleNamesLookupRepository alleleNamesLookupRepository, 
            IHlaCategorisationService hlaCategorisationService)
        {
            this.alleleNamesLookupRepository = alleleNamesLookupRepository;
            this.hlaCategorisationService = hlaCategorisationService;
        }

        public async Task<IEnumerable<string>> GetCurrentAlleleNames(MatchLocus matchLocus, string alleleLookupName)
        {
            return await GetLookupResults(matchLocus, alleleLookupName);
        }

        protected override bool LookupNameIsValid(string lookupName)
        {
            return !string.IsNullOrEmpty(lookupName) &&
                   hlaCategorisationService.GetHlaTypingCategory(lookupName) == HlaTypingCategory.Allele;
        }

        protected override async Task<IEnumerable<string>> PerformLookup(MatchLocus matchLocus, string lookupName)
        {
            var alleleNameLookupResult = await alleleNamesLookupRepository.GetAlleleNameIfExists(matchLocus, lookupName);

            if (alleleNameLookupResult == null)
            {
                throw new InvalidHlaException(matchLocus, lookupName);
            }

            return alleleNameLookupResult.CurrentAlleleNames;
        }
    }
}