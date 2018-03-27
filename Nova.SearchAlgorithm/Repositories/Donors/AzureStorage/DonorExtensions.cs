using AutoMapper;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Models;
using System;

namespace Nova.SearchAlgorithm.Repositories.Donors.AzureStorage
{
    internal static class DonorExtensions
    {
        internal static DonorTableEntity ToTableEntity(this SearchableDonor donor, IMapper mapper)
        {
            return new DonorTableEntity(donor.RegistryCode, donor.DonorId)
            {
                SerialisedDonor = JsonConvert.SerializeObject(mapper.Map<SearchableDonor>(donor)),
            };
        }

        internal static SearchableDonor ToSearchableDonor(this DonorTableEntity result, IMapper mapper)
        {
            var searchableDonor = mapper.Map<SearchableDonor>(DeserializeDonor(result.SerialisedDonor));
            return searchableDonor;
        }

        private static SearchableDonor DeserializeDonor(string serialisedDonor)
        {
            return JsonConvert.DeserializeObject<SearchableDonor>(serialisedDonor);
        }
    }
}