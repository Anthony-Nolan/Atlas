using AutoMapper;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Data.Models;
using System;

namespace Nova.SearchAlgorithm.Repositories.Donors.AzureStorage
{
    internal static class DonorExtensions
    {
        internal static DonorTableEntity ToTableEntity(this InputDonor donor, IMapper mapper)
        {
            return new DonorTableEntity(donor.RegistryCode.ToString(), donor.DonorId.ToString())
            {
                SerialisedDonor = JsonConvert.SerializeObject(mapper.Map<InputDonor>(donor)),
            };
        }

        internal static DonorResult ToRawDonor(this DonorTableEntity result, IMapper mapper)
        {
            var rawDonor = mapper.Map<DonorResult>(DeserializeRawDonor(result.SerialisedDonor));
            return rawDonor;
        }

        private static DonorResult DeserializeRawDonor(string serialisedDonor)
        {
            return JsonConvert.DeserializeObject<DonorResult>(serialisedDonor);
        }
    }
}