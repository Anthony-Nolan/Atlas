using Nova.SearchAlgorithm.Client.Models;

namespace Nova.SearchAlgorithm.Repositories.Donors.CosmosStorage
{
    public class PotentialHlaMatchRelationCosmosDocument
    {
        public string Id { get; set; }
        public Locus Locus { get; set; }
        public TypePositions MatchingTypePositions { get; set; }
        public string Name { get; set; }

        public int DonorId { get; set; }

        public PotentialHlaMatchRelation ToPotentialHlaMatchRelation(TypePositions searchPosition)
        {
            return new PotentialHlaMatchRelation
            {
                Locus = Locus,
                SearchTypePosition = searchPosition,
                MatchingTypePositions = MatchingTypePositions,
                Name = Name,
                DonorId = DonorId
            };
        }

        public void SetId(Locus locus, int donorId, string hla)
        {
            Id = GenerateId(locus, donorId, hla);
        }

        public static string GenerateId(Locus locus, int donorId, string hla)
        {
            return $"{locus}-{donorId}-{hla}";
        }
    }
}