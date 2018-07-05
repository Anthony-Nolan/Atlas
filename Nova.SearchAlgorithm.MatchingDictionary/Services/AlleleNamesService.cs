using Nova.SearchAlgorithm.MatchingDictionary.Models.AlleleNames;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    /// Creates a complete collection of Allele Names.
    /// </summary>
    public interface IAlleleNamesService
    {
        /// <summary>
        /// Generates and persists Allele Names collection,
        /// overwriting any existing collection.
        /// </summary>
        /// <returns></returns>
        Task RecreateAlleleNames();

        /// <summary>
        /// Generates - but does not persist - Allele Names collection.
        /// </summary>
        /// <returns></returns>
        IEnumerable<AlleleNameEntry> GetAlleleNamesAndTheirVariants();
    }

    /// <inheritdoc />
    /// <summary>
    /// Extracts allele names from a WMDA data repository and persists the resulting collection.
    /// </summary>
    public class AlleleNamesService : IAlleleNamesService
    {
        private readonly AlleleNamesExtractorArgs extractorArgs;
        private readonly IAlleleNamesRepository alleleNamesRepository;

        public AlleleNamesService(IWmdaDataRepository dataRepository, IAlleleNamesRepository alleleNamesRepository)
        {
            extractorArgs = new AlleleNamesExtractorArgs(
                dataRepository.AlleleNameHistories,
                dataRepository.Alleles);

            this.alleleNamesRepository = alleleNamesRepository;
        }

        public async Task RecreateAlleleNames()
        {
            var alleleNames = GetAlleleNamesAndTheirVariants();
            await alleleNamesRepository.RecreateAlleleNamesTable(alleleNames);
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

