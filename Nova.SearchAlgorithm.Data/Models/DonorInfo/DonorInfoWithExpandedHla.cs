using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Data.Models.DonorInfo
{
    public class DonorInfoWithExpandedHla : DonorInfo
    {
        public PhenotypeInfo<ExpandedHla> MatchingHla { get; set; }
    }
}