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
                if (!AlleleNameIsValid(alleleLookupName))
                {
                    throw new ArgumentException($"{alleleLookupName} is not an allele name.");
                }

                var formattedLookupName = FormatAlleleLookupName(alleleLookupName);

                return await PerformAlleleNameLookup(matchLocus, formattedLookupName);
            }
            catch (Exception ex)
            {
                var msg = $"Failed to lookup the allele name {alleleLookupName} at locus {matchLocus}.";
                throw new MatchingDictionaryHttpException(msg, ex);
            }
        }

        private bool AlleleNameIsValid(string alleleLookupName)
        {
            return !string.IsNullOrEmpty(alleleLookupName) && 
                hlaCategorisationService.GetHlaTypingCategory(alleleLookupName) == HlaTypingCategory.Allele;
        }

        private static string FormatAlleleLookupName(string alleleLookupName)
        {
            return alleleLookupName.Trim().TrimStart('*');
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