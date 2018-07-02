using Nova.SearchAlgorithm.MatchingDictionary.Models.AlleleNames;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames
{
    internal class ReservedAlleleNamesExtractor : AlleleNamesExtractorBase
    {
        public ReservedAlleleNamesExtractor(AlleleNamesExtractorArgs extractorArgs) : base(extractorArgs)
        {
        }

        public override IEnumerable<AlleleNameEntry> GetAlleleNames()
        {
            return ExtractorArgs
                .AllelesInCurrentVersionOfHlaNom
                .Where(AlleleNameIsReserved)
                .Select(allele => new AlleleNameEntry(allele.Locus, allele.Name, allele.Name));
        }

        private bool AlleleNameIsReserved(HlaNom allele)
        {
            return allele.IsDeleted && AlleleNameIsNotInHistories(allele.Locus, allele.Name);
        }
    }
}
