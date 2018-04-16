using Nova.SearchAlgorithm.Client.Models;

namespace Nova.SearchAlgorithm.Data.Models
{
    public class SearchableDonor
    {
        public int DonorId { get; set; }
        // TODO:NOVA-929 make donor types a strongly typed Enum

        public string DonorType { get; set; }
        public RegistryCode RegistryCode { get; set; }
        public PhenotypeInfo<MatchingHla> MatchingHla { get; set; }

        public PotentialMatch ToApiDonorMatch()
        {
            return new PotentialMatch
            {
                DonorId = DonorId,
                DonorType = DonorType,
                Registry = RegistryCode
            };
        }
    }
}