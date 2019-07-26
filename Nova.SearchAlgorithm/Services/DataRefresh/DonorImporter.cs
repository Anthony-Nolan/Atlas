using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Nova.DonorService.Client.Models.SearchableDonors;
using Nova.SearchAlgorithm.Clients.Http;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Common.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Extensions;
using Nova.Utils.ApplicationInsights;

namespace Nova.SearchAlgorithm.Services.DataRefresh
{
    /// <summary>
    /// Responsible for fetching all eligible donors for the search algorithm
    /// </summary>
    public interface IDonorImporter
    {
        /// <summary>
        /// Fetches all donors with a higher id than the highest existing donor, and stores their data in the donor table
        /// Does not perform analysis of donor p-groups
        /// </summary>
        Task ImportDonors();
    }

    public class DonorImporter : IDonorImporter
    {
        private const int DonorPageSize = 100;

        private readonly IDataRefreshRepository dataRefreshRepository;
        private readonly IDonorImportRepository donorImportRepository;
        private readonly IDonorServiceClient donorServiceClient;
        private readonly ILogger logger;

        public DonorImporter(
            IDataRefreshRepository dataRefreshRepository,
            IDonorImportRepository donorImportRepository,
            IDonorServiceClient donorServiceClient,
            ILogger logger)
        {
            this.dataRefreshRepository = dataRefreshRepository;
            this.donorImportRepository = donorImportRepository;
            this.donorServiceClient = donorServiceClient;
            this.logger = logger;
        }

        public async Task ImportDonors()
        {
            try
            {
                await ContinueDonorImport();
            }
            catch (Exception ex)
            {
                logger.SendTrace($"Donor Import Failed: {ex.Message}", LogLevel.Error);
                throw new DonorImportHttpException("Unable to complete donor import: " + ex.Message, ex);
            }
        }

        private async Task ContinueDonorImport()
        {
            var nextId = await dataRefreshRepository.HighestDonorId();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var page = await FetchDonorPage(nextId);

            while (page.DonorsInfo.Any())
            {
                await donorImportRepository.InsertBatchOfDonors(page.DonorsInfo.Select(d => d.ToInputDonor()));

                stopwatch.Stop();
                logger.SendTrace("Imported donor batch", LogLevel.Info, new Dictionary<string, string>
                {
                    {"BatchSize", DonorPageSize.ToString()},
                    {"BatchImportTime", stopwatch.ElapsedMilliseconds.ToString()},
                });
                stopwatch.Reset();
                stopwatch.Start();

                nextId = page.LastId ?? await dataRefreshRepository.HighestDonorId();
                page = await FetchDonorPage(nextId);
            }

            logger.SendTrace("Donor import is complete", LogLevel.Info);
        }

        private async Task<SearchableDonorInformationPage> FetchDonorPage(int nextId)
        {
            logger.SendTrace($"Requesting donor page size {DonorPageSize} from ID {nextId} onwards", LogLevel.Trace);
            return await donorServiceClient.GetDonorsInfoForSearchAlgorithm(DonorPageSize, nextId);
        }
    }
}