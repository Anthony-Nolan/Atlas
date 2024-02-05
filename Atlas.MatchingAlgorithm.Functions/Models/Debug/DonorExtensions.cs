using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Debug.Client.Models.DonorImport;
using Atlas.MatchingAlgorithm.Data.Models.Entities;

namespace Atlas.MatchingAlgorithm.Functions.Models.Debug
{
    internal static class DonorExtensions
    {
        public static DonorDebugInfo ToDonorDebugInfo(this Donor donor)
        {
            return new DonorDebugInfo
            {
                ExternalDonorCode = donor.ExternalDonorCode,
                DonorType = donor.DonorType.ToString(),
                RegistryCode = donor.RegistryCode,
                EthnicityCode = donor.EthnicityCode,
                Hla = donor.ToDonorInfo().HlaNames.ToPhenotypeInfoTransfer()
            };
        }
    }
}
