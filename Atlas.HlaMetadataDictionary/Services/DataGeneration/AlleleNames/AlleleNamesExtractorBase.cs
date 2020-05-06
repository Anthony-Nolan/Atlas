using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.Wmda;
using Atlas.HlaMetadataDictionary.Repositories;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.AlleleNames
{
    public abstract class AlleleNamesExtractorBase
    {
        private readonly IWmdaDataRepository dataRepository;
        private readonly Dictionary<string, IList<HlaNom>> historicAlleleNamesCache = new Dictionary<string, IList<HlaNom>>();

        protected AlleleNamesExtractorBase(IWmdaDataRepository dataRepository)
        {
            this.dataRepository = dataRepository;
        }

        protected IEnumerable<HlaNom> AllelesInVersionOfHlaNom(string hlaDatabaseVersion)
        {
            return dataRepository.GetWmdaDataset(hlaDatabaseVersion).Alleles;
        }

        protected bool AlleleNameIsNotInHistories(string locus, string alleleName, string hlaDatabaseVersion)
        {
            return !GetHistoricNamesAsTypings(hlaDatabaseVersion).Any(historicalTyping =>
                historicalTyping.TypingLocus.Equals(locus) &&
                historicalTyping.Name.Equals(alleleName)
            );
        }

        private IList<HlaNom> GetHistoricNamesAsTypings(string hlaDatabaseVersion)
        {
            if (!historicAlleleNamesCache.TryGetValue(hlaDatabaseVersion, out var data))
            {
                var dataset = dataRepository.GetWmdaDataset(hlaDatabaseVersion);
                data = dataset
                    .AlleleNameHistories
                    .SelectMany(history =>
                        history.DistinctAlleleNames, (history, historicalName) =>
                        new HlaNom(TypingMethod.Molecular, history.TypingLocus, historicalName))
                    .Distinct()
                    .ToList();
                historicAlleleNamesCache.Add(hlaDatabaseVersion, data);
            }

            return data;
        }
    }
}