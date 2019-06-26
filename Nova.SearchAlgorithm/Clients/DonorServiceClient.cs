using Newtonsoft.Json;
using Nova.DonorService.Client.Models.DonorInfoForSearchAlgorithm;
using Nova.DonorService.SearchAlgorithm.Models.DonorInfoForSearchAlgorithm;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Client;
using Nova.Utils.Http;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Clients
{
    public interface IDonorServiceClient
    {
        /// <summary>
        /// Returns information required for the new search algorithm for a single donor.
        /// Only contains information required to perform search and not for display in the frontend.
        /// </summary>
        Task<DonorInfoForSearchAlgorithm> GetDonorInfoForSearchAlgorithm(int donorId);

        /// <summary>
        /// Returns a page of donors information which is required for the new search algorithm.
        /// These donors Info only contain the information required to do a search with the new algorithm and not for display in the frontend.
        /// Useful for any client wishing to process all donors information one page at a time.
        /// </summary>
        /// <param name="resultsPerPage">The number of donors required per page</param>
        /// <param name="lastId">The last ID of the previous page. This pagination system is to make sure
        /// that any client paging through donors won't miss out if donors are inserted or deleted in-between page requests.
        /// If null or omitted, the first page of results will be returned.</param>
        /// <returns>A page of donors Info for search algorithm</returns>
        Task<DonorInfoForSearchAlgorithmPage> GetDonorsInfoForSearchAlgorithm(int resultsPerPage, int? lastId = null);

        /// <summary>
        /// Returns all donors whose personal details have changed since the provided date.
        /// </summary>
        /// <param name="dateSince">Donors who have changed on or since this date will be returned.</param>
        /// <returns>A list of donors.</returns>
        Task<List<DonorInfoForSearchAlgorithm>> GetDonorsInfoForSearchAlgorithmChangedSince(DateTime dateSince);

        /// <summary>
        /// Returns all donors info for the new search algorithm whose HLA details have changed since the provided date.
        /// </summary>
        /// <param name="dateSince">Donors info who have changed on or since this date will be returned.</param>
        /// <returns>A list of donors info for the new search algorithm.</returns>
        Task<List<DonorInfoForSearchAlgorithm>> GetDonorsInfoHlaChangedSince(DateTime dateSince);
    }

    public class DonorServiceClient : ClientBase, IDonorServiceClient
    {
        public DonorServiceClient(string baseUrl, string apiKey, string clientName, JsonSerializerSettings jsonSettings) 
            : base(new ClientSettings{ApiKey = apiKey, BaseUrl = baseUrl, ClientName = "search_algorithm__donor_service_client", JsonSettings = jsonSettings})
        {
        }

        public DonorServiceClient(ClientSettings settings, ILogger logger = null, HttpMessageHandler handler = null, IErrorsParser errorsParser = null) : base(settings, logger, handler, errorsParser)
        {
        }

        public async Task<DonorInfoForSearchAlgorithm> GetDonorInfoForSearchAlgorithm(int donorId)
        {
            var request = GetRequest(HttpMethod.Get, $"donors-info-for-search-algorithm/{donorId}");
            return await MakeRequestAsync<DonorInfoForSearchAlgorithm>(request);
        }

        public async Task<DonorInfoForSearchAlgorithmPage> GetDonorsInfoForSearchAlgorithm(int resultsPerPage, int? lastId = null)
        {
            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("resultsPerPage", resultsPerPage.ToString()),
            };
            if (lastId.HasValue)
            {
                parameters.Add(new KeyValuePair<string, string>("lastId", lastId.Value.ToString()));
            }

            var request = GetRequest(HttpMethod.Get, "donors-info-for-search-algorithm", parameters);
            return await MakeRequestAsync<DonorInfoForSearchAlgorithmPage>(request);
        }

        public async Task<List<DonorInfoForSearchAlgorithm>> GetDonorsInfoForSearchAlgorithmChangedSince(DateTime dateSince)
        {
            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("dateSince", dateSince.ToString())
            };
            var request = GetRequest(HttpMethod.Get, "donors-info-for-search-algorithm/changed-since", parameters);
            return await MakeRequestAsync<List<DonorInfoForSearchAlgorithm>>(request);
        }

        public async Task<List<DonorInfoForSearchAlgorithm>> GetDonorsInfoHlaChangedSince(DateTime dateSince)
        {
            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("dateSince", dateSince.ToString())
            };
            var request = GetRequest(HttpMethod.Get, "donors-info-for-search-algorithm/hla-changed-since", parameters);
            return await MakeRequestAsync<List<DonorInfoForSearchAlgorithm>>(request);
        }
    }
}