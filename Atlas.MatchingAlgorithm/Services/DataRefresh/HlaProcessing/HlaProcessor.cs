using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Helpers;
using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.ApplicationInsights;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Settings;
using LoggingStopwatch;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.HlaProcessing
{
    public interface IHlaProcessor
    {
        /// <summary>
        /// For any donors with a higher id than the last updated donor:
        ///  - Fetches p-groups for all donor's hla
        ///  - Stores the pre-processed p-groups for use in matching
        /// </summary>
        Task UpdateDonorHla(string hlaNomenclatureVersion, int refreshRecordId, int? lastProcessedDonor = null, bool continueExistingImport = false);
    }

    public class HlaProcessor : IHlaProcessor
    {
        private const int BatchSize = 2000; // At 1k this definitely works fine. At 4k it's been seen throwing OOM Exceptions
        private const string HlaFailureEventName = "Imported Donor Hla Processing Failure(s) in the Matching Algorithm's DataRefresh";

        private readonly ILogger logger;
        private readonly IDonorHlaExpanderFactory donorHlaExpanderFactory;
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;
        private readonly IFailedDonorsNotificationSender failedDonorsNotificationSender;
        private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private readonly DataRefreshSettings settings;
        private readonly IDonorImportRepository donorImportRepository;
        private readonly IDataRefreshRepository dataRefreshRepository;
        private readonly IPGroupRepository pGroupRepository;

        public const int NumberOfBatchesOverlapOnRestart = 2;

        public HlaProcessor(
            ILogger logger,
            IDonorHlaExpanderFactory donorHlaExpanderFactory,
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory,
            IFailedDonorsNotificationSender failedDonorsNotificationSender,
            IDormantRepositoryFactory repositoryFactory,
            IDataRefreshHistoryRepository dataRefreshHistoryRepository,
            DataRefreshSettings settings)
        {
            this.logger = logger;
            this.donorHlaExpanderFactory = donorHlaExpanderFactory;
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
            this.failedDonorsNotificationSender = failedDonorsNotificationSender;
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
            this.settings = settings;
            donorImportRepository = repositoryFactory.GetDonorImportRepository();
            dataRefreshRepository = repositoryFactory.GetDataRefreshRepository();
            pGroupRepository = repositoryFactory.GetPGroupRepository();
        }

        public async Task UpdateDonorHla(string hlaNomenclatureVersion, int refreshRecordId, int? lastProcessedDonor, bool continueExistingImport)
        {
            await PerformUpfrontSetup(hlaNomenclatureVersion);

            try
            {
                await PerformHlaUpdate(hlaNomenclatureVersion, refreshRecordId, lastProcessedDonor, continueExistingImport);
            }
            catch (Exception e)
            {
                logger.SendEvent(new HlaRefreshFailureEventModel(e));
                throw;
            }
        }

        private async Task PerformHlaUpdate(string hlaNomenclatureVersion, int refreshRecordId, int? lastProcessedDonor, bool continueExistingProcessing)
        {
            var totalDonorCount = await dataRefreshRepository.GetDonorCount();
            var batchedDonors = await dataRefreshRepository.NewOrderedDonorBatchesToImport(BatchSize, lastProcessedDonor, continueExistingProcessing);
            var (donorsPreviouslyProcessed, lastDonorIdSuspectedOfBeingReprocessed) = await DetermineProgressAndReprocessingBoundaries(batchedDonors, continueExistingProcessing);
            var failedDonors = new List<FailedDonorInfo>();
            var donorsToImport = totalDonorCount - donorsPreviouslyProcessed;

            if (continueExistingProcessing)
            {
                logger.SendTrace($"Hla Processing continuing. {donorsPreviouslyProcessed} donors previously processed. {donorsToImport} remain.");
            }
            
            var progressReports = new LongLoggingSettings
            {
                ExpectedNumberOfIterations = batchedDonors.Count,
                InnerOperationLoggingPeriod = 10, // Note this is every 10 *Batches*
                ReportPercentageCompletion = true,
                ReportProjectedCompletionTime = true
            };
            var summaryReportOnly = new LongLoggingSettings { InnerOperationLoggingPeriod = int.MaxValue, ReportOuterTimerStart = false};
            var summaryReportWithThreadingBreakdown = new LongLoggingSettings { InnerOperationLoggingPeriod = int.MaxValue, ReportOuterTimerStart = false, ReportPerThreadTime = true };
            using (var batchProgressTimer = logger.RunLongOperationWithTimer($"Hla Batch Overall Processing. Inner Operation is UpdateDonorBatch", progressReports))
            using (var hlaExpansionTimer = logger.RunLongOperationWithTimer($"Hla Expansion during HlaProcessing", summaryReportOnly))
            using (var pGroupLinearWaitTimer = logger.RunLongOperationWithTimer($"Linear wait on HlaInsert during HlaProcessing", summaryReportOnly))
            using (var pGroupInsertTimer = logger.RunLongOperationWithTimer($"Parallel Write time on HlaInsert during HlaProcessing", summaryReportWithThreadingBreakdown))
            {
                var completedDonors = new FixedSizedQueue<int>(NumberOfBatchesOverlapOnRestart * BatchSize);

                foreach (var donorBatch in batchedDonors)
                {
                    // When continuing a donor import there will be some overlap of donors to ensure all donors are processed. 
                    // This ensures we do not end up with duplicate p-groups in the matching hla tables
                    // We do not want to attempt to remove p-groups for all batches as it would be detrimental to performance, so we limit it to the first two batches
                    var shouldRemovePGroups = donorBatch.First().DonorId <= lastDonorIdSuspectedOfBeingReprocessed;

                    using (batchProgressTimer.TimeInnerOperation())
                    {
                        var failedDonorsFromBatch = await UpdateDonorBatch(
                            donorBatch,
                            hlaNomenclatureVersion,
                            shouldRemovePGroups,
                            hlaExpansionTimer,
                            pGroupLinearWaitTimer,
                            pGroupInsertTimer
                            );
                        failedDonors.AddRange(failedDonorsFromBatch);
                    }

                    completedDonors.Enqueue(donorBatch.Last().DonorId);

                    if (completedDonors.Count >= NumberOfBatchesOverlapOnRestart)
                    {
                        await dataRefreshHistoryRepository
                            .UpdateLastSafelyProcessedDonor(refreshRecordId, completedDonors.Peek());
                    }
                }
            }

            if (failedDonors.Any())
            {
                await failedDonorsNotificationSender.SendFailedDonorsAlert(failedDonors, HlaFailureEventName, Priority.Low);
            }
        }

        private async Task<(int,int)> DetermineProgressAndReprocessingBoundaries(
            List<List<DonorInfo>> batchedDonors,
            bool continueExistingImport
        )
        {
            if (continueExistingImport)
            {
                // Only safe because we AREN'T streaming these donors. Revisit this if we change that!
                var initialDonorToReprocess = batchedDonors.First().First();

                // Literally, the following query counts donors that exist in Donors table, < DonorIdX, but since donors
                // are imported strictly in order, that's equivalent to the number of processed donors already handled.
                var donorsPreviouslyProcessed = await dataRefreshRepository.GetDonorCountLessThan(initialDonorToReprocess.DonorId);

                var overlapDonors = batchedDonors.Take(DataRefreshRepository.NumberOfBatchesOverlapOnRestart).ToList();
                var lastDonorIdInOverlap = overlapDonors.Last().Last().DonorId;

                return (donorsPreviouslyProcessed, lastDonorIdInOverlap);
            }
            else
            {
                var noDonorsWerePreviouslyProcessed = 0;
                var noDonorsAreBeingReprocessed = 0;
                return (noDonorsWerePreviouslyProcessed, noDonorsAreBeingReprocessed);
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
            bool shouldRemovePGroups,
            LongOperationLoggingStopwatch hlaExpansionTimer,
            LongOperationLoggingStopwatch pGroupLinearWaitTimer,
            LongOperationLoggingStopwatch pGroupInsertTimer)
        {
            if (shouldRemovePGroups)
            {
                await donorImportRepository.RemovePGroupsForDonorBatch(donorBatch.Select(d => d.DonorId));
            }
            var donorHlaExpander = donorHlaExpanderFactory.BuildForSpecifiedHlaNomenclatureVersion(hlaNomenclatureVersion);

            var timedInnerOperation = hlaExpansionTimer.TimeInnerOperation();
            var hlaExpansionResults = await donorHlaExpander.ExpandDonorHlaBatchAsync(donorBatch, HlaFailureEventName);
            timedInnerOperation.Dispose();

            EnsureAllPGroupsExist(hlaExpansionResults.ProcessingResults);

            await donorImportRepository.AddMatchingPGroupsForExistingDonorBatch(
                hlaExpansionResults.ProcessingResults,
                settings.DataRefreshDonorUpdatesShouldBeFullyTransactional,
                pGroupLinearWaitTimer,
                pGroupInsertTimer);

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
                using (logger.RunTimed("HLA PROCESSOR: Caching HlaMetadataDictionary tables", LogLevel.Info, true))
                {
                    // Cloud tables are cached for performance reasons
                    var dictionaryCacheControl = hlaMetadataDictionaryFactory.BuildCacheControl(hlaNomenclatureVersion);
                    await dictionaryCacheControl.PreWarmAllCaches();
                }

                using (logger.RunTimed("HLA PROCESSOR: Inserting new P-Groups to database", LogLevel.Info, true))
                {
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
                }
            }
            catch (Exception e)
            {
                logger.SendEvent(new HlaRefreshSetUpFailureEventModel(e));
                throw;
            }
        }
    }
}