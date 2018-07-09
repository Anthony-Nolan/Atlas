using Nova.SearchAlgorithm.MatchingDictionary.Models.AlleleNames;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames
{
    public interface IReservedAlleleNamesExtractor
    {
        IEnumerable<AlleleNameEntry> GetAlleleNames();
    }

    public class ReservedAlleleNamesExtractor : AlleleNamesExtractorBase, IReservedAlleleNamesExtractor
    {
        public ReservedAlleleNamesExtractor(IWmdaDataRepository dataRepository)
            : base(dataRepository)
        {
        }

        public IEnumerable<AlleleNameEntry> GetAlleleNames()
        {
            return AllelesInCurrentVersionOfHlaNom
                .Where(AlleleNameIsReserved)
                .Select(allele => new AlleleNameEntry(allele.Locus, allele.Name, allele.Name));
        }

        private bool AlleleNameIsReserved(HlaNom allele)
        {
            return allele.IsDeleted && AlleleNameIsNotInHistories(allele.Locus, allele.Name);
        }
    }
}
