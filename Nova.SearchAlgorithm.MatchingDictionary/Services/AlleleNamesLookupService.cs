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
        private readonly IAlleleNamesRepository alleleNamesRepository;
        private readonly IHlaCategorisationService hlaCategorisationService;

        public AlleleNamesLookupService(
            IAlleleNamesRepository alleleNamesRepository, 
            IHlaCategorisationService hlaCategorisationService)
        {
            this.alleleNamesRepository = alleleNamesRepository;
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
            var alleleNameEntry = await alleleNamesRepository.GetAlleleNameIfExists(matchLocus, lookupName);

            if (alleleNameEntry == null)
            {
                throw new InvalidHlaException(matchLocus, lookupName);
            }

            return alleleNameEntry.CurrentAlleleNames;
        }
    }
}