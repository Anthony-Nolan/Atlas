using Nova.SearchAlgorithm.MatchingDictionary.Models.AlleleNames;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    public interface IAlleleNamesService
    {
        IEnumerable<AlleleNameEntry> GetAlleleNamesAndTheirVariants();
    }

    public class AlleleNamesService : IAlleleNamesService
    {
        private readonly List<AlleleNameHistory> histories;
        private readonly List<HlaNom> allelesInCurrentVersionOfHlaNom;
        private readonly List<HlaNom> allHistoricalNamesAsAlleleTypings;

        public AlleleNamesService(IWmdaDataRepository dataRepository)
        {
            // enumerate collections here as they will be queried thousands of times
            histories = dataRepository.AlleleNameHistories.ToList();
            allelesInCurrentVersionOfHlaNom = dataRepository.Alleles.ToList();

            allHistoricalNamesAsAlleleTypings = (
                from history in histories
                from historicalName in history.DistinctAlleleNames
                select new HlaNom(TypingMethod.Molecular, history.Locus, historicalName)
                ).ToList();
        }

        public IEnumerable<AlleleNameEntry> GetAlleleNamesAndTheirVariants()
        {
            var alleleNamesFromHistories = histories.SelectMany(GetAlleleNamesFromSingleHistory).ToList();
            var alleleNameVariants = GetUniqueAlleleNameVariantsNotFoundInHistories(alleleNamesFromHistories);

            return alleleNamesFromHistories.Concat(alleleNameVariants);
        }

        private IEnumerable<AlleleNameEntry> GetAlleleNamesFromSingleHistory(AlleleNameHistory history)
        {
            return history.TryToAlleleNameEntries(out var entries)
                ? entries
                : GetAlleleNameEntriesUsingIdenticalToAlleleName(history);
        }

        private IEnumerable<AlleleNameEntry> GetAlleleNameEntriesUsingIdenticalToAlleleName(AlleleNameHistory history)
        {
            var identicalToAlleleName = GetAlleleNameFromIdenticalToProperty(history);
            return history.ToAlleleNameEntries(identicalToAlleleName);
        }

        private string GetAlleleNameFromIdenticalToProperty(AlleleNameHistory history)
        {
            var mostRecentNameAsAllele = new HlaNom(
                TypingMethod.Molecular, history.Locus, history.MostRecentAlleleName);

            var identicalToAlleleName = allelesInCurrentVersionOfHlaNom
                .First(allele => allele.TypingEquals(mostRecentNameAsAllele))
                .IdenticalHla;

            return identicalToAlleleName;
        }

        private IEnumerable<AlleleNameEntry> GetUniqueAlleleNameVariantsNotFoundInHistories(IEnumerable<AlleleNameEntry> alleleNames)
        {
            var variantsNotFoundInHistories = alleleNames.SelectMany(GetAlleleNameVariantsNotFoundInHistories); ;
            return GroupAlleleNamesByLocusAndLookupName(variantsNotFoundInHistories);
        }

        private IEnumerable<AlleleNameEntry> GetAlleleNameVariantsNotFoundInHistories(AlleleNameEntry alleleName)
        {
            var alleleTypingFromCurrentName = new AlleleTyping(
                alleleName.MatchLocus, 
                alleleName.CurrentAlleleNames.First());

            return alleleTypingFromCurrentName
                .NameVariantsTruncatedByFieldAndExpressionSuffix
                .Where(nameVariant => !AlleleNameVariantInHistories(alleleTypingFromCurrentName.Locus, nameVariant))
                .Select(nameVariant => new AlleleNameEntry(
                    alleleName.MatchLocus, 
                    nameVariant, 
                    alleleName.CurrentAlleleNames));
        }

        private bool AlleleNameVariantInHistories(string locus, string nameVariant)
        {
            return allHistoricalNamesAsAlleleTypings.Any(historicalTyping => 
                historicalTyping.Locus.Equals(locus) && historicalTyping.Name.Equals(nameVariant));
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

