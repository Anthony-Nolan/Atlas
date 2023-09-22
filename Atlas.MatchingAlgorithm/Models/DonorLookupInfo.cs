using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;

namespace Atlas.MatchingAlgorithm.Models
{
    public class DonorLookupInfo
    {
        public string ExternalDonorCode { get; set; }

        public string EthnicityCode { get; set; }

        public string RegistryCode { get; set; }
    }

    public static class DonorLookupExtensions
    {
        public static DonorLookupInfo ToDonorLookupInfo(this Donor donor)
            => new() 
            {
                ExternalDonorCode = donor.ExternalDonorCode,
                EthnicityCode = donor.EthnicityCode,
                RegistryCode = donor.RegistryCode
            };

        public static DonorLookupInfo ToDonorLookupInfo(this DonorInfo donor)
            => new()
            {
                ExternalDonorCode = donor.ExternalDonorCode,
                EthnicityCode = donor.EthnicityCode,
                RegistryCode = donor.RegistryCode
            };
    }
}
