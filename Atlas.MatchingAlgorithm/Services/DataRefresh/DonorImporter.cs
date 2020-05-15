using Atlas.MatchingAlgorithm.Clients.Http.DonorService;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.Common.Notifications;
using Polly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh
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
        private const string ImportFailureEventName = "Donor Import Failure(s) in the Search Algorithm";

        private readonly IDataRefreshRepository dataRefreshRepository;
        private readonly IDonorImportRepository donorImportRepository;
        private readonly IDonorServiceClient donorServiceClient;
        private readonly IDonorInfoConverter donorInfoConverter;
        private readonly IFailedDonorsNotificationSender failedDonorsNotificationSender;
        private readonly ILogger logger;

        public DonorImporter(
            IDormantRepositoryFactory repositoryFactory,
            IDonorServiceClient donorServiceClient,
            IDonorInfoConverter donorInfoConverter,
            IFailedDonorsNotificationSender failedDonorsNotificationSender,
            ILogger logger)
        {
            dataRefreshRepository = repositoryFactory.GetDataRefreshRepository();
            donorImportRepository = repositoryFactory.GetDonorImportRepository();
            this.donorServiceClient = donorServiceClient;
            this.donorInfoConverter = donorInfoConverter;
            this.failedDonorsNotificationSender = failedDonorsNotificationSender;
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

            var allFailedDonors = new List<FailedDonorInfo>();
            var page = await FetchDonorPage(nextId);

            while (page.DonorsInfo.Any())
            {
                var failedDonors = await InsertDonors(page.DonorsInfo, stopwatch);
                allFailedDonors.AddRange(failedDonors);

                nextId = page.LastId ?? await dataRefreshRepository.HighestDonorId();
                page = await FetchDonorPage(nextId);
            }

            await failedDonorsNotificationSender.SendFailedDonorsAlert(allFailedDonors, ImportFailureEventName, Priority.Medium);

            logger.SendTrace("Donor import is complete", LogLevel.Info);
        }

        private async Task<SearchableDonorInformationPage> FetchDonorPage(int nextId)
        {
            logger.SendTrace($"Requesting donor page size {DonorPageSize} from ID {nextId} onwards", LogLevel.Trace);

            const int retryCount = 5;
            var policy = Policy.Handle<Exception>().WaitAndRetryAsync(
                retryCount: retryCount,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(1000),
                onRetry: (e, t) =>
                {
                    logger.SendTrace(
                        $"Failed to fetch donors from Oracle with exception {e}. Retrying up to {retryCount} times.",
                        LogLevel.Error
                    );
                });
            return await policy.ExecuteAsync(async () => await donorServiceClient.GetDonorsInfoForSearchAlgorithm(DonorPageSize, nextId));
        }

        private async Task<IEnumerable<FailedDonorInfo>> InsertDonors(IEnumerable<SearchableDonorInformation> donors, Stopwatch stopwatch)
        {
            var donorInfoConversionResult = await donorInfoConverter.ConvertDonorInfoAsync(donors, ImportFailureEventName);
            await donorImportRepository.InsertBatchOfDonors(donorInfoConversionResult.ProcessingResults);

            stopwatch.Stop();
            logger.SendTrace("Imported donor batch", LogLevel.Info, new Dictionary<string, string>
            {
                {"BatchSize", DonorPageSize.ToString()},
                {"ImportedDonors", donorInfoConversionResult.ProcessingResults.Count().ToString()},
                {"BatchImportTime", stopwatch.ElapsedMilliseconds.ToString()}
            });
            stopwatch.Reset();
            stopwatch.Start();

            return donorInfoConversionResult.FailedDonors;
        }
    }
}