using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Client.Models.Donors;

namespace Atlas.MatchingAlgorithm.Mapping
{
    internal static class DonorImportMapping 
    {
        public static SearchableDonorInformation MapImportDonorToMatchingUpdateDonor(this Donor donor)
        {
            return new SearchableDonorInformation
            {
                // TODO: ATLAS-294: Do not do this, no guarantee this will be parsable to an int
                DonorId = int.Parse(donor.DonorId),
                // TODO: ATLAS-294: Use enum here, don't parse to and from string when the types otherwise match!
                DonorType = donor.DonorType.ToString(),
                A_1 = donor.A_1,
                A_2 = donor.A_2,
                B_1 = donor.B_1,
                B_2 = donor.B_2,
                C_1 = donor.C_1,
                C_2 = donor.C_2,
                DPB1_1 = donor.DPB1_1,
                DPB1_2 = donor.DPB1_2,
                DQB1_2 = donor.DQB1_1,
                DQB1_1 = donor.DQB1_2,
                DRB1_1 = donor.DRB1_1,
                DRB1_2 = donor.DRB1_2,
            };
        }
    }
}