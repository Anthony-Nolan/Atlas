using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.Lookups.AlleleNameLookup;
using Atlas.HlaMetadataDictionary.Repositories;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.AlleleNames
{
    internal interface IAlleleNameVariantsExtractor
    {
        IEnumerable<IAlleleNameLookupResult> GetAlleleNames(IEnumerable<IAlleleNameLookupResult> originalAlleleNames, string hlaNomenclatureVersion);
    }

    internal class AlleleNameVariantsExtractor : AlleleNamesExtractorBase, IAlleleNameVariantsExtractor
    {
        public AlleleNameVariantsExtractor(IWmdaDataRepository dataRepository)
            : base(dataRepository)
        {
        }

        public IEnumerable<IAlleleNameLookupResult> GetAlleleNames(IEnumerable<IAlleleNameLookupResult> originalAlleleNames, string hlaNomenclatureVersion)
        {
            var variantsNotFoundInHistories = originalAlleleNames.SelectMany(n => GetAlleleNameVariantsNotFoundInHistories(n, hlaNomenclatureVersion)).ToList();
            return GroupAlleleNamesByLocusAndLookupName(variantsNotFoundInHistories);
        }

        private IEnumerable<IAlleleNameLookupResult> GetAlleleNameVariantsNotFoundInHistories(IAlleleNameLookupResult alleleName, string hlaNomenclatureVersion)
        {
            var typingFromCurrentName = new AlleleTyping(
                alleleName.Locus,
                alleleName.CurrentAlleleNames.First());

            return typingFromCurrentName
                .NameVariantsTruncatedByFieldAndOrExpressionSuffix
                .Where(nameVariant => AlleleNameIsNotInHistories(typingFromCurrentName.TypingLocus, nameVariant, hlaNomenclatureVersion))
                .Select(nameVariant => new AlleleNameLookupResult(
                    alleleName.Locus,
                    nameVariant,
                    alleleName.CurrentAlleleNames));
        }

        private static IEnumerable<IAlleleNameLookupResult> GroupAlleleNamesByLocusAndLookupName(IEnumerable<IAlleleNameLookupResult> alleleNameVariants)
        {
            var groupedEntries = alleleNameVariants
                .GroupBy(e => new { e.Locus, e.LookupName })
                .Select(e => new AlleleNameLookupResult(
                    e.Key.Locus,
                    e.Key.LookupName,
                    e.SelectMany(x => x.CurrentAlleleNames).Distinct()
                ));

            return groupedEntries;
        }
    }
}
