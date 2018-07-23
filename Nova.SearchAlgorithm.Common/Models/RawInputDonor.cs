using Nova.SearchAlgorithm.Client.Models;

namespace Nova.SearchAlgorithm.Common.Models
{
    public class RawInputDonor
    {
        public int DonorId { get; set; }
        
        public DonorType DonorType { get; set; }
        public RegistryCode RegistryCode { get; set; }
        public PhenotypeInfo<string> HlaNames { get; set; }
    }
}