using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.Data.Models.DonorInfo
{
    public class DonorInfoBatch
    {
        public IEnumerable<DonorInfo> Donors { get; set; }
    }
}