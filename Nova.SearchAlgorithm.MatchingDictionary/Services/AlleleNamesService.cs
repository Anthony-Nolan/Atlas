using Nova.SearchAlgorithm.MatchingDictionary.Models.AlleleNames;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    public interface IAlleleNamesService
    {
        IEnumerable<AlleleNameEntry> GetAlleleNamesAndTheirVariants();
    }

    public class AlleleNamesService : IAlleleNamesService
    {
        private readonly AlleleNamesExtractorArgs extractorArgs;

        public AlleleNamesService(IWmdaDataRepository dataRepository)
        {
            extractorArgs = new AlleleNamesExtractorArgs(
                dataRepository.AlleleNameHistories,
                dataRepository.Alleles);
        }

        public IEnumerable<AlleleNameEntry> GetAlleleNamesAndTheirVariants()
        {
            var alleleNamesFromHistories = GetAlleleNamesFromHistories().ToList();
            var nameVariants = GetAlleleNameVariants(alleleNamesFromHistories);
            var reservedNames = GetReservedAlleleNames();

            var mergedCollectionOfAlleleNames = alleleNamesFromHistories
                .Concat(nameVariants)
                .Concat(reservedNames)
                .ToList();

            // TODO: NOVA-1385 - several alleles are causing duplicate entry errors
            // Need further input on how these edge cases should be handled
            // Removing the offending alleles from the collection in the mean time
            mergedCollectionOfAlleleNames
                .RemoveAll(AlleleNamesSpecialCases.RemoveSpecifiedAlleleNames);

            return mergedCollectionOfAlleleNames;
        }

        private IEnumerable<AlleleNameEntry> GetAlleleNamesFromHistories()
        {
            return new AlleleNamesFromHistoriesExtractor(extractorArgs).GetAlleleNames();
        }

        private IEnumerable<AlleleNameEntry> GetAlleleNameVariants(IEnumerable<AlleleNameEntry> originalAlleleNames)
        {
            return new AlleleNameVariantsExtractor(extractorArgs, originalAlleleNames).GetAlleleNames();
        }

        private IEnumerable<AlleleNameEntry> GetReservedAlleleNames()
        {
            return new ReservedAlleleNamesExtractor(extractorArgs).GetAlleleNames();
        }
    }
}

