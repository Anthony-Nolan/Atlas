using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.Utils.Hla.Services;
using Atlas.Utils.Hla.Models;
using Atlas.HlaMetadataDictionary.Exceptions;
using Atlas.HlaMetadataDictionary.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Utils.Models;

namespace Atlas.HlaMetadataDictionary.Services
{
    public interface IAlleleNamesLookupService
    {
        Task<IEnumerable<string>> GetCurrentAlleleNames(Locus locus, string alleleLookupName, string hlaDatabaseVersion);
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

        public async Task<IEnumerable<string>> GetCurrentAlleleNames(Locus locus, string alleleLookupName, string hlaDatabaseVersion)
        {
            return await GetLookupResults(locus, alleleLookupName, hlaDatabaseVersion);
        }

        protected override bool LookupNameIsValid(string lookupName)
        {
            return !string.IsNullOrEmpty(lookupName) &&
                   hlaCategorisationService.GetHlaTypingCategory(lookupName) == HlaTypingCategory.Allele;
        }

        protected override async Task<IEnumerable<string>> PerformLookup(Locus locus, string lookupName, string hlaDatabaseVersion)
        {
            var alleleNameLookupResult = await alleleNamesLookupRepository.GetAlleleNameIfExists(locus, lookupName, hlaDatabaseVersion);

            if (alleleNameLookupResult == null)
            {
                throw new InvalidHlaException(new HlaInfo(locus, lookupName));
            }

            return alleleNameLookupResult.CurrentAlleleNames;
        }
    }
}