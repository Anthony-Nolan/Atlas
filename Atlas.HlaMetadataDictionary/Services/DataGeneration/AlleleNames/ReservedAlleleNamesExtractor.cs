using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.Models.Lookups.AlleleNameLookup;
using Atlas.HlaMetadataDictionary.Models.Wmda;
using Atlas.HlaMetadataDictionary.Repositories;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.AlleleNames
{
    internal interface IReservedAlleleNamesExtractor
    {
        IEnumerable<AlleleNameMetadata> GetAlleleNames(string hlaNomenclatureVersion);
    }

    internal class ReservedAlleleNamesExtractor : AlleleNamesExtractorBase, IReservedAlleleNamesExtractor
    {
        public ReservedAlleleNamesExtractor(IWmdaDataRepository dataRepository)
            : base(dataRepository)
        {
        }

        public IEnumerable<AlleleNameMetadata> GetAlleleNames(string hlaNomenclatureVersion)
        {
            return AllelesInVersionOfHlaNom(hlaNomenclatureVersion)
                .Where(a => AlleleNameIsReserved(a, hlaNomenclatureVersion))
                .Select(allele => new AlleleNameMetadata(allele.TypingLocus, allele.Name, allele.Name));
        }

        private bool AlleleNameIsReserved(HlaNom allele, string hlaNomenclatureVersion)
        {
            return allele.IsDeleted && AlleleNameIsNotInHistories(allele.TypingLocus, allele.Name, hlaNomenclatureVersion);
        }
    }
}
