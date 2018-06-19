using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Repositories.Donors.AzureStorage
{
    /// <summary>
    /// A mapping from either PGroup or Serology to matching DonorIDs.
    /// </summary>
    public class PotentialHlaMatchRelationTableEntity : TableEntity
    {
        public Locus Locus { get; set; }
        public int TypePositions { get; set; }
        public string Name { get; set; }
        
        public int DonorId { get; set; }

        public PotentialHlaMatchRelationTableEntity() { }

        public PotentialHlaMatchRelationTableEntity(string partitionKey, string rowKey) : base(partitionKey, rowKey) {}

        public PotentialHlaMatchRelationTableEntity(Locus locus, TypePositions typePositions, string matchName, int donorId) : base(GeneratePartitionKey(donorId), GenerateRowKey(locus, matchName))
        {
            Locus = locus;
            TypePositions = (int)typePositions;
            Name = matchName;
            DonorId = donorId;
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

        public static string GeneratePartitionKey(int donorId)
        {
            return donorId.ToString();
        }

        public static string GenerateRowKey(Locus locus, string matchName)
        {
            return $"{locus.ToString().ToUpper()}_{matchName}";
        }
    }
}