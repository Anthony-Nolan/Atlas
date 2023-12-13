using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Models.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Donor = Atlas.DonorImport.Data.Models.Donor;

namespace Atlas.DonorImport.Helpers
{
    public static class SearchableDonorUpdateMapper
    {
        public static SearchableDonorUpdate MapToMatchingUpdateMessage(Donor updatedDonor)
        {
            return new SearchableDonorUpdate
            {
                DonorId = updatedDonor.AtlasId,
                IsAvailableForSearch = true, //Only false for deletions, which are handled separately
                SearchableDonorInformation = new SearchableDonorInformation
                {
                    DonorId = updatedDonor.AtlasId,
                    DonorType = updatedDonor.DonorType.ToMatchingAlgorithmType(),
                    ExternalDonorCode = updatedDonor.ExternalDonorCode,
                    EthnicityCode = updatedDonor.EthnicityCode,
                    RegistryCode = updatedDonor.RegistryCode,
                    A_1 = updatedDonor.A_1,
                    A_2 = updatedDonor.A_2,
                    B_1 = updatedDonor.B_1,
                    B_2 = updatedDonor.B_2,
                    C_1 = updatedDonor.C_1,
                    C_2 = updatedDonor.C_2,
                    DPB1_1 = updatedDonor.DPB1_1,
                    DPB1_2 = updatedDonor.DPB1_2,
                    DQB1_1 = updatedDonor.DQB1_1,
                    DQB1_2 = updatedDonor.DQB1_2,
                    DRB1_1 = updatedDonor.DRB1_1,
                    DRB1_2 = updatedDonor.DRB1_2,
                }
            };
        }

        public static SearchableDonorUpdate MapToDeletionUpdateMessage(int deletedDonorId)
        {
            return new SearchableDonorUpdate
            {
                DonorId = deletedDonorId,
                IsAvailableForSearch = false,
                SearchableDonorInformation = null
            };
        }
    }
}
