using Atlas.DonorImport.Data.Models;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Models
{
    public static class PublishableDonorUpdateExtensions
    {
        public static PublishableDonorUpdate ToPublishableDonorUpdate(this SearchableDonorUpdate donorUpdate)
        {
            return new PublishableDonorUpdate { SearchableDonorUpdate = JsonConvert.SerializeObject(donorUpdate) };
        }

        public static SearchableDonorUpdate ToSearchableDonorUpdate(this PublishableDonorUpdate donorUpdate)
        {
            return JsonConvert.DeserializeObject<SearchableDonorUpdate>(donorUpdate.SearchableDonorUpdate);
        }
    }
}
