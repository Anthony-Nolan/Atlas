using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

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
