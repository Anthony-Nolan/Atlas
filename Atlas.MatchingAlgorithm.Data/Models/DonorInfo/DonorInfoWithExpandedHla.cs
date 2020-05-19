using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;

namespace Atlas.MatchingAlgorithm.Data.Models.DonorInfo
{
    public class DonorInfoWithExpandedHla : DonorInfo
    {
        public PhenotypeInfo<IHlaMatchingLookupResult> MatchingHla { get; set; }
    }
}