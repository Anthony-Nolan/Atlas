using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.Donors;

namespace Nova.SearchAlgorithm.Common.Models
{
    public class InputDonorWithExpandedHla
    {
        public int DonorId { get; set; }
        
        public DonorType DonorType { get; set; }
        public RegistryCode RegistryCode { get; set; }
        public PhenotypeInfo<ExpandedHla> MatchingHla { get; set; }

        public InputDonor ToRawInputDonor()
        {
            return new InputDonor
            {
                DonorId = DonorId,
                RegistryCode = RegistryCode,
                DonorType = DonorType,
                HlaNames = MatchingHla.Map((l, p, expandedHla) => expandedHla?.OriginalName)
            };
        }
    }
}