using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Models;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Repositories.Donors.AzureStorage
{
    /// <summary>
    /// A mapping from either PGroup or Serology to matching DonorIDs.
    /// </summary>
    public class PotentialHlaMatchRelationTableEntity : TableEntity
    {
        public string Locus { get; set; }
        public int TypePositions { get; set; }
        public string Name { get; set; }
        
        public int DonorId { get; set; }

        public PotentialHlaMatchRelationTableEntity() { }

        public PotentialHlaMatchRelationTableEntity(string partitionKey, string rowKey) : base(partitionKey, rowKey) {}

        public PotentialHlaMatchRelationTableEntity(string locus, TypePositions typePositions, string matchName, int donorId) : base(GeneratePartitionKey(locus, matchName), donorId.ToString())
        {
            Locus = locus;
            TypePositions = (int)typePositions;
            Name = matchName;
            DonorId = donorId;
        }

        public PotentialHlaMatchRelationTableEntity(PotentialHlaMatchRelation match) : this(match.Locus, match.MatchingTypePositions, match.Name, match.DonorId)
        {
        }

        public PotentialHlaMatchRelation ToPotentialHlaMatchRelation(TypePositions searchTypePosition)
        {
            return new PotentialHlaMatchRelation()
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