using Nova.SearchAlgorithm.MatchingDictionary.Models.AlleleNames;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames
{
    internal class AlleleNameVariantsExtractor : AlleleNamesExtractorBase
    {
        private readonly IEnumerable<AlleleNameEntry> originalAlleleNames;

        public AlleleNameVariantsExtractor(
            AlleleNamesExtractorArgs extractorArgs, IEnumerable<AlleleNameEntry> originalAlleleNames)
                : base(extractorArgs)
        {
            this.originalAlleleNames = originalAlleleNames;
        }

        public override IEnumerable<AlleleNameEntry> GetAlleleNames()
        {
            var variantsNotFoundInHistories = originalAlleleNames.SelectMany(GetAlleleNameVariantsNotFoundInHistories);
            return GroupAlleleNamesByLocusAndLookupName(variantsNotFoundInHistories);
        }

        private IEnumerable<AlleleNameEntry> GetAlleleNameVariantsNotFoundInHistories(AlleleNameEntry alleleName)
        {
            var typingFromCurrentName = new AlleleTyping(
                alleleName.MatchLocus,
                alleleName.CurrentAlleleNames.First());

            return typingFromCurrentName
                .NameVariantsTruncatedByFieldAndOrExpressionSuffix
                .Where(nameVariant => AlleleNameIsNotInHistories(typingFromCurrentName.Locus, nameVariant))
                .Select(nameVariant => new AlleleNameEntry(
                    alleleName.MatchLocus,
                    nameVariant,
                    alleleName.CurrentAlleleNames));
        }

        private static IEnumerable<AlleleNameEntry> GroupAlleleNamesByLocusAndLookupName(IEnumerable<AlleleNameEntry> alleleNameVariants)
        {
            var groupedEntries = alleleNameVariants
                .GroupBy(e => new { e.MatchLocus, e.LookupName })
                .Select(e => new AlleleNameEntry(
                    e.Key.MatchLocus,
                    e.Key.LookupName,
                    e.SelectMany(x => x.CurrentAlleleNames).Distinct()
                ));

            return groupedEntries;
        }
    }
}
