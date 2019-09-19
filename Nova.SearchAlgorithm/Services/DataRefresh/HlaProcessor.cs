using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.ApplicationInsights;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Common.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Nova.SearchAlgorithm.Services.MatchingDictionary;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.ApplicationInsights.EventModels;
using Nova.Utils.Notifications;

namespace Nova.SearchAlgorithm.Services.DataRefresh
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

        private readonly ILogger logger;
        private readonly IExpandHlaPhenotypeService expandHlaPhenotypeService;
        private readonly IAntigenCachingService antigenCachingService;
        private readonly IDonorImportRepository donorImportRepository;
        private readonly IDataRefreshRepository dataRefreshRepository;
        private readonly IHlaMatchingLookupRepository hlaMatchingLookupRepository;
        private readonly IAlleleNamesLookupRepository alleleNamesLookupRepository;
        private readonly INotificationsClient notificationsClient;
        private readonly IPGroupRepository pGroupRepository;

        public HlaProcessor(
            ILogger logger,
            IExpandHlaPhenotypeService expandHlaPhenotypeService,
            IAntigenCachingService antigenCachingService,
            IDormantRepositoryFactory repositoryFactory,
            IHlaMatchingLookupRepository hlaMatchingLookupRepository,
            IAlleleNamesLookupRepository alleleNamesLookupRepository,
            INotificationsClient notificationsClient)
        {
            this.logger = logger;
            this.expandHlaPhenotypeService = expandHlaPhenotypeService;
            this.antigenCachingService = antigenCachingService;
            donorImportRepository = repositoryFactory.GetDonorImportRepository();
            dataRefreshRepository = repositoryFactory.GetDataRefreshRepository();
            pGroupRepository = repositoryFactory.GetPGroupRepository();
            this.hlaMatchingLookupRepository = hlaMatchingLookupRepository;
            this.alleleNamesLookupRepository = alleleNamesLookupRepository;
            this.notificationsClient = notificationsClient;
        }

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

            var failedDonors = new List<InputDonorWithExpandedHla>();

            while (batchedQuery.HasMoreResults)
            {
                var donorBatch = (await batchedQuery.RequestNextAsync()).ToList();

                // When continuing a donor import there will be some overlap of donors to ensure all donors are processed. 
                // This ensures we do not end up with duplicate p-groups in the matching hla tables
                // We do not want to attempt to remove p-groups for all batches as it would be detrimental to performance, so we limit it to the first two batches
                var shouldRemovePGroups = donorsProcessed < DataRefreshRepository.NumberOfBatchesOverlapOnRestart * BatchSize;

                var failedDonorFromBatch = await UpdateDonorBatch(donorBatch, hlaDatabaseVersion, shouldRemovePGroups);
                failedDonors.AddRange(failedDonorFromBatch);

                donorsProcessed += BatchSize;
                logger.SendTrace($"Hla Processing {(double) donorsProcessed / totalDonorCount:0.00%} complete", LogLevel.Info);
            }

            if (failedDonors.Any())
            {
                var failedAnthonyNolanDonors = failedDonors.Where(d => d.RegistryCode == RegistryCode.AN).Select(d => d.DonorId).ToList();
                var failedAlignedRegistryDonors = failedDonors.Where(d => d.RegistryCode != RegistryCode.AN).Select(d => d.DonorId).ToList();
                const string alertSummary = "Hla Processing: One or more donors could not be processed";
                logger.SendEvent(new EventModel(alertSummary)
                {
                    Level = LogLevel.Error,
                    Properties =
                    {
                        {"AnthonyNolanDonorIds", string.Join(",", failedAnthonyNolanDonors)},
                        {"AlignedRegistryDonorIds", string.Join(",", failedAlignedRegistryDonors)},
                    }
                });
                await notificationsClient.SendAlert(new Alert(
                    alertSummary,
                    $"{failedAnthonyNolanDonors.Count} Anthony Nolan donors failed. {failedAlignedRegistryDonors.Count} Aligned Registry donors failed. " +
                    "See application insights for further information - an event with the same name as this model should have been raised, as well as individual events for each donor.",
                    Priority.Low,
                    "Nova.SearchAlgorithm"
                ));
            }
        }

        /// <summary>
        /// Fetches Expanded HLA information for all donors in a batch, and stores the processed  information in the database.
        /// </summary>
        /// <param name="donorBatch">The collection of donors to update</param>
        /// <param name="hlaDatabaseVersion">The version of the HLA database to use to fetch expanded HLA information</param>
        /// <param name="shouldRemovePGroups">If set, existing p-groups will be removed before adding new ones.</param>
        /// <returns>A collection of any donors that did ot import successfully</returns>
        private async Task<IEnumerable<InputDonorWithExpandedHla>> UpdateDonorBatch(
            IEnumerable<DonorResult> donorBatch,
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

            var donorHlaData = await Task.WhenAll(donorBatch.Select(d => FetchDonorHlaData(d, hlaDatabaseVersion)));
            var inputDonors = donorHlaData.Where(x => x?.MatchingHla != null).ToList();
            await donorImportRepository.AddMatchingPGroupsForExistingDonorBatch(inputDonors);

            stopwatch.Stop();
            logger.SendTrace("Updated Donors", LogLevel.Verbose, new Dictionary<string, string>
            {
                {"NumberOfDonors", inputDonors.Count.ToString()},
                {"UpdateTime", stopwatch.ElapsedMilliseconds.ToString()}
            });

            return donorHlaData.Where(x => x.MatchingHla == null);
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

        /// <summary>
        /// Fetches expanded HLA information for a donor, via the matching dictionary.
        /// If any HLA can not be looked up (e.g. they are invalid, or valid only in a different version of the HLA naming database), will return null HLA information. 
        /// </summary>
        private async Task<InputDonorWithExpandedHla> FetchDonorHlaData(DonorResult donor, string hlaDatabaseVersion)
        {
            try
            {
                var matchingHla = await expandHlaPhenotypeService.GetPhenotypeOfExpandedHla(donor.HlaNames, hlaDatabaseVersion);

                return new InputDonorWithExpandedHla
                {
                    DonorId = donor.DonorId,
                    DonorType = donor.DonorType,
                    RegistryCode = donor.RegistryCode,
                    MatchingHla = matchingHla,
                };
            }
            catch (MatchingDictionaryException e)
            {
                logger.SendEvent(new HlaRefreshMatchingDictionaryLookupFailureEventModel(e, donor.DonorId.ToString()));
                return new InputDonorWithExpandedHla
                {
                    DonorId = donor.DonorId,
                    DonorType = donor.DonorType,
                    RegistryCode = donor.RegistryCode,
                    MatchingHla = null,
                };
            }
        }
    }
}