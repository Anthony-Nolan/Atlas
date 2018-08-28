using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.AlleleNameLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames
{
    public interface IAlleleNameVariantsExtractor
    {
        IEnumerable<IAlleleNameLookupResult> GetAlleleNames(IEnumerable<IAlleleNameLookupResult> originalAlleleNames);
    }

    public class AlleleNameVariantsExtractor : AlleleNamesExtractorBase, IAlleleNameVariantsExtractor
    {
        public AlleleNameVariantsExtractor(IWmdaDataRepository dataRepository)
            : base(dataRepository)
        {
        }

        public IEnumerable<IAlleleNameLookupResult> GetAlleleNames(IEnumerable<IAlleleNameLookupResult> originalAlleleNames)
        {
            var variantsNotFoundInHistories = originalAlleleNames.SelectMany(GetAlleleNameVariantsNotFoundInHistories);
            return GroupAlleleNamesByLocusAndLookupName(variantsNotFoundInHistories);
        }

        private IEnumerable<IAlleleNameLookupResult> GetAlleleNameVariantsNotFoundInHistories(IAlleleNameLookupResult alleleName)
        {
            var typingFromCurrentName = new AlleleTyping(
                alleleName.MatchLocus,
                alleleName.CurrentAlleleNames.First());

            return typingFromCurrentName
                .NameVariantsTruncatedByFieldAndOrExpressionSuffix
                .Where(nameVariant => AlleleNameIsNotInHistories(typingFromCurrentName.Locus, nameVariant))
                .Select(nameVariant => new AlleleNameLookupResult(
                    alleleName.MatchLocus,
                    nameVariant,
                    alleleName.CurrentAlleleNames));
        }

        private static IEnumerable<IAlleleNameLookupResult> GroupAlleleNamesByLocusAndLookupName(IEnumerable<IAlleleNameLookupResult> alleleNameVariants)
        {
            var groupedEntries = alleleNameVariants
                .GroupBy(e => new { e.MatchLocus, e.LookupName })
                .Select(e => new AlleleNameLookupResult(
                    e.Key.MatchLocus,
                    e.Key.LookupName,
                    e.SelectMany(x => x.CurrentAlleleNames).Distinct()
                ));

            return groupedEntries;
        }
    }
}
