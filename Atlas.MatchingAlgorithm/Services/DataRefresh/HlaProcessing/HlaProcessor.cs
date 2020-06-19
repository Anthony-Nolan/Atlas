using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.ApplicationInsights;
using Atlas.MatchingAlgorithm.Common.Repositories;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.HlaProcessing
{
    public interface IHlaProcessor
    {
        /// <summary>
        /// For any donors with a higher id than the last updated donor:
        ///  - Fetches p-groups for all donor's hla
        ///  - Stores the pre-processed p-groups for use in matching
        /// </summary>
        Task UpdateDonorHla(string hlaNomenclatureVersion);
    }

    public class HlaProcessor : IHlaProcessor
    {
        private const int BatchSize = 1000;
        private const string HlaFailureEventName = "Imported Donor Hla Processing Failure(s) in the Search Algorithm";

        private readonly ILogger logger;
        private readonly IDonorHlaExpanderFactory donorHlaExpanderFactory;
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;
        private readonly IFailedDonorsNotificationSender failedDonorsNotificationSender;
        private readonly IMacDictionary macDictionary;
        private readonly IDonorImportRepository donorImportRepository;
        private readonly IDataRefreshRepository dataRefreshRepository;
        private readonly IPGroupRepository pGroupRepository;

        public HlaProcessor(
            ILogger logger,
            IDonorHlaExpanderFactory donorHlaExpanderFactory,
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory,
            IFailedDonorsNotificationSender failedDonorsNotificationSender,
            IMacDictionary macDictionary,
            IDormantRepositoryFactory repositoryFactory)
        {
            this.logger = logger;
            this.donorHlaExpanderFactory = donorHlaExpanderFactory;
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
            this.failedDonorsNotificationSender = failedDonorsNotificationSender;
            this.macDictionary = macDictionary;
            donorImportRepository = repositoryFactory.GetDonorImportRepository();
            dataRefreshRepository = repositoryFactory.GetDataRefreshRepository();
            pGroupRepository = repositoryFactory.GetPGroupRepository();
        }

        public async Task UpdateDonorHla(string hlaNomenclatureVersion)
        {
            await PerformUpfrontSetup(hlaNomenclatureVersion);

            try
            {
                await PerformHlaUpdate(hlaNomenclatureVersion);
            }
            catch (Exception e)
            {
                logger.SendEvent(new HlaRefreshFailureEventModel(e));
                throw;
            }
        }

        private async Task PerformHlaUpdate(string hlaNomenclatureVersion)
        {
            var totalDonorCount = await dataRefreshRepository.GetDonorCount();
            var batchedQuery = await dataRefreshRepository.DonorsAddedSinceLastHlaUpdate(BatchSize);
            var donorsProcessed = 0;

            var failedDonors = new List<FailedDonorInfo>();

            while (batchedQuery.HasMoreResults)
            {
                var donorBatch = (await batchedQuery.RequestNextAsync()).ToList();
                var donorsInBatch = donorBatch.Count;

                // When continuing a donor import there will be some overlap of donors to ensure all donors are processed. 
                // This ensures we do not end up with duplicate p-groups in the matching hla tables
                // We do not want to attempt to remove p-groups for all batches as it would be detrimental to performance, so we limit it to the first two batches
                var shouldRemovePGroups = donorsProcessed < DataRefreshRepository.NumberOfBatchesOverlapOnRestart * BatchSize;

                var failedDonorsFromBatch = await UpdateDonorBatch(donorBatch, hlaNomenclatureVersion, shouldRemovePGroups);
                failedDonors.AddRange(failedDonorsFromBatch);

                donorsProcessed += donorsInBatch;
                logger.SendTrace($"Hla Processing {Decimal.Divide(donorsProcessed, totalDonorCount):0.00%} complete");
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
        /// <param name="hlaNomenclatureVersion">The version of the HLA Nomenclature to use to fetch expanded HLA information</param>
        /// <param name="shouldRemovePGroups">If set, existing p-groups will be removed before adding new ones.</param>
        /// <returns>A collection of donors that failed the import process.</returns>
        private async Task<IEnumerable<FailedDonorInfo>> UpdateDonorBatch(
            IEnumerable<DonorInfo> donorBatch,
            string hlaNomenclatureVersion,
            bool shouldRemovePGroups)
        {
            donorBatch = donorBatch.ToList();
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (shouldRemovePGroups)
            {
                await donorImportRepository.RemovePGroupsForDonorBatch(donorBatch.Select(d => d.DonorId));
            }

            var donorHlaExpander = donorHlaExpanderFactory.BuildForSpecifiedHlaNomenclatureVersion(hlaNomenclatureVersion);
            var hlaExpansionResults = await donorHlaExpander.ExpandDonorHlaBatchAsync(donorBatch, HlaFailureEventName);
            await donorImportRepository.AddMatchingPGroupsForExistingDonorBatch(hlaExpansionResults.ProcessingResults);

            stopwatch.Stop();
            logger.SendTrace("Updated Donors", LogLevel.Verbose, new Dictionary<string, string>
            {
                {"NumberOfDonors", hlaExpansionResults.ProcessingResults.Count().ToString()},
                {"UpdateTime", stopwatch.ElapsedMilliseconds.ToString()}
            });

            return hlaExpansionResults.FailedDonors;
        }

        private async Task PerformUpfrontSetup(string hlaNomenclatureVersion)
        {
            try
            {

                logger.SendTrace("HLA PROCESSOR: caching HlaMetadataDictionary tables", LogLevel.Info);
                // Cloud tables are cached for performance reasons - this must be done upfront to avoid multiple tasks attempting to set up the cache
                var dictionaryCacheControl = hlaMetadataDictionaryFactory.BuildCacheControl(hlaNomenclatureVersion);
                await dictionaryCacheControl.PreWarmAllCaches();

                logger.SendTrace("HLA PROCESSOR: caching antigens from hla service", LogLevel.Info);

                logger.SendTrace("HLA PROCESSOR: inserting new p groups to database", LogLevel.Info);
                // P Groups are inserted (when using relational database storage) upfront. All groups are extracted from the HlaMetadataDictionary, and new ones added to the SQL database
                var hlaDictionary = hlaMetadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersion);
                var pGroups = hlaDictionary.GetAllPGroups();
                pGroupRepository.InsertPGroups(pGroups);
            }
            catch (Exception e)
            {
                logger.SendEvent(new HlaRefreshSetUpFailureEventModel(e));
                throw;
            }
        }
    }
}