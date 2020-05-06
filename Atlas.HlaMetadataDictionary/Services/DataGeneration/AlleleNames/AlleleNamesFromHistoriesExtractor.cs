using Atlas.MatchingAlgorithm.MatchingDictionary.Models.HLATypings;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.AlleleNameLookup;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Wmda;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories;
using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.MatchingDictionary.Services.DataGeneration.AlleleNames;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Services.AlleleNames
{
    public interface IAlleleNamesFromHistoriesExtractor
    {
        IEnumerable<AlleleNameLookupResult> GetAlleleNames(string hlaDatabaseVersion);
    }

    public class AlleleNamesFromHistoriesExtractor : AlleleNamesExtractorBase, IAlleleNamesFromHistoriesExtractor
    {
        private readonly IAlleleNameHistoriesConsolidator historiesConsolidator;

        private IEnumerable<AlleleNameHistory> ConsolidatedAlleleNameHistories(string hlaDatabaseVersion)
        {
            return historiesConsolidator.GetConsolidatedAlleleNameHistories(hlaDatabaseVersion).ToList();
        }

        public AlleleNamesFromHistoriesExtractor(
            IAlleleNameHistoriesConsolidator historiesConsolidator,
            IWmdaDataRepository dataRepository)
            : base(dataRepository)
        {
            this.historiesConsolidator = historiesConsolidator;
        }

        public IEnumerable<AlleleNameLookupResult> GetAlleleNames(string hlaDatabaseVersion)
        {
            return ConsolidatedAlleleNameHistories(hlaDatabaseVersion)
                .SelectMany(h => GetAlleleNamesFromSingleHistory(h, hlaDatabaseVersion));
        }

        private IEnumerable<AlleleNameLookupResult> GetAlleleNamesFromSingleHistory(AlleleNameHistory history, string hlaDatabaseVersion)
        {
            var currentAlleleName = GetCurrentAlleleName(history, hlaDatabaseVersion);

            return !string.IsNullOrEmpty(currentAlleleName)
                ? history.ToAlleleNameLookupResults(currentAlleleName)
                : new List<AlleleNameLookupResult>();
        }

        private string GetCurrentAlleleName(AlleleNameHistory history, string hlaDatabaseVersion)
        {
            return history.CurrentAlleleName ?? GetIdenticalToAlleleName(history, hlaDatabaseVersion);
        }

        private string GetIdenticalToAlleleName(AlleleNameHistory history, string hlaDatabaseVersion)
        {
            var mostRecentNameAsTyping = new HlaNom(
                TypingMethod.Molecular, history.TypingLocus, history.MostRecentAlleleName);

            var identicalToAlleleName = AllelesInVersionOfHlaNom(hlaDatabaseVersion)
                .First(allele => allele.TypingEquals(mostRecentNameAsTyping))
                .IdenticalHla;

            return identicalToAlleleName;
        }
    }
}