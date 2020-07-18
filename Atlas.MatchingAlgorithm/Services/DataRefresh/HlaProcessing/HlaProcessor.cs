using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.ApplicationInsights;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Donors;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.HlaProcessing
{
    public interface IHlaProcessor
    {
        /// <summary>
        /// For any donors with a higher id than the last updated donor:
        ///  - Fetches p-groups for all donor's hla
        ///  - Stores the pre-processed p-groups for use in matching
        /// </summary>
        Task UpdateDonorHla(string hlaNomenclatureVersion, bool continueExistingImport = false);
    }

    public class HlaProcessor : IHlaProcessor
    {
        private const int BatchSize = 4000;
        private const string HlaFailureEventName = "Imported Donor Hla Processing Failure(s) in the Matching Algorithm's DataRefresh";

        private readonly ILogger logger;
        private readonly IDonorHlaExpanderFactory donorHlaExpanderFactory;
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;
        private readonly IFailedDonorsNotificationSender failedDonorsNotificationSender;
        private readonly IDonorImportRepository donorImportRepository;
        private readonly IDataRefreshRepository dataRefreshRepository;
        private readonly IPGroupRepository pGroupRepository;

        public HlaProcessor(
            ILogger logger,
            IDonorHlaExpanderFactory donorHlaExpanderFactory,
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory,
            IFailedDonorsNotificationSender failedDonorsNotificationSender,
            IDormantRepositoryFactory repositoryFactory)
        {
            this.logger = logger;
            this.donorHlaExpanderFactory = donorHlaExpanderFactory;
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
            this.failedDonorsNotificationSender = failedDonorsNotificationSender;
            donorImportRepository = repositoryFactory.GetDonorImportRepository();
            dataRefreshRepository = repositoryFactory.GetDataRefreshRepository();
            pGroupRepository = repositoryFactory.GetPGroupRepository();
        }

        public async Task UpdateDonorHla(string hlaNomenclatureVersion, bool continueExistingImport)
        {
            await PerformUpfrontSetup(hlaNomenclatureVersion);

            try
            {
                await PerformHlaUpdate(hlaNomenclatureVersion, continueExistingImport);
            }
            catch (Exception e)
            {
                logger.SendEvent(new HlaRefreshFailureEventModel(e));
                throw;
            }
        }

        private async Task PerformHlaUpdate(string hlaNomenclatureVersion, bool continueExistingImport)
        {
            var totalDonorCount = await dataRefreshRepository.GetDonorCount();
            var batchedDonors = await dataRefreshRepository.NewOrderedDonorBatchesToImport(BatchSize, continueExistingImport);
            var (donorsProcessed, lastDonorIdSuspectedOfBeingReimported) = await DetermineProgressAndReimportBoundaries(batchedDonors, continueExistingImport);
            var failedDonors = new List<FailedDonorInfo>();

            if (continueExistingImport)
            {
                logger.SendTrace($"Hla Processing continuing, from {Decimal.Divide(donorsProcessed, totalDonorCount):0.00%} complete");
            }
            
            foreach (var donorBatch in batchedDonors)
            {
                var donorsInBatch = donorBatch.Count;

                // When continuing a donor import there will be some overlap of donors to ensure all donors are processed. 
                // This ensures we do not end up with duplicate p-groups in the matching hla tables
                // We do not want to attempt to remove p-groups for all batches as it would be detrimental to performance, so we limit it to the first two batches
                var shouldRemovePGroups = donorBatch.First().DonorId <= lastDonorIdSuspectedOfBeingReimported;

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

        private async Task<(int,int)> DetermineProgressAndReimportBoundaries(
            List<List<DonorInfo>> batchedDonors,
            bool continueExistingImport
        )
        {
            if (continueExistingImport)
            {
                var initialDonor = batchedDonors.First().First(); // Only safe because we AREN'T streaming these donors. Revisit this if we change that!
                var donorsPreviouslyProcessed = await dataRefreshRepository.GetDonorCountLessThan(initialDonor.DonorId);
                var overlapDonors = batchedDonors.Take(DataRefreshRepository.NumberOfBatchesOverlapOnRestart).ToList();
                var lastDonorIdInOverlap = overlapDonors.Last().Last().DonorId;

                return (donorsPreviouslyProcessed, lastDonorIdInOverlap);
            }
            else
            {
                var noDonorsWerePreviouslyProcessed = 0;
                var noDonorsAreBeingReimported = 0;
                return (noDonorsWerePreviouslyProcessed, noDonorsAreBeingReimported);
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
            List<DonorInfo> donorBatch,
            string hlaNomenclatureVersion,
            bool shouldRemovePGroups)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (shouldRemovePGroups)
            {
                await donorImportRepository.RemovePGroupsForDonorBatch(donorBatch.Select(d => d.DonorId));
            }

            var donorHlaExpander = donorHlaExpanderFactory.BuildForSpecifiedHlaNomenclatureVersion(hlaNomenclatureVersion);
            var hlaExpansionResults = await donorHlaExpander.ExpandDonorHlaBatchAsync(donorBatch, HlaFailureEventName);
            EnsureAllPGroupsExist(hlaExpansionResults.ProcessingResults);

            await donorImportRepository.AddMatchingPGroupsForExistingDonorBatch(hlaExpansionResults.ProcessingResults);

            stopwatch.Stop();
            logger.SendTrace("Updated Donors", LogLevel.Verbose, new Dictionary<string, string>
            {
                {"NumberOfDonors", hlaExpansionResults.ProcessingResults.Count.ToString()},
                {"UpdateTime", stopwatch.ElapsedMilliseconds.ToString()}
            });

            return hlaExpansionResults.FailedDonors;
        }

        /// <remarks>
        /// See notes in FindOrCreatePGroupIds.
        /// In practice this will never do anything in Prod code, because of the InsertPGroups prep step, below.
        /// But it means that during tests the DonorUpdate code behaves more like
        /// "the real thing", since the PGroups have already been inserted into the DB.
        /// </remarks>
        private void EnsureAllPGroupsExist(IReadOnlyCollection<DonorInfoWithExpandedHla> donorsWithHlas)
        {
            var allPGroups = donorsWithHlas
                .SelectMany(d =>
                    d.MatchingHla?.ToEnumerable().SelectMany(hla =>
                        hla?.MatchingPGroups ?? new string[0]
                    ) ?? new List<string>()
                ).ToList();

            pGroupRepository.FindOrCreatePGroupIds(allPGroups);
        }

        private async Task PerformUpfrontSetup(string hlaNomenclatureVersion)
        {
            try
            {
                logger.SendTrace("HLA PROCESSOR: Caching HlaMetadataDictionary tables");

                // Cloud tables are cached for performance reasons - this must be done upfront to avoid multiple tasks attempting to set up the cache
                var dictionaryCacheControl = hlaMetadataDictionaryFactory.BuildCacheControl(hlaNomenclatureVersion);
                await dictionaryCacheControl.PreWarmAllCaches();

                logger.SendTrace("HLA PROCESSOR: Inserting new P-Groups to database");

                // P Groups are inserted upfront, for performance reasons. All groups are extracted from the
                // HlaMetadataDictionary, and any that are new are added to the SQL database.
                //
                // In most realistic continuations this step could be skipped, but it's just about possible that
                // the previous import could have been killed during the Pre-Warm, in which case the PGroups might
                // not have been inserted yet.
                //
                // Fortunately, since we've pre-warmed the cache, the PGroup fetch will be instantaneous and the
                // PGroupInsertion filters existing PGroups, so it will end up being a no-op if this is repeated.
                // So it should be almost instantaneous for a continuation.
                //
                // Lastly it only takes a few seconds to run even the first time it's run, so there's no realistic
                // bad out-come from allowing it to re-run.
                var hlaDictionary = hlaMetadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersion);
                var pGroups = await hlaDictionary.GetAllPGroups();
                pGroupRepository.InsertPGroups(pGroups);

                logger.SendTrace("HLA PROCESSOR: P-Groups inserted.");
            }
            catch (Exception e)
            {
                logger.SendEvent(new HlaRefreshSetUpFailureEventModel(e));
                throw;
            }
        }
    }
}