using Newtonsoft.Json;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models;

namespace Nova.SearchAlgorithm.Repositories.Donors.AzureStorage
{
    internal static class DonorTableEntityExtensions
    {
        internal static DonorTableEntity ToTableEntity(this RawInputDonor donor)
        {
            return new DonorTableEntity(donor.RegistryCode.ToString(), donor.DonorId.ToString())
            {
                DonorId = donor.DonorId,
                RegistryCode = (int)donor.RegistryCode,
                DonorType = (int)donor.DonorType,
                A_1 = donor.HlaNames?.A_1,
                A_2 = donor.HlaNames?.A_2,
                B_1 = donor.HlaNames?.B_1,
                B_2 = donor.HlaNames?.B_2,
                C_1 = donor.HlaNames?.C_1,
                C_2 = donor.HlaNames?.C_2,
                DRB1_1 = donor.HlaNames?.DRB1_1,
                DRB1_2 = donor.HlaNames?.DRB1_2,
                DQB1_1 = donor.HlaNames?.DQB1_1,
                DQB1_2 = donor.HlaNames?.DQB1_2
            };
        }

        internal static DonorTableEntity ToTableEntity(this InputDonor donor)
        {
            var hlaNames = donor.ToRawInputDonor().HlaNames;

            return new DonorTableEntity(donor.RegistryCode.ToString(), donor.DonorId.ToString())
            {
                DonorId = donor.DonorId,
                RegistryCode = (int)donor.RegistryCode,
                DonorType = (int)donor.DonorType,
                A_1 = hlaNames.A_1,
                A_2 = hlaNames.A_2,
                B_1 = hlaNames.B_1,
                B_2 = hlaNames.B_2,
                C_1 = hlaNames.C_1,
                C_2 = hlaNames.C_2,
                DRB1_1 = hlaNames.DRB1_1,
                DRB1_2 = hlaNames.DRB1_2,
                DQB1_1 = hlaNames.DQB1_1,
                DQB1_2 = hlaNames.DQB1_2,
                ExpandedA_1 = SerialiseHlaDetails(donor.MatchingHla?.A_1),
                ExpandedA_2 = SerialiseHlaDetails(donor.MatchingHla?.A_2),
                ExpandedB_1 = SerialiseHlaDetails(donor.MatchingHla?.B_1),
                ExpandedB_2 = SerialiseHlaDetails(donor.MatchingHla?.B_2),
                ExpandedC_1 = SerialiseHlaDetails(donor.MatchingHla?.C_1),
                ExpandedC_2 = SerialiseHlaDetails(donor.MatchingHla?.C_2),
                ExpandedDRB1_1 = SerialiseHlaDetails(donor.MatchingHla?.DRB1_1),
                ExpandedDRB1_2 = SerialiseHlaDetails(donor.MatchingHla?.DRB1_2),
                ExpandedDQB1_1 = SerialiseHlaDetails(donor.MatchingHla?.DQB1_1),
                ExpandedDQB1_2 = SerialiseHlaDetails(donor.MatchingHla?.DQB1_2)
            };
        }

        internal static DonorResult ToDonorResult(this DonorTableEntity result)
        {
            var donorResult = new DonorResult
            {
                DonorId = result.DonorId,
                RegistryCode = (RegistryCode)result.RegistryCode,
                DonorType = (DonorType)result.DonorType,
                HlaNames = new PhenotypeInfo<string>
                {
                    A_1 = result.A_1,
                    A_2 = result.A_2,
                    B_1 = result.B_1,
                    B_2 = result.B_2,
                    C_1 = result.C_1,
                    C_2 = result.C_2,
                    DRB1_1 = result.DRB1_1,
                    DRB1_2 = result.DRB1_2,
                    DQB1_1 = result.DQB1_1,
                    DQB1_2 = result.DQB1_2
                },
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = DeserializeHlaDetails(result.ExpandedA_1),
                    A_2 = DeserializeHlaDetails(result.ExpandedA_2),
                    B_1 = DeserializeHlaDetails(result.ExpandedB_1),
                    B_2 = DeserializeHlaDetails(result.ExpandedB_2),
                    C_1 = DeserializeHlaDetails(result.ExpandedC_1),
                    C_2 = DeserializeHlaDetails(result.ExpandedC_2),
                    DRB1_1 = DeserializeHlaDetails(result.ExpandedDRB1_1),
                    DRB1_2 = DeserializeHlaDetails(result.ExpandedDRB1_2),
                    DQB1_1 = DeserializeHlaDetails(result.ExpandedDQB1_1),
                    DQB1_2 = DeserializeHlaDetails(result.ExpandedDQB1_2)
                }
            };
            return donorResult;
        }

        private static ExpandedHla DeserializeHlaDetails(string serialisedHlaDetails)
        {
            return serialisedHlaDetails == null ? null : JsonConvert.DeserializeObject<ExpandedHla>(serialisedHlaDetails);
        }

        private static string SerialiseHlaDetails(ExpandedHla hlaDetails)
        {
            return JsonConvert.SerializeObject(hlaDetails);
        }
    }
}