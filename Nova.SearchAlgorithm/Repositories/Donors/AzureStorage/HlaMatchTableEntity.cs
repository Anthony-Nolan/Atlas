using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Models;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Repositories.Donors.AzureStorage
{
    /// <summary>
    /// A mapping from either PGroup or Serology to a matching DonorIDs.
    /// </summary>
    public class HlaMatchTableEntity : TableEntity
    {
        public string Locus { get; set; }
        public int TypePosition { get; set; }
        public string Name { get; set; }
        
        public int DonorId { get; set; }

        public HlaMatchTableEntity() { }

        public HlaMatchTableEntity(string partitionKey, string rowKey) : base(partitionKey, rowKey) {}

        public HlaMatchTableEntity(string locus, int typePosition, string matchName, int donorId) : base(GeneratePartitionKey(locus, typePosition, matchName), donorId.ToString())
        {
            Locus = locus;
            TypePosition = typePosition;
            Name = matchName;
            DonorId = donorId;
        }

        public static string GeneratePartitionKey(string locus, int typePosition, string matchName)
        {
            return string.Format("{0}_{1}-{2}", locus, typePosition, matchName);
        }
    }
}