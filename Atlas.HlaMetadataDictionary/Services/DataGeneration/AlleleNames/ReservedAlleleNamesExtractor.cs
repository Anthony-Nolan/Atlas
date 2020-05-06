using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.AlleleNameLookup;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Wmda;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories;
using Atlas.MatchingAlgorithm.MatchingDictionary.Services.DataGeneration.AlleleNames;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Services.AlleleNames
{
    public interface IReservedAlleleNamesExtractor
    {
        IEnumerable<AlleleNameLookupResult> GetAlleleNames(string hlaDatabaseVersion);
    }

    public class ReservedAlleleNamesExtractor : AlleleNamesExtractorBase, IReservedAlleleNamesExtractor
    {
        public ReservedAlleleNamesExtractor(IWmdaDataRepository dataRepository)
            : base(dataRepository)
        {
        }

        public IEnumerable<AlleleNameLookupResult> GetAlleleNames(string hlaDatabaseVersion)
        {
            return AllelesInVersionOfHlaNom(hlaDatabaseVersion)
                .Where(a => AlleleNameIsReserved(a, hlaDatabaseVersion))
                .Select(allele => new AlleleNameLookupResult(allele.TypingLocus, allele.Name, allele.Name));
        }

        private bool AlleleNameIsReserved(HlaNom allele, string hlaDatabaseVersion)
        {
            return allele.IsDeleted && AlleleNameIsNotInHistories(allele.TypingLocus, allele.Name, hlaDatabaseVersion);
        }
    }
}
