using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames
{
    public abstract class AlleleNamesExtractorBase
    {
        protected readonly List<HlaNom> AllelesInCurrentVersionOfHlaNom;
        private readonly List<HlaNom> historicalNamesAsTypings;

        protected AlleleNamesExtractorBase(IWmdaDataRepository dataRepository)
        {
            AllelesInCurrentVersionOfHlaNom = dataRepository.Alleles.ToList();

            historicalNamesAsTypings = dataRepository
                .AlleleNameHistories
                .SelectMany(history =>
                    history.DistinctAlleleNames, (history, historicalName) =>
                    new HlaNom(TypingMethod.Molecular, history.TypingLocus, historicalName))
                .Distinct()
                .ToList();
        }

        protected bool AlleleNameIsNotInHistories(string locus, string alleleName)
        {
            return !historicalNamesAsTypings
                .Any(historicalTyping =>
                    historicalTyping.TypingLocus.Equals(locus) && 
                    historicalTyping.Name.Equals(alleleName));
        }
    }
}
