using Nova.SearchAlgorithm.Client.Models;

namespace Nova.SearchAlgorithm.Data.Models
{
    public class InputDonor
    {
        public int DonorId { get; set; }
        
        public DonorType DonorType { get; set; }
        public RegistryCode RegistryCode { get; set; }
        public PhenotypeInfo<ExpandedHla> MatchingHla { get; set; }
    }
}