using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    public interface IAlleleNamesService
    {
        IEnumerable<AlleleNameEntry> GetAlleleNames();
    }

    public class AlleleNamesService : IAlleleNamesService
    {
        private readonly List<AlleleNameHistory> alleleNameHistories;
        private readonly IQueryable<HlaNom> allelesListedInCurrentVersionOfHlaNom;

        public AlleleNamesService(IWmdaDataRepository dataRepository)
        {
            alleleNameHistories = dataRepository.AlleleNameHistories.ToList();
            allelesListedInCurrentVersionOfHlaNom = dataRepository.Alleles.AsQueryable();
        }

        public IEnumerable<AlleleNameEntry> GetAlleleNames()
        {
            return alleleNameHistories.SelectMany(GetAlleleNamesFromHistory);
        }

        private IEnumerable<AlleleNameEntry> GetAlleleNamesFromHistory(AlleleNameHistory history)
        {
            return history.TryToAlleleNameEntries(out var entries) 
                ? entries 
                : GetAlleleNameEntriesUsingIdenticalToAlleleName(history);
        }

        private IEnumerable<AlleleNameEntry> GetAlleleNameEntriesUsingIdenticalToAlleleName(AlleleNameHistory history)
        {
            var identicalToAlleleName = GetAlleleNameFromIdenticalToProperty(history);
            return history.ToAlleleNameEntries(identicalToAlleleName);
        }

        private string GetAlleleNameFromIdenticalToProperty(AlleleNameHistory history)
        {
            var mostRecentNameAsAllele = new HlaNom(
                TypingMethod.Molecular, history.Locus, history.MostRecentAlleleName);
            
            var identicalToAlleleName = allelesListedInCurrentVersionOfHlaNom
                .First(allele => allele.TypingEquals(mostRecentNameAsAllele))
                .IdenticalHla;

            return identicalToAlleleName;
        }
    }
}
