using Atlas.MatchingAlgorithm.Common.Models;

namespace Atlas.MatchingAlgorithm.Data.Models.DonorInfo
{
    public class DonorInfoWithExpandedHla : DonorInfo
    {
        public PhenotypeInfo<ExpandedHla> MatchingHla { get; set; }
    }
}