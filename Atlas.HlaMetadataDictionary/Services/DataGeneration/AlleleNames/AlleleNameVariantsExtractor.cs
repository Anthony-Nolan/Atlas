using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.AlleleNames
{
    internal interface IAlleleNameVariantsExtractor
    {
        IEnumerable<IAlleleNameMetadata> GetAlleleNames(IEnumerable<IAlleleNameMetadata> originalAlleleNames, string hlaNomenclatureVersion);
    }

    internal class AlleleNameVariantsExtractor : AlleleNamesExtractorBase, IAlleleNameVariantsExtractor
    {
        public AlleleNameVariantsExtractor(IWmdaDataRepository dataRepository)
            : base(dataRepository)
        {
        }

        public IEnumerable<IAlleleNameMetadata> GetAlleleNames(IEnumerable<IAlleleNameMetadata> originalAlleleNames, string hlaNomenclatureVersion)
        {
            var variantsNotFoundInHistories = originalAlleleNames.SelectMany(n => GetAlleleNameVariantsNotFoundInHistories(n, hlaNomenclatureVersion)).ToList();
            return GroupAlleleNamesByLocusAndLookupName(variantsNotFoundInHistories);
        }

        private IEnumerable<IAlleleNameMetadata> GetAlleleNameVariantsNotFoundInHistories(IAlleleNameMetadata alleleName, string hlaNomenclatureVersion)
        {
            var typingFromCurrentName = new AlleleTyping(
                alleleName.Locus,
                alleleName.CurrentAlleleNames.First());

            return typingFromCurrentName
                .NameVariantsTruncatedByFieldAndOrExpressionSuffix
                .Where(nameVariant => AlleleNameIsNotInHistories(typingFromCurrentName.TypingLocus, nameVariant, hlaNomenclatureVersion))
                .Select(nameVariant => new AlleleNameMetadata(
                    alleleName.Locus,
                    nameVariant,
                    alleleName.CurrentAlleleNames));
        }

        private static IEnumerable<IAlleleNameMetadata> GroupAlleleNamesByLocusAndLookupName(IEnumerable<IAlleleNameMetadata> alleleNameVariants)
        {
            var groupedEntries = alleleNameVariants
                .GroupBy(e => new { e.Locus, e.LookupName })
                .Select(e => new AlleleNameMetadata(
                    e.Key.Locus,
                    e.Key.LookupName,
                    e.SelectMany(x => x.CurrentAlleleNames).Distinct()
                ));

            return groupedEntries;
        }
    }
}
