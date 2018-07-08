using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames
{
    public interface IAlleleNameHistoriesConsolidator
    {
        IEnumerable<AlleleNameHistory> GetConsolidatedAlleleNameHistories();
    }

    public class AlleleNameHistoriesConsolidator : IAlleleNameHistoriesConsolidator
    {
        private readonly List<AlleleNameHistory> alleleNameHistories;

        public AlleleNameHistoriesConsolidator(IWmdaDataRepository dataRepository)
        {
            alleleNameHistories = dataRepository.AlleleNameHistories.ToList();
        }

        public IEnumerable<AlleleNameHistory> GetConsolidatedAlleleNameHistories()
        {
            ConsolidateAlleleNameHistories();
            return alleleNameHistories;
        }

        private void ConsolidateAlleleNameHistories()
        {
            var alleleNamesListedInMultipleHistories = GetAlleleNamesListedInMultipleHistories().ToList();
            alleleNamesListedInMultipleHistories.ForEach(RetainCurrentHistoryAndRemoveAdditionalHistories);
        }

        private IEnumerable<IWmdaHlaTyping> GetAlleleNamesListedInMultipleHistories()
        {
            var alleleNamesListedMoreThanOnce = alleleNameHistories
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
                alleleNameHistories.RemoveAll(history => history.DistinctAlleleNamesContain(allele));
                alleleNameHistories.Add(currentHistory);
            }
        }

        private AlleleNameHistory GetFirstHistoryWhereCurrentNameIsNotNull(IWmdaHlaTyping alleleTyping)
        {
            return alleleNameHistories
                .FirstOrDefault(history =>
                    history.DistinctAlleleNamesContain(alleleTyping) &&
                    !string.IsNullOrEmpty(history.CurrentAlleleName));
        }
    }
}
