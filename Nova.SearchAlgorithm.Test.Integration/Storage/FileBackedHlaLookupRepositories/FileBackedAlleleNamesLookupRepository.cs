using Nova.SearchAlgorithm.Common.Models;
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

        public Task<IAlleleNameLookupResult> GetAlleleNameIfExists(Locus locus, string lookupName)
        {
            return Task.FromResult(HlaLookupResults
                .SingleOrDefault(result => 
                    result.Locus == locus && 
                    result.LookupName.Equals(lookupName)));
        }
    }
}
