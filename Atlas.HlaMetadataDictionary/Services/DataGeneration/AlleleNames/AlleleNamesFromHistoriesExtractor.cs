using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Extensions;
using Atlas.HlaMetadataDictionary.Repositories;
using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.AlleleNames
{
    internal interface IAlleleNamesFromHistoriesExtractor
    {
        IEnumerable<AlleleNameMetadata> GetAlleleNames(string hlaNomenclatureVersion);
    }

    internal class AlleleNamesFromHistoriesExtractor : AlleleNamesExtractorBase, IAlleleNamesFromHistoriesExtractor
    {
        private readonly IAlleleNameHistoriesConsolidator historiesConsolidator;

        public AlleleNamesFromHistoriesExtractor(
            IAlleleNameHistoriesConsolidator historiesConsolidator,
            IWmdaDataRepository dataRepository)
            : base(dataRepository)
        {
            this.historiesConsolidator = historiesConsolidator;
        }

        public IEnumerable<AlleleNameMetadata> GetAlleleNames(string hlaNomenclatureVersion)
        {
            return ConsolidatedAlleleNameHistories(hlaNomenclatureVersion)
                .SelectMany(h => GetAlleleNamesFromSingleHistory(h, hlaNomenclatureVersion));
        }

        private IEnumerable<AlleleNameHistory> ConsolidatedAlleleNameHistories(string hlaNomenclatureVersion)
        {
            return historiesConsolidator.GetConsolidatedAlleleNameHistories(hlaNomenclatureVersion).ToList();
        }

        private IEnumerable<AlleleNameMetadata> GetAlleleNamesFromSingleHistory(AlleleNameHistory history, string hlaNomenclatureVersion)
        {
            var currentAlleleName = GetCurrentAlleleName(history, hlaNomenclatureVersion);

            return !string.IsNullOrEmpty(currentAlleleName)
                ? history.ToAlleleNameMetadata(currentAlleleName)
                : new List<AlleleNameMetadata>();
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