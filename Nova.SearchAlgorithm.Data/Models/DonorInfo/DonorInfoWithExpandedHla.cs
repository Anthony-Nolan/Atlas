using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Data.Models.DonorInfo
{
    public class DonorInfoWithExpandedHla : InputDonor
    {
        public PhenotypeInfo<ExpandedHla> MatchingHla { get; set; }
    }
}