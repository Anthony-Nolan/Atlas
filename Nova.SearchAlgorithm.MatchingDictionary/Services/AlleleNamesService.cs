using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.AlleleNameLookup;

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
        IEnumerable<AlleleNameLookupResult> GetAlleleNamesAndTheirVariants();
    }

    /// <inheritdoc />
    /// <summary>
    /// Extracts allele names from a WMDA data repository and persists the resulting collection.
    /// </summary>
    public class AlleleNamesService : IAlleleNamesService
    {
        private readonly IAlleleNamesFromHistoriesExtractor alleleNamesFromHistoriesExtractor;
        private readonly IAlleleNameVariantsExtractor alleleNameVariantsExtractor;
        private readonly IReservedAlleleNamesExtractor reservedAlleleNamesExtractor;
        private readonly IAlleleNamesLookupRepository alleleNamesLookupRepository;
        
        public AlleleNamesService(
            IAlleleNamesFromHistoriesExtractor alleleNamesFromHistoriesExtractor,
            IAlleleNameVariantsExtractor alleleNameVariantsExtractor,
            IReservedAlleleNamesExtractor reservedAlleleNamesExtractor,
            IAlleleNamesLookupRepository alleleNamesLookupRepository)
        {
            this.alleleNamesFromHistoriesExtractor = alleleNamesFromHistoriesExtractor;
            this.alleleNameVariantsExtractor = alleleNameVariantsExtractor;
            this.reservedAlleleNamesExtractor = reservedAlleleNamesExtractor;
            this.alleleNamesLookupRepository = alleleNamesLookupRepository;
        }

        public async Task RecreateAlleleNames()
        {
            var alleleNames = GetAlleleNamesAndTheirVariants();
            await alleleNamesLookupRepository.RecreateHlaLookupTable(alleleNames);
        }

        public IEnumerable<AlleleNameLookupResult> GetAlleleNamesAndTheirVariants()
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

        private IEnumerable<AlleleNameLookupResult> GetAlleleNamesFromHistories()
        {
            return alleleNamesFromHistoriesExtractor.GetAlleleNames();
        }

        private IEnumerable<AlleleNameLookupResult> GetAlleleNameVariants(IEnumerable<AlleleNameLookupResult> originalAlleleNames)
        {
            return alleleNameVariantsExtractor.GetAlleleNames(originalAlleleNames);
        }

        private IEnumerable<AlleleNameLookupResult> GetReservedAlleleNames()
        {
            return reservedAlleleNamesExtractor.GetAlleleNames();
        }
    }
}

