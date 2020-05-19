using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.Models.Lookups.AlleleNameLookup;
using Atlas.HlaMetadataDictionary.Models.Wmda;
using Atlas.HlaMetadataDictionary.Repositories;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.AlleleNames
{
    public interface IReservedAlleleNamesExtractor
    {
        IEnumerable<AlleleNameLookupResult> GetAlleleNames(string hlaDatabaseVersion);
    }

    public class ReservedAlleleNamesExtractor : AlleleNamesExtractorBase, IReservedAlleleNamesExtractor
    {
        public ReservedAlleleNamesExtractor(IWmdaDataRepository dataRepository)
            : base(dataRepository)
        {
        }

        public IEnumerable<AlleleNameLookupResult> GetAlleleNames(string hlaDatabaseVersion)
        {
            return AllelesInVersionOfHlaNom(hlaDatabaseVersion)
                .Where(a => AlleleNameIsReserved(a, hlaDatabaseVersion))
                .Select(allele => new AlleleNameLookupResult(allele.TypingLocus, allele.Name, allele.Name));
        }

        private bool AlleleNameIsReserved(HlaNom allele, string hlaDatabaseVersion)
        {
            return allele.IsDeleted && AlleleNameIsNotInHistories(allele.TypingLocus, allele.Name, hlaDatabaseVersion);
        }
    }
}
