using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    public class AlleleNameTableEntity : TableEntity
    {
        public string SerialisedCurrentAlleleNames { get; set; }

        public AlleleNameTableEntity() { }

        public AlleleNameTableEntity(MatchLocus matchLocus, string lookupName)
            : base(GetPartition(matchLocus), GetRowKey(lookupName))
        {
        }

        public static string GetPartition(MatchLocus matchLocus)
        {
            return matchLocus.ToString();
        }

        public MatchLocus GetMatchLocusFromPartitionKey()
        {
            return (MatchLocus)Enum.Parse(typeof(MatchLocus), PartitionKey);
        }

        public static string GetRowKey(string lookupName)
        {
            // row key is just the lookupName, but keep encapsulated in case of change
            return lookupName;
        }

        public string GetLookupNameFromRowKey()
        {
            // row key is just the lookupName, but keep encapsulated in case of change
            return RowKey;
        }
    }
}