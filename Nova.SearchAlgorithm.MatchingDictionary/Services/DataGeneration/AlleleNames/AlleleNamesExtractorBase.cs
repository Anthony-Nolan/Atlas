using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.DataGeneration.AlleleNames
{
    public abstract class AlleleNamesExtractorBase
    {
        private readonly IWmdaDataRepository dataRepository;
        private readonly Dictionary<string, WmdaAlleleNames> wmdaAlleleNamesData = new Dictionary<string, WmdaAlleleNames>();

        protected AlleleNamesExtractorBase(IWmdaDataRepository dataRepository)
        {
            this.dataRepository = dataRepository;
        }

        protected IEnumerable<HlaNom> AllelesInVersionOfHlaNom(string hlaDatabaseVersion)
        {
            return GetWmdaAlleleNamesData(hlaDatabaseVersion).AllelesInVersionOfHlaNom;
        }
        
        protected bool AlleleNameIsNotInHistories(string locus, string alleleName, string hlaDatabaseVersion)
        {
            return !GetWmdaAlleleNamesData(hlaDatabaseVersion).HistoricalNamesAsTypings
                .Any(historicalTyping =>
                    historicalTyping.TypingLocus.Equals(locus) &&
                    historicalTyping.Name.Equals(alleleName)
                );
        }

        private WmdaAlleleNames GetWmdaAlleleNamesData(string hlaDatabaseVersion)
        {
            if (!wmdaAlleleNamesData.TryGetValue(hlaDatabaseVersion, out var data))
            {
                var dataset = dataRepository.GetWmdaDataset(hlaDatabaseVersion);
                data = new WmdaAlleleNames
                {
                    HistoricalNamesAsTypings = dataset
                        .AlleleNameHistories
                        .SelectMany(history =>
                            history.DistinctAlleleNames, (history, historicalName) =>
                            new HlaNom(TypingMethod.Molecular, history.TypingLocus, historicalName))
                        .Distinct()
                        .ToList(),
                    AllelesInVersionOfHlaNom = dataset.Alleles.ToList()
                };
                wmdaAlleleNamesData.Add(hlaDatabaseVersion, data);
            }

            return data;
        }
    }

    internal class WmdaAlleleNames
    {
        public List<HlaNom> AllelesInVersionOfHlaNom { get; set; }
        public List<HlaNom> HistoricalNamesAsTypings { get; set; }
    }
}