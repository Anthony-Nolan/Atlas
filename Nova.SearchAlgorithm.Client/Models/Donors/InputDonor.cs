using Nova.Utils.PhenoTypeInfo;

namespace Nova.SearchAlgorithm.Client.Models.Donors
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