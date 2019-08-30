using Nova.SearchAlgorithm.Client.Models;

namespace Nova.SearchAlgorithm.Common.Models
{
    public class InputDonorWithExpandedHla
    {
        public int DonorId { get; set; }
        
        public DonorType DonorType { get; set; }
        public RegistryCode RegistryCode { get; set; }
        public PhenotypeInfo<ExpandedHla> MatchingHla { get; set; }
    }
}