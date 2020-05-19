using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.Models.Lookups.AlleleNameLookup;
using Atlas.HlaMetadataDictionary.Repositories;

namespace Atlas.MatchingAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories
{
    public class FileBackedAlleleNamesLookupRepository :
        FileBackedHlaLookupRepositoryBase<IAlleleNameLookupResult>,
        IAlleleNamesLookupRepository
    {
        protected override IEnumerable<IAlleleNameLookupResult> GetHlaLookupResults(FileBackedHlaLookupResultCollections resultCollections)
        {
            return resultCollections.AlleleNameLookupResults;
        }

        public Task<IAlleleNameLookupResult> GetAlleleNameIfExists(Locus locus, string lookupName, string hlaDatabaseVersion)
        {
            return Task.FromResult(HlaLookupResults
                .SingleOrDefault(result => 
                    result.Locus == locus && 
                    result.LookupName.Equals(lookupName)));
        }
    }
}
