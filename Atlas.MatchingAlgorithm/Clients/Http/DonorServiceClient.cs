using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Atlas.MatchingAlgorithm.Models;
using Atlas.Utils.Core.ApplicationInsights;
using Atlas.Utils.Core.Http;

namespace Atlas.MatchingAlgorithm.Clients.Http
{
    public interface IDonorServiceClient
    {
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
        Task<SearchableDonorInformationPage> GetDonorsInfoForSearchAlgorithm(int resultsPerPage, int lastId);
    }

    public class FileBasedDonorServiceClient : IDonorServiceClient
    {
        private readonly string filePath;
        private readonly ILogger logger;

        public FileBasedDonorServiceClient(string filePath, ILogger logger = null)
        {
            this.filePath = filePath;
            this.logger = logger;
        }

        public Task<SearchableDonorInformationPage> GetDonorsInfoForSearchAlgorithm(int resultsPerPage, int lastId)
        {
            var allDonors = ReadAllDonors();
            logger.SendTrace($"Read {allDonors.Count} donor records from file, rather than contacting remote service.", LogLevel.Trace);

            var lastDonorOnRecord = allDonors.Last().DonorId;

            if (lastId == lastDonorOnRecord)
            {
                return Task.FromResult(new SearchableDonorInformationPage
                {
                    DonorsInfo = new List<SearchableDonorInformation>(),
                    ResultsPerPage = resultsPerPage,
                    LastId = -1,
                });
            }

            var donorsToReturn =
                (lastId <= 0 ? allDonors : allDonors.SkipWhile(donor => donor.DonorId != lastId).Skip(1) )
                .Take(resultsPerPage)
                .ToList();

            if (!donorsToReturn.Any())
            {
                throw new ArgumentException("A positive LastId was provided, but did not match any any Donor in the list of DonorIds. Unclear how this could occur.", nameof(lastId));
            }

            return Task.FromResult(new SearchableDonorInformationPage
            {
                DonorsInfo = donorsToReturn,
                ResultsPerPage = donorsToReturn.Count,
                LastId = donorsToReturn.Last().DonorId,
            });
        }

        public List<SearchableDonorInformation> ReadAllDonors()
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Unable to find DonorOverride file.", filePath);
            }

            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, new Configuration {Quote = '\''}))
            {
                return csv.GetRecords<SearchableDonorInformation>().ToList();
            }
        }
    }

    public class DonorServiceClient : ClientBase, IDonorServiceClient
    {
        public DonorServiceClient(HttpClientSettings settings, ILogger logger = null, HttpMessageHandler handler = null, HttpErrorParser errorsParser = null) : base(settings, logger, handler, errorsParser)
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