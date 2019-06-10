using Nova.HLAService.Client.Models;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    public interface IAlleleNamesLookupService
    {
        Task<IEnumerable<string>> GetCurrentAlleleNames(Locus locus, string alleleLookupName);
    }

    public class AlleleNamesLookupService : LookupServiceBase<IEnumerable<string>>, IAlleleNamesLookupService
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

        public async Task<IEnumerable<string>> GetCurrentAlleleNames(Locus locus, string alleleLookupName)
        {
            return await GetLookupResults(locus, alleleLookupName);
        }

        protected override bool LookupNameIsValid(string lookupName)
        {
            return !string.IsNullOrEmpty(lookupName) &&
                   hlaCategorisationService.GetHlaTypingCategory(lookupName) == HlaTypingCategory.Allele;
        }

        protected override async Task<IEnumerable<string>> PerformLookup(Locus locus, string lookupName)
        {
            var alleleNameLookupResult = await alleleNamesLookupRepository.GetAlleleNameIfExists(locus, lookupName);

            if (alleleNameLookupResult == null)
            {
                throw new InvalidHlaException(locus, lookupName);
            }

            return alleleNameLookupResult.CurrentAlleleNames;
        }
    }
}