using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Data.Models.DonorInfo
{
    /// <summary>
    /// A donor from our data source along with the donor's raw hla data.
    /// </summary>
    public class DonorResult
    {
        public int DonorId { get; set; }
        public DonorType DonorType { get; set; }
        public RegistryCode RegistryCode { get; set; }
        public bool IsAvailableForSearch { get; set; }
        public PhenotypeInfo<string> HlaNames { get; set; }
        public PhenotypeInfo<ExpandedHla> MatchingHla { get; set; }
    }
}