using Atlas.MatchingAlgorithm.Models;
using Atlas.Utils.Core.ApplicationInsights;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Clients.Http.DonorService
{
    public class DonorServiceClient : ClientBase, IDonorServiceClient
    {
        public DonorServiceClient(
            HttpClientSettings settings,
            ILogger logger = null,
            HttpMessageHandler handler = null,
            HttpErrorParser errorsParser = null) : base(settings, logger, handler, errorsParser)
        {
        }

        public async Task<SearchableDonorInformationPage> GetDonorsInfoForSearchAlgorithm(int resultsPerPage, int lastId)
        {
            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("resultsPerPage", resultsPerPage.ToString()),
                new KeyValuePair<string, string>("lastId", lastId.ToString()),
            };

            var request = GetRequest(HttpMethod.Get, "donors-info-for-search-algorithm", parameters);
            return await MakeRequestAsync<SearchableDonorInformationPage>(request);
        }
    }
}