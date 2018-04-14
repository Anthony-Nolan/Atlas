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
        public int TypePositions { get; set; }
        public string Name { get; set; }
        
        public int DonorId { get; set; }

        public HlaMatchTableEntity() { }

        public HlaMatchTableEntity(string partitionKey, string rowKey) : base(partitionKey, rowKey) {}

        public HlaMatchTableEntity(string locus, TypePositions typePositions, string matchName, int donorId) : base(GeneratePartitionKey(locus, matchName), donorId.ToString())
        {
            Locus = locus;
            TypePositions = (int)typePositions;
            Name = matchName;
            DonorId = donorId;
        }

        public HlaMatchTableEntity(HlaMatch match) : this(match.Locus, match.MatchingTypePositions, match.Name, match.DonorId)
        {
        }

        public HlaMatch ToHlaMatch(TypePositions searchTypePosition)
        {
            return new HlaMatch()
            {
                Locus = Locus,
                Name = Name,
                SearchTypePosition = searchTypePosition,
                MatchingTypePositions = (TypePositions)TypePositions,
                DonorId = DonorId
            };
        }

        public static string GeneratePartitionKey(string locus, string matchName)
        {
            return string.Format("{0}_{1}", locus, matchName);
        }
    }
}