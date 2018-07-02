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

            return alleleNamesFromHistories.Concat(nameVariants).Concat(reservedNames);
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

