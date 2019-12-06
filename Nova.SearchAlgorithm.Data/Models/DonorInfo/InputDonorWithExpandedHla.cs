using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Data.Models.DonorInfo
{
    public class InputDonorWithExpandedHla
    {
        public int DonorId { get; set; }
        public DonorType DonorType { get; set; }
        public RegistryCode RegistryCode { get; set; }
        public PhenotypeInfo<ExpandedHla> MatchingHla { get; set; }
    }
}