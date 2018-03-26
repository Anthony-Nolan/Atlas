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
            // TODO: decide the partition and row key values - in the interm, using the search type and Random number, respectively
            return new DonorTableEntity(donor.DonorId, new Random().Next().ToString())
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