using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.AlleleNameLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories
{
    public class FileBackedAlleleNamesLookupRepository :
        FileBackedHlaLookupRepositoryBase<IAlleleNameLookupResult>,
        IAlleleNamesLookupRepository
    {
        protected override IEnumerable<IAlleleNameLookupResult> GetHlaLookupResults(FileBackedHlaLookupResultCollections resultCollections)
        {
            return resultCollections.AlleleNameLookupResults;
        }

        public Task<IAlleleNameLookupResult> GetAlleleNameIfExists(MatchLocus matchLocus, string lookupName)
        {
            return Task.FromResult(HlaLookupResults
                .SingleOrDefault(result => 
                    result.MatchLocus == matchLocus && 
                    result.LookupName.Equals(lookupName)));
        }
    }
}
