using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Debug.Client.Models.DonorImport;
using Atlas.DonorImport.Data.Models;

namespace Atlas.DonorImport.Functions.Models.Debug
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
                Hla = new PhenotypeInfo<string>(
                        valueA_1: donor.A_1,
                        valueA_2: donor.A_2,
                        valueB_1: donor.B_1,
                        valueB_2: donor.B_2,
                        valueC_1: donor.C_1,
                        valueC_2: donor.C_2,
                        valueDpb1_1: donor.DPB1_1,
                        valueDpb1_2: donor.DPB1_2,
                        valueDqb1_1: donor.DQB1_1,
                        valueDqb1_2: donor.DQB1_2,
                        valueDrb1_1: donor.DRB1_1,
                        valueDrb1_2: donor.DRB1_2)
                    .ToPhenotypeInfoTransfer()
            };
        }
    }
}
