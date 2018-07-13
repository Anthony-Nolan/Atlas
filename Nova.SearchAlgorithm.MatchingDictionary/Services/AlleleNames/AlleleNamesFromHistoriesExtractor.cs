using Nova.SearchAlgorithm.MatchingDictionary.Models.AlleleNames;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames
{
    public interface IAlleleNamesFromHistoriesExtractor
    {
        IEnumerable<AlleleNameLookupResult> GetAlleleNames();
    }

    public class AlleleNamesFromHistoriesExtractor : AlleleNamesExtractorBase, IAlleleNamesFromHistoriesExtractor
    {
        private readonly List<AlleleNameHistory> consolidatedAlleleNameHistories;

        public AlleleNamesFromHistoriesExtractor(
            IAlleleNameHistoriesConsolidator historiesConsolidator,
            IWmdaDataRepository dataRepository) 
            : base(dataRepository)
        {
            consolidatedAlleleNameHistories = historiesConsolidator.GetConsolidatedAlleleNameHistories().ToList();
        }

        public IEnumerable<AlleleNameLookupResult> GetAlleleNames()
        {
            return consolidatedAlleleNameHistories
                .SelectMany(GetAlleleNamesFromSingleHistory);
        }

        private IEnumerable<AlleleNameLookupResult> GetAlleleNamesFromSingleHistory(AlleleNameHistory history)
        {
            var currentAlleleName = GetCurrentAlleleName(history);

            return !string.IsNullOrEmpty(currentAlleleName)
                ? history.ToAlleleNameLookupResults(currentAlleleName)
                : new List<AlleleNameLookupResult>();
        }

        private string GetCurrentAlleleName(AlleleNameHistory history)
        {
            return history.CurrentAlleleName ?? GetIdenticalToAlleleName(history);
        }

        private string GetIdenticalToAlleleName(AlleleNameHistory history)
        {
            var mostRecentNameAsTyping = new HlaNom(
                TypingMethod.Molecular, history.Locus, history.MostRecentAlleleName);

            var identicalToAlleleName = AllelesInCurrentVersionOfHlaNom
                .First(allele => allele.TypingEquals(mostRecentNameAsTyping))
                .IdenticalHla;

            return identicalToAlleleName;
        }
    }
}
