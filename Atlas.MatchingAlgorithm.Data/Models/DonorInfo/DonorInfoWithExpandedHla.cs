using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;

namespace Atlas.MatchingAlgorithm.Data.Models.DonorInfo
{
    public class DonorInfoWithExpandedHla : DonorInfo
    {
        public PhenotypeInfo<IHlaMatchingLookupResult> MatchingHla { get; set; }
    }
}