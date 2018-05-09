using Nova.SearchAlgorithm.Client.Models;

namespace Nova.SearchAlgorithm.Data.Models
{
    public class SearchableDonor
    {
        public int DonorId { get; set; }
        public DonorType DonorType { get; set; }
        public RegistryCode RegistryCode { get; set; }

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