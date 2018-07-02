using Nova.SearchAlgorithm.MatchingDictionary.Models.AlleleNames;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames
{
    internal abstract class AlleleNamesExtractorBase
    {
        protected readonly AlleleNamesExtractorArgs ExtractorArgs;

        protected AlleleNamesExtractorBase(AlleleNamesExtractorArgs extractorArgs)
        {
            ExtractorArgs = extractorArgs;
        }

        public abstract IEnumerable<AlleleNameEntry> GetAlleleNames();

        protected bool AlleleNameIsNotInHistories(string locus, string alleleName)
        {
            return !ExtractorArgs
                .HistoricalNamesAsTypings
                .Any(historicalTyping =>
                    historicalTyping.Locus.Equals(locus) && 
                    historicalTyping.Name.Equals(alleleName));
        }
    }
}
