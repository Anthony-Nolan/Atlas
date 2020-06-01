using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Extensions;
using Atlas.HlaMetadataDictionary.Models.Lookups.AlleleNameLookup;
using Atlas.HlaMetadataDictionary.Models.Wmda;
using Atlas.HlaMetadataDictionary.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.AlleleNames
{
    internal interface IAlleleNamesFromHistoriesExtractor
    {
        IEnumerable<AlleleNameLookupResult> GetAlleleNames(string hlaNomenclatureVersion);
    }

    internal class AlleleNamesFromHistoriesExtractor : AlleleNamesExtractorBase, IAlleleNamesFromHistoriesExtractor
    {
        private readonly IAlleleNameHistoriesConsolidator historiesConsolidator;

        private IEnumerable<AlleleNameHistory> ConsolidatedAlleleNameHistories(string hlaNomenclatureVersion)
        {
            return historiesConsolidator.GetConsolidatedAlleleNameHistories(hlaNomenclatureVersion).ToList();
        }

        public AlleleNamesFromHistoriesExtractor(
            IAlleleNameHistoriesConsolidator historiesConsolidator,
            IWmdaDataRepository dataRepository)
            : base(dataRepository)
        {
            this.historiesConsolidator = historiesConsolidator;
        }

        public IEnumerable<AlleleNameLookupResult> GetAlleleNames(string hlaNomenclatureVersion)
        {
            return ConsolidatedAlleleNameHistories(hlaNomenclatureVersion)
                .SelectMany(h => GetAlleleNamesFromSingleHistory(h, hlaNomenclatureVersion));
        }

        private IEnumerable<AlleleNameLookupResult> GetAlleleNamesFromSingleHistory(AlleleNameHistory history, string hlaNomenclatureVersion)
        {
            var currentAlleleName = GetCurrentAlleleName(history, hlaNomenclatureVersion);

            return !string.IsNullOrEmpty(currentAlleleName)
                ? history.ToAlleleNameLookupResults(currentAlleleName)
                : new List<AlleleNameLookupResult>();
        }

        private string GetCurrentAlleleName(AlleleNameHistory history, string hlaNomenclatureVersion)
        {
            return history.CurrentAlleleName ?? GetIdenticalToAlleleName(history, hlaNomenclatureVersion);
        }

        private string GetIdenticalToAlleleName(AlleleNameHistory history, string hlaNomenclatureVersion)
        {
            var mostRecentNameAsTyping = new HlaNom(
                TypingMethod.Molecular, history.TypingLocus, history.MostRecentAlleleName);

            var identicalToAlleleName = AllelesInVersionOfHlaNom(hlaNomenclatureVersion)
                .First(allele => allele.TypingEquals(mostRecentNameAsTyping))
                .IdenticalHla;

            return identicalToAlleleName;
        }
    }
}