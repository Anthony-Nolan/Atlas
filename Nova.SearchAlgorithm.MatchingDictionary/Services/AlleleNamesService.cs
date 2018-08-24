using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.AlleleNameLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    /// Generates a complete collection of Allele Names.
    /// </summary>
    public interface IAlleleNamesService
    {
        IEnumerable<IAlleleNameLookupResult> GetAlleleNamesAndTheirVariants();
    }

    /// <inheritdoc />
    /// <summary>
    /// Extracts allele names from a WMDA data repository.
    /// </summary>
    public class AlleleNamesService : IAlleleNamesService
    {
        private readonly IAlleleNamesFromHistoriesExtractor alleleNamesFromHistoriesExtractor;
        private readonly IAlleleNameVariantsExtractor alleleNameVariantsExtractor;
        private readonly IReservedAlleleNamesExtractor reservedAlleleNamesExtractor;
        
        public AlleleNamesService(
            IAlleleNamesFromHistoriesExtractor alleleNamesFromHistoriesExtractor,
            IAlleleNameVariantsExtractor alleleNameVariantsExtractor,
            IReservedAlleleNamesExtractor reservedAlleleNamesExtractor)
        {
            this.alleleNamesFromHistoriesExtractor = alleleNamesFromHistoriesExtractor;
            this.alleleNameVariantsExtractor = alleleNameVariantsExtractor;
            this.reservedAlleleNamesExtractor = reservedAlleleNamesExtractor;
        }

        public IEnumerable<IAlleleNameLookupResult> GetAlleleNamesAndTheirVariants()
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

        private IEnumerable<IAlleleNameLookupResult> GetAlleleNamesFromHistories()
        {
            return alleleNamesFromHistoriesExtractor.GetAlleleNames();
        }

        private IEnumerable<IAlleleNameLookupResult> GetAlleleNameVariants(IEnumerable<IAlleleNameLookupResult> originalAlleleNames)
        {
            return alleleNameVariantsExtractor.GetAlleleNames(originalAlleleNames);
        }

        private IEnumerable<IAlleleNameLookupResult> GetReservedAlleleNames()
        {
            return reservedAlleleNamesExtractor.GetAlleleNames();
        }
    }
}

