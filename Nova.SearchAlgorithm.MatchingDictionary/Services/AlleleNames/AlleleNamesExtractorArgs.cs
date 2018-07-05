using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames
{
    internal class AlleleNamesExtractorArgs
    {
        public List<AlleleNameHistory> AlleleNameHistories { get; }
        public List<HlaNom> AllelesInCurrentVersionOfHlaNom { get; }
        public List<HlaNom> HistoricalNamesAsTypings { get; }

        public AlleleNamesExtractorArgs(
            IEnumerable<AlleleNameHistory> alleleNameHistories,
            IEnumerable<HlaNom> allelesInCurrentVersionOfHlaNom)
        {
            // enumerate collections here as they will be queried thousands of times
            AlleleNameHistories = alleleNameHistories.ToList();
            AllelesInCurrentVersionOfHlaNom = allelesInCurrentVersionOfHlaNom.ToList();

            HistoricalNamesAsTypings = (
                from history in AlleleNameHistories
                from historicalName in history.DistinctAlleleNames
                select new HlaNom(TypingMethod.Molecular, history.Locus, historicalName)
                ).ToList();

            HandleAlleleNamesListedInMultipleHistories();
        }

        private void HandleAlleleNamesListedInMultipleHistories()
        {
            var alleleNamesListedInMultipleHistories = GetAlleleNamesListedInMultipleHistories().ToList();
            alleleNamesListedInMultipleHistories.ForEach(RetainCurrentHistoryAndRemoveAdditionalHistories);
        }

        private IEnumerable<IWmdaHlaTyping> GetAlleleNamesListedInMultipleHistories()
        {
            var alleleNamesListedMoreThanOnce = AlleleNameHistories
                .SelectMany(
                    history => history.DistinctAlleleNames,
                    (history, alleleName) => new { history.Locus, HlaId = history.Name, AlleleName = alleleName })
                .GroupBy(name => new HlaNom(TypingMethod.Molecular, name.Locus, name.AlleleName))
                .Where(grouped => grouped.Count() > 1)
                .Select(grouped => grouped.Key)
                .Distinct();

            return alleleNamesListedMoreThanOnce;
        }

        private void RetainCurrentHistoryAndRemoveAdditionalHistories(IWmdaHlaTyping allele)
        {
            var currentHistory = GetFirstHistoryWhereCurrentNameIsNotNull(allele);

            if (currentHistory != null)
            {
                AlleleNameHistories.RemoveAll(history => history.DistinctAlleleNamesContain(allele));
                AlleleNameHistories.Add(currentHistory);
            }
        }

        private AlleleNameHistory GetFirstHistoryWhereCurrentNameIsNotNull(IWmdaHlaTyping alleleTyping)
        {
            return AlleleNameHistories
                .FirstOrDefault(history =>
                    history.DistinctAlleleNamesContain(alleleTyping) &&
                    !string.IsNullOrEmpty(history.CurrentAlleleName));
        }
    }
}
