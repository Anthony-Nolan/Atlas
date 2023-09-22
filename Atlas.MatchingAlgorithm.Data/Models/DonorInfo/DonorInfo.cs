using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Donors;

namespace Atlas.MatchingAlgorithm.Data.Models.DonorInfo
{
    /// <summary>
    /// Minimum information needed to create / update a donor within the search algorithm's database
    /// </summary>
    public class DonorInfo
    {
        public int DonorId { get; set; }
        public DonorType DonorType { get; set; }
        public PhenotypeInfo<string> HlaNames { get; set; }
        public bool IsAvailableForSearch { get; set; } = true;
        public string ExternalDonorCode { get; set; }
        public string EthnicityCode { get; set; }
        public string RegistryCode { get; set; }
    }
}