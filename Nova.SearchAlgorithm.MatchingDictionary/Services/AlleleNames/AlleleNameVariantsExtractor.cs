using Nova.SearchAlgorithm.MatchingDictionary.Models.AlleleNames;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames
{
    public interface IAlleleNameVariantsExtractor
    {
        IEnumerable<AlleleNameLookupResult> GetAlleleNames(IEnumerable<AlleleNameLookupResult> originalAlleleNames);
    }

    public class AlleleNameVariantsExtractor : AlleleNamesExtractorBase, IAlleleNameVariantsExtractor
    {
        public AlleleNameVariantsExtractor(IWmdaDataRepository dataRepository)
            : base(dataRepository)
        {
        }

        public IEnumerable<AlleleNameLookupResult> GetAlleleNames(IEnumerable<AlleleNameLookupResult> originalAlleleNames)
        {
            var variantsNotFoundInHistories = originalAlleleNames.SelectMany(GetAlleleNameVariantsNotFoundInHistories);
            return GroupAlleleNamesByLocusAndLookupName(variantsNotFoundInHistories);
        }

        private IEnumerable<AlleleNameLookupResult> GetAlleleNameVariantsNotFoundInHistories(AlleleNameLookupResult alleleName)
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

        private static IEnumerable<AlleleNameLookupResult> GroupAlleleNamesByLocusAndLookupName(IEnumerable<AlleleNameLookupResult> alleleNameVariants)
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
