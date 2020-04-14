using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.Common.Models.Matching
{
    public class DonorIdWithPGroupNames
    {
        public int DonorId { get; set; }
        public PhenotypeInfo<IEnumerable<string>> PGroupNames { get; set; }
    }
}