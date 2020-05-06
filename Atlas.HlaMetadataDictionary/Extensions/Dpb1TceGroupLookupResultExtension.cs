using Atlas.HlaMetadataDictionary.Models.Lookups.Dpb1TceGroupLookup;

namespace Atlas.HlaMetadataDictionary.Repositories.AzureStorage
{
    public static class Dpb1TceGroupLookupResultExtensions
    {
        public static IDpb1TceGroupsLookupResult ToDpb1TceGroupLookupResult(this HlaLookupTableEntity entity)
        {
            var tceGroup = entity.GetHlaInfo<string>();

            return new Dpb1TceGroupsLookupResult(
                entity.LookupName, 
                tceGroup);
        }
    }
}