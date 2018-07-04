using Nova.SearchAlgorithm.MatchingDictionary.Models.AlleleNames;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames
{
    internal class AlleleNamesFromHistoriesExtractor : AlleleNamesExtractorBase
    {
        public AlleleNamesFromHistoriesExtractor(AlleleNamesExtractorArgs extractorArgs) : base(extractorArgs)
        {
        }

        public override IEnumerable<AlleleNameEntry> GetAlleleNames()
        {
            return ExtractorArgs
                .AlleleNameHistories
                .SelectMany(GetAlleleNamesFromSingleHistory);
        }

        private IEnumerable<AlleleNameEntry> GetAlleleNamesFromSingleHistory(AlleleNameHistory history)
        {
            var currentAlleleName = GetCurrentAlleleName(history);

            return !string.IsNullOrEmpty(currentAlleleName)
                ? history.ToAlleleNameEntries(currentAlleleName)
                : new List<AlleleNameEntry>();
        }

        private string GetCurrentAlleleName(AlleleNameHistory history)
        {
            return history.CurrentAlleleName ?? GetIdenticalToAlleleName(history);
        }

        private string GetIdenticalToAlleleName(AlleleNameHistory history)
        {
            var mostRecentNameAsTyping = new HlaNom(
                TypingMethod.Molecular, history.Locus, history.MostRecentAlleleName);

            var identicalToAlleleName = ExtractorArgs
                .AllelesInCurrentVersionOfHlaNom
                .First(allele => allele.TypingEquals(mostRecentNameAsTyping))
                .IdenticalHla;

            return identicalToAlleleName;
        }
    }
}
