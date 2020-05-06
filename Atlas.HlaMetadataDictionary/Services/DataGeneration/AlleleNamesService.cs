using Atlas.HlaMetadataDictionary.Models.Lookups.AlleleNameLookup;
using Atlas.HlaMetadataDictionary.Services.AlleleNames;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.HlaMetadataDictionary.Services
{
    /// <summary>
    /// Generates a complete collection of Allele Names.
    /// </summary>
    public interface IAlleleNamesService
    {
        IEnumerable<IAlleleNameLookupResult> GetAlleleNamesAndTheirVariants(string hlaDatabaseVersion);
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

        public IEnumerable<IAlleleNameLookupResult> GetAlleleNamesAndTheirVariants(string hlaDatabaseVersion)
        {
            var alleleNamesFromHistories = GetAlleleNamesFromHistories(hlaDatabaseVersion).ToList();
            var nameVariants = GetAlleleNameVariants(alleleNamesFromHistories, hlaDatabaseVersion).ToList();
            var reservedNames = GetReservedAlleleNames(hlaDatabaseVersion).ToList();

            var mergedCollectionOfAlleleNames = alleleNamesFromHistories
                .Concat(nameVariants)
                .Concat(reservedNames)
                .ToList();

            return mergedCollectionOfAlleleNames;
        }

        private IEnumerable<IAlleleNameLookupResult> GetAlleleNamesFromHistories(string hlaDatabaseVersion)
        {
            return alleleNamesFromHistoriesExtractor.GetAlleleNames(hlaDatabaseVersion);
        }

        private IEnumerable<IAlleleNameLookupResult> GetAlleleNameVariants(IEnumerable<IAlleleNameLookupResult> originalAlleleNames, string hlaDatabaseVersion)
        {
            return alleleNameVariantsExtractor.GetAlleleNames(originalAlleleNames, hlaDatabaseVersion);
        }

        private IEnumerable<IAlleleNameLookupResult> GetReservedAlleleNames(string hlaDatabaseVersion)
        {
            return reservedAlleleNamesExtractor.GetAlleleNames(hlaDatabaseVersion);
        }
    }
}

