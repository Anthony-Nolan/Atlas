using Newtonsoft.Json;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Repositories.Donors.CosmosStorage
{
    public class PotentialHlaMatchRelationCosmosDocument
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public Locus Locus { get; set; }
        public TypePositions MatchingTypePositions { get; set; }
        public string Name { get; set; }
        public DonorCosmosDocument Donor { get; set; }

        public int DonorId { get; set; }

        public PotentialHlaMatchRelation ToPotentialHlaMatchRelation(TypePositions searchPosition)
        {
            return new PotentialHlaMatchRelation
            {
                Locus = Locus,
                SearchTypePosition = searchPosition,
                MatchingTypePositions = MatchingTypePositions,
                Name = Name,
                DonorId = DonorId,
                Donor = Donor.ToDonorResult()
            };
        }
    }
}