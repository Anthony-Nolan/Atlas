using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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

        private readonly IDataRefreshRepository repository;
        private readonly IDonorImportRepository donorImportRepository;
        private readonly IDonorServiceClient donorServiceClient;
        private readonly ILogger logger;

        public DonorImporter(
            IDataRefreshRepository repository,
            IDonorImportRepository donorImportRepository,
            IDonorServiceClient donorServiceClient,
            ILogger logger)
        {
            this.repository = repository;
            this.donorImportRepository = donorImportRepository;
            this.donorServiceClient = donorServiceClient;
            this.logger = logger;
        }

        public async Task ImportDonors()
        {
            try
            {
                await ContinueDonorImport(await repository.HighestDonorId());
            }
            catch (Exception ex)
            {
                logger.SendTrace($"Donor Import Failed: {ex.Message}", LogLevel.Error);
                throw new DonorImportHttpException("Unable to complete donor import: " + ex.Message, ex);
            }
        }

        private async Task ContinueDonorImport(int lastId)
        {
            var nextId = lastId;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            logger.SendTrace($"Requesting donor page size {DonorPageSize} from ID {nextId} onwards", LogLevel.Trace);
            var page = await donorServiceClient.GetDonorsInfoForSearchAlgorithm(DonorPageSize, nextId);

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

                logger.SendTrace($"Requesting donor page size {DonorPageSize} from ID {nextId} onwards", LogLevel.Trace);
                nextId = page.LastId ?? (await repository.HighestDonorId());
                page = await donorServiceClient.GetDonorsInfoForSearchAlgorithm(DonorPageSize, nextId);
            }

            logger.SendTrace("Donor import is complete", LogLevel.Info);
        }
    }
}