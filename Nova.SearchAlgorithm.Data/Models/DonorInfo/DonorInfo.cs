using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Data.Models.DonorInfo
{
    /// <summary>
    /// Minimum information needed to create / update a donor within the search algorithm's database
    /// </summary>
    public class DonorInfo
    {
        public int DonorId { get; set; }
        public DonorType DonorType { get; set; }
        public RegistryCode RegistryCode { get; set; }
        public PhenotypeInfo<string> HlaNames { get; set; }

        /// <summary>
        /// Defaults to true.
        /// </summary>
        public bool IsAvailableForSearch { get; set; } = true;
    }
}