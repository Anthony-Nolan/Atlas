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

        internal static SearchableDonor ToSearchableDonor(this DonorTableEntity result, IMapper mapper)
        {
            var searchableDonor = mapper.Map<SearchableDonor>(DeserializeSearchableDonor(result.SerialisedDonor));
            return searchableDonor;
        }

        internal static RawDonor ToRawDonor(this DonorTableEntity result, IMapper mapper)
        {
            var searchableDonor = mapper.Map<RawDonor>(DeserializeRawDonor(result.SerialisedDonor));
            return searchableDonor;
        }

        private static SearchableDonor DeserializeSearchableDonor(string serialisedDonor)
        {
            return JsonConvert.DeserializeObject<SearchableDonor>(serialisedDonor);
        }

        private static RawDonor DeserializeRawDonor(string serialisedDonor)
        {
            return JsonConvert.DeserializeObject<RawDonor>(serialisedDonor);
        }
    }
}