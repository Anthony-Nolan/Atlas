using Atlas.MatchingAlgorithm.MatchingDictionary.Models.HLATypings;
using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.AlleleNameLookup;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories;
using Atlas.MatchingAlgorithm.MatchingDictionary.Services.DataGeneration.AlleleNames;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Services.AlleleNames
{
    public interface IAlleleNameVariantsExtractor
    {
        IEnumerable<IAlleleNameLookupResult> GetAlleleNames(IEnumerable<IAlleleNameLookupResult> originalAlleleNames, string hlaDatabaseVersion);
    }

    public class AlleleNameVariantsExtractor : AlleleNamesExtractorBase, IAlleleNameVariantsExtractor
    {
        public AlleleNameVariantsExtractor(IWmdaDataRepository dataRepository)
            : base(dataRepository)
        {
        }

        public IEnumerable<IAlleleNameLookupResult> GetAlleleNames(IEnumerable<IAlleleNameLookupResult> originalAlleleNames, string hlaDatabaseVersion)
        {
            var variantsNotFoundInHistories = originalAlleleNames.SelectMany(n => GetAlleleNameVariantsNotFoundInHistories(n, hlaDatabaseVersion)).ToList();
            return GroupAlleleNamesByLocusAndLookupName(variantsNotFoundInHistories);
        }

        private IEnumerable<IAlleleNameLookupResult> GetAlleleNameVariantsNotFoundInHistories(IAlleleNameLookupResult alleleName, string hlaDatabaseVersion)
        {
            var typingFromCurrentName = new AlleleTyping(
                alleleName.Locus,
                alleleName.CurrentAlleleNames.First());

            return typingFromCurrentName
                .NameVariantsTruncatedByFieldAndOrExpressionSuffix
                .Where(nameVariant => AlleleNameIsNotInHistories(typingFromCurrentName.TypingLocus, nameVariant, hlaDatabaseVersion))
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
