using Nova.SearchAlgorithm.Client.Models;
using Nova.Utils.PhenotypeInfo;

namespace Nova.SearchAlgorithm.Data.Models.DonorInfo
{
    /// <summary>
    /// Contains all the information to create / update a donor within the search algorithm's database
    /// </summary>
    public class InputDonor
    {
        public int DonorId { get; set; }
        public DonorType DonorType { get; set; }
        public RegistryCode RegistryCode { get; set; }
        public PhenotypeInfo<string> HlaNames { get; set; }
    }
}