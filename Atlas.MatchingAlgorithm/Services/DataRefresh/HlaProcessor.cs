using Atlas.MatchingAlgorithm.ApplicationInsights;
using Atlas.MatchingAlgorithm.Common.Repositories;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Services.MatchingDictionary;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh
{
    public interface IHlaProcessor
    {
        /// <summary>
        /// For any donors with a higher id than the last updated donor:
        ///  - Fetches p-groups for all donor's hla
        ///  - Stores the pre-processed p-groups for use in matching
        /// </summary>
        Task UpdateDonorHla(string hlaDatabaseVersion);
    }

    public class HlaProcessor : IHlaProcessor
    {
        private const int BatchSize = 1000;
        private const string HlaFailureEventName = "Imported Donor Hla Processing Failure(s) in the Search Algorithm";

        private readonly ILogger logger;
        private readonly IDonorHlaExpander donorHlaExpander;
        private readonly IFailedDonorsNotificationSender failedDonorsNotificationSender;
        private readonly IAntigenCachingService antigenCachingService;
        private readonly IDonorImportRepository donorImportRepository;
        private readonly IDataRefreshRepository dataRefreshRepository;
        private readonly IHlaMatchingLookupRepository hlaMatchingLookupRepository;
        private readonly IAlleleNamesLookupRepository alleleNamesLookupRepository;
        private readonly IPGroupRepository pGroupRepository;

        public HlaProcessor(
            ILogger logger,
            IDonorHlaExpander donorHlaExpander,
            IFailedDonorsNotificationSender failedDonorsNotificationSender,
            IAntigenCachingService antigenCachingService,
            IDormantRepositoryFactory repositoryFactory,
            IHlaMatchingLookupRepository hlaMatchingLookupRepository,
            IAlleleNamesLookupRepository alleleNamesLookupRepository)
        {
            this.logger = logger;
            this.donorHlaExpander = donorHlaExpander;
            this.failedDonorsNotificationSender = failedDonorsNotificationSender;
            this.antigenCachingService = antigenCachingService;
            donorImportRepository = repositoryFactory.GetDonorImportRepository();
            dataRefreshRepository = repositoryFactory.GetDataRefreshRepository();
            pGroupRepository = repositoryFactory.GetPGroupRepository();
            this.hlaMatchingLookupRepository = hlaMatchingLookupRepository;
            this.alleleNamesLookupRepository = alleleNamesLookupRepository; }

        public async Task UpdateDonorHla(string hlaDatabaseVersion)
        {
            await PerformUpfrontSetup(hlaDatabaseVersion);

            try
            {
                await PerformHlaUpdate(hlaDatabaseVersion);
            }
            catch (Exception e)
            {
                logger.SendEvent(new HlaRefreshFailureEventModel(e));
                throw;
            }
            finally
            {
                await PerformTearDown();
            }
        }

        private async Task PerformHlaUpdate(string hlaDatabaseVersion)
        {
            var totalDonorCount = await dataRefreshRepository.GetDonorCount();
            var batchedQuery = await dataRefreshRepository.DonorsAddedSinceLastHlaUpdate(BatchSize);
            var donorsProcessed = 0;

            var failedDonors = new List<FailedDonorInfo>();

            while (batchedQuery.HasMoreResults)
            {
                var donorBatch = (await batchedQuery.RequestNextAsync()).ToList();

                // When continuing a donor import there will be some overlap of donors to ensure all donors are processed. 
                // This ensures we do not end up with duplicate p-groups in the matching hla tables
                // We do not want to attempt to remove p-groups for all batches as it would be detrimental to performance, so we limit it to the first two batches
                var shouldRemovePGroups = donorsProcessed < DataRefreshRepository.NumberOfBatchesOverlapOnRestart * BatchSize;

                var failedDonorsFromBatch = await UpdateDonorBatch(donorBatch, hlaDatabaseVersion, shouldRemovePGroups);
                failedDonors.AddRange(failedDonorsFromBatch);

                donorsProcessed += BatchSize;
                logger.SendTrace($"Hla Processing {(double)donorsProcessed / totalDonorCount:0.00%} complete", LogLevel.Info);
            }

            if (failedDonors.Any())
            {
                await failedDonorsNotificationSender.SendFailedDonorsAlert(failedDonors, HlaFailureEventName, Priority.Low);
            }
        }

        /// <summary>
        /// Fetches Expanded HLA information for all donors in a batch, and stores the processed  information in the database.
        /// </summary>
        /// <param name="donorBatch">The collection of donors to update</param>
        /// <param name="hlaDatabaseVersion">The version of the HLA database to use to fetch expanded HLA information</param>
        /// <param name="shouldRemovePGroups">If set, existing p-groups will be removed before adding new ones.</param>
        /// <returns>A collection of donors that failed the import process.</returns>
        private async Task<IEnumerable<FailedDonorInfo>> UpdateDonorBatch(
            IEnumerable<DonorInfo> donorBatch,
            string hlaDatabaseVersion,
            bool shouldRemovePGroups)
        {
            donorBatch = donorBatch.ToList();
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (shouldRemovePGroups)
            {
                await donorImportRepository.RemovePGroupsForDonorBatch(donorBatch.Select(d => d.DonorId));
            }

            var hlaExpansionResults = await donorHlaExpander.ExpandDonorHlaBatchAsync(donorBatch, HlaFailureEventName, hlaDatabaseVersion);
            await donorImportRepository.AddMatchingPGroupsForExistingDonorBatch(hlaExpansionResults.ProcessingResults);

            stopwatch.Stop();
            logger.SendTrace("Updated Donors", LogLevel.Verbose, new Dictionary<string, string>
            {
                {"NumberOfDonors", hlaExpansionResults.ProcessingResults.Count().ToString()},
                {"UpdateTime", stopwatch.ElapsedMilliseconds.ToString()}
            });

            return hlaExpansionResults.FailedDonors;
        }

        private async Task PerformUpfrontSetup(string hlaDatabaseVersion)
        {
            try
            {
                logger.SendTrace("HLA PROCESSOR: caching matching dictionary tables", LogLevel.Info);
                // Cloud tables are cached for performance reasons - this must be done upfront to avoid multiple tasks attempting to set up the cache
                await hlaMatchingLookupRepository.LoadDataIntoMemory(hlaDatabaseVersion);
                await alleleNamesLookupRepository.LoadDataIntoMemory(hlaDatabaseVersion);

                logger.SendTrace("HLA PROCESSOR: caching antigens from hla service", LogLevel.Info);
                // All antigens are fetched from the HLA service. We use our cache for NMDP lookups to avoid too much load on the hla service
                await antigenCachingService.GenerateAntigenCache();

                logger.SendTrace("HLA PROCESSOR: inserting new p groups to database", LogLevel.Info);
                // P Groups are inserted (when using relational database storage) upfront. All groups are extracted from the matching dictionary, and new ones added to the SQL database
                var pGroups = hlaMatchingLookupRepository.GetAllPGroups(hlaDatabaseVersion);
                pGroupRepository.InsertPGroups(pGroups);

                logger.SendTrace("HLA PROCESSOR: preparing database", LogLevel.Info);
                await donorImportRepository.FullHlaRefreshSetUp();
            }
            catch (Exception e)
            {
                logger.SendEvent(new HlaRefreshSetUpFailureEventModel(e));
                throw;
            }
        }

        private async Task PerformTearDown()
        {
            logger.SendTrace("HLA PROCESSOR: restoring database", LogLevel.Info);
            await donorImportRepository.FullHlaRefreshTearDown();
        }
    }
}