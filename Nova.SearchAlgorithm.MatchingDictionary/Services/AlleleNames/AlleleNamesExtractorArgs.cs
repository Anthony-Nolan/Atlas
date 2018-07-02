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
        }
    }
}
