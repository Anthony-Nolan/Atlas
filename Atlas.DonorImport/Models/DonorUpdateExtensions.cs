﻿using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.ExternalInterface.Models;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Models
{
    public static class PublishableDonorUpdateExtensions
    {
        public static PublishableDonorUpdate ToPublishableDonorUpdate(this SearchableDonorUpdate donorUpdate)
        {
            return new PublishableDonorUpdate
            {
                DonorId = donorUpdate.DonorId,
                SearchableDonorUpdate = JsonConvert.SerializeObject(donorUpdate)
            };
        }

        public static SearchableDonorUpdate ToSearchableDonorUpdate(this PublishableDonorUpdate donorUpdate)
        {
            return JsonConvert.DeserializeObject<SearchableDonorUpdate>(donorUpdate.SearchableDonorUpdate);
        }
    }
}
