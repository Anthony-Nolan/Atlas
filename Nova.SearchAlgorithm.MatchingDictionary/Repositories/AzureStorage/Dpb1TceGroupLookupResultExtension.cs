using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.Dpb1TceGroupLookup;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    public static class Dpb1TceGroupLookupResultExtensions
    {
        public static IDpb1TceGroupsLookupResult ToDpb1TceGroupLookupResult(this HlaLookupTableEntity entity)
        {
            var tceGroups = entity.GetHlaInfo<IEnumerable<string>>();

            return new Dpb1TceGroupsLookupResult(
                entity.LookupName, 
                tceGroups);
        }
    }
}