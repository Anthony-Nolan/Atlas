using Atlas.HlaMetadataDictionary.Repositories;
using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.AlleleNames
{
    internal abstract class AlleleNamesExtractorBase
    {
        private readonly IWmdaDataRepository dataRepository;
        private readonly Dictionary<string, HashSet<(string TypingLocus, string Name)>> historicAlleleNamesCache = new();

        protected AlleleNamesExtractorBase(IWmdaDataRepository dataRepository)
        {
            this.dataRepository = dataRepository;
        }

        protected IEnumerable<HlaNom> AllelesInVersionOfHlaNom(string hlaNomenclatureVersion)
        {
            return dataRepository.GetWmdaDataset(hlaNomenclatureVersion).Alleles;
        }

        protected bool AlleleNameIsNotInHistories(string locus, string alleleName, string hlaNomenclatureVersion)
        {
            return !GetHistoricNames(hlaNomenclatureVersion).Contains((locus, alleleName));
        }

        /// <remarks>
        /// A set, as every allele name variant is checked against all the historic names.
        /// </remarks>
        private HashSet<(string TypingLocus, string Name)> GetHistoricNames(string hlaNomenclatureVersion)
        {
            if (!historicAlleleNamesCache.TryGetValue(hlaNomenclatureVersion, out var data))
            {
                var dataset = dataRepository.GetWmdaDataset(hlaNomenclatureVersion);
                data = dataset
                    .AlleleNameHistories
                    .SelectMany(history => history.DistinctAlleleNames, (history, historicalName) => (history.TypingLocus, historicalName))
                    .ToHashSet();
                historicAlleleNamesCache.Add(hlaNomenclatureVersion, data);
            }

            return data;
        }
    }
}