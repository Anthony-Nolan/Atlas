using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nova.HLAService.Client.Models;
using Nova.HLAService.Client.Services;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    public interface IAlleleNamesLookupService
    {
        Task<IEnumerable<string>> GetCurrentAlleleNames(MatchLocus matchLocus, string alleleLookupName);
    }

    public class AlleleNamesLookupService : IAlleleNamesLookupService
    {
        private readonly IAlleleNamesRepository alleleNamesRepository;
        private readonly IHlaCategorisationService hlaCategorisationService;

        public AlleleNamesLookupService(IAlleleNamesRepository alleleNamesRepository, IHlaCategorisationService hlaCategorisationService)
        {
            this.alleleNamesRepository = alleleNamesRepository;
            this.hlaCategorisationService = hlaCategorisationService;
        }

        public async Task<IEnumerable<string>> GetCurrentAlleleNames(MatchLocus matchLocus, string alleleLookupName)
        {
            try
            {
                if (!TryPrepareAlleleNameForLookup(alleleLookupName, out var preparedLookupName))
                {
                    throw new ArgumentException($"{alleleLookupName} is not an allele name.");
                }

                return await PerformAlleleNameLookup(matchLocus, preparedLookupName);
            }
            catch (Exception ex)
            {
                var msg = $"Failed to get matching HLA for {alleleLookupName} at locus {matchLocus}.";
                throw new MatchingDictionaryHttpException(msg, ex);
            }
        }

        private bool TryPrepareAlleleNameForLookup(string submittedLookupName, out string preparedLookupName)
        {
            if (string.IsNullOrEmpty(submittedLookupName) ||
                    hlaCategorisationService.GetHlaTypingCategory(submittedLookupName) != HlaTypingCategory.Allele)
            {
                preparedLookupName = submittedLookupName;
                return false;
            }

            preparedLookupName = submittedLookupName.Trim().TrimStart('*');
            return true;
        }

        private async Task<IEnumerable<string>> PerformAlleleNameLookup(MatchLocus matchLocus, string alleleLookupName)
        {
            var alleleNameEntry = await alleleNamesRepository.GetAlleleNameIfExists(matchLocus, alleleLookupName);

            if (alleleNameEntry == null)
            {
                throw new InvalidHlaException(matchLocus, alleleLookupName);
            }

            return alleleNameEntry.CurrentAlleleNames;
        }
    }
}