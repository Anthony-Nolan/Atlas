using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.AlleleNames;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames
{
    internal class MaintainedAlleleNamesExtractor : AlleleNamesExtractorBase
    {
        public MaintainedAlleleNamesExtractor(AlleleNamesExtractorArgs extractorArgs) : base(extractorArgs)
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
            return history.ToAlleleNameEntries(currentAlleleName);
        }

        private string GetCurrentAlleleName(AlleleNameHistory history)
        {
            return history.CurrentAlleleName ?? GetCurrentAlleleNameFromIdenticalToProperty(history);
        }

        private string GetCurrentAlleleNameFromIdenticalToProperty(AlleleNameHistory history)
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
