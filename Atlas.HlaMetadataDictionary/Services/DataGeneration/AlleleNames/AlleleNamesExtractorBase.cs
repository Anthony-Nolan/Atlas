using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Models.Wmda;
using Atlas.HlaMetadataDictionary.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.AlleleNames
{
    internal abstract class AlleleNamesExtractorBase
    {
        private readonly IWmdaDataRepository dataRepository;
        private readonly Dictionary<string, IList<HlaNom>> historicAlleleNamesCache = new Dictionary<string, IList<HlaNom>>();

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
            return !GetHistoricNamesAsTypings(hlaNomenclatureVersion).Any(historicalTyping =>
                historicalTyping.TypingLocus.Equals(locus) &&
                historicalTyping.Name.Equals(alleleName)
            );
        }

        private IList<HlaNom> GetHistoricNamesAsTypings(string hlaNomenclatureVersion)
        {
            if (!historicAlleleNamesCache.TryGetValue(hlaNomenclatureVersion, out var data))
            {
                var dataset = dataRepository.GetWmdaDataset(hlaNomenclatureVersion);
                data = dataset
                    .AlleleNameHistories
                    .SelectMany(history =>
                        history.DistinctAlleleNames, (history, historicalName) =>
                        new HlaNom(TypingMethod.Molecular, history.TypingLocus, historicalName))
                    .Distinct()
                    .ToList();
                historicAlleleNamesCache.Add(hlaNomenclatureVersion, data);
            }

            return data;
        }
    }
}