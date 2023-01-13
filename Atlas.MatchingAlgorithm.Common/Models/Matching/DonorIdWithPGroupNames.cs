using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

namespace Atlas.MatchingAlgorithm.Common.Models.Matching
{
    public class DonorIdWithPGroupNames
    {
        public int DonorId { get; set; }
        public PhenotypeInfo<IEnumerable<string>> PGroupNames { get; set; }
    }
}