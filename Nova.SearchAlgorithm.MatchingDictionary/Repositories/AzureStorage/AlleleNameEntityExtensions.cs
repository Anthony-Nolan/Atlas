using Newtonsoft.Json;
using Nova.SearchAlgorithm.MatchingDictionary.Models.AlleleNames;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    internal static class AlleleNameEntityExtensions
    {
        internal static AlleleNameTableEntity ToTableEntity(this AlleleNameEntry entry)
        {
            return new AlleleNameTableEntity(entry.MatchLocus, entry.LookupName)
            {
                SerialisedCurrentAlleleNames = JsonConvert.SerializeObject(entry.CurrentAlleleNames)
            };
        }

        internal static AlleleNameEntry ToAlleleNameEntry(this AlleleNameTableEntity result)
        {
            var matchLocus = result.GetMatchLocusFromPartitionKey();
            var lookupName = result.GetLookupNameFromRowKey();
            var currentAlleleNames = JsonConvert
                .DeserializeObject<IEnumerable<string>>(result.SerialisedCurrentAlleleNames);

            return new AlleleNameEntry(matchLocus, lookupName, currentAlleleNames);
        }
    }
}