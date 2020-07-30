﻿using System;
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

        public const int NumberOfBatchesOverlapOnRestart = 3;

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
            var summaryReportWithThreadingCount = new LongLoggingSettings { InnerOperationLoggingPeriod = int.MaxValue, ReportOuterTimerStart = false, ReportThreadCount = true, ReportPerThreadTime = false };

            var timerCollection = new LongStopwatchCollection(text => logger.SendTrace(text), summaryReportOnly);

            using (timerCollection.InitialiseStopwatch("batchProgressTimer", "Hla Batch Overall Processing. Inner Operation is UpdateDonorBatch", null, progressReports)) 
            using (timerCollection.InitialiseStopwatch("hlaExpansionTimer", " * Hla Expansion, during HlaProcessing")) 
            using (timerCollection.InitialiseStopwatch("newPGroupInsertion", " * Ensuring all PGroups exist in the DB, during HlaProcessing (no actual DB writing, just processing)")) 
            using (timerCollection.InitialiseStopwatch("newPGroupInsertion_Flattening", " * * Flatten the donors' PGroups, during EnsureAllPGroupsExist, during HlaProcessing")) 
            using (timerCollection.InitialiseStopwatch("newPGroupInsertion_FindNew", " * * Check PGroups against known dictionary, during EnsureAllPGroupsExist, during HlaProcessing"))
            using (timerCollection.InitialiseStopwatch("upsertTimer", " * UpsertMatchingPGroupsAtSpecifiedLoci, during HlaProcessing")) 
            using (timerCollection.InitialiseStopwatch("pGroupInsertSetupTimer", " * * Time setting up Hla BulkInsert statements, during HlaProcessing")) 
            using (timerCollection.InitialiseStopwatch("pGroupInsertSetup_BuildDataTableTimer", " * * * Data Table Build, in Hla BulkInsert SETUP, during HlaProcessing"))
            using (timerCollection.InitialiseDisabledStopwatch("pGroupInsertSetup_CreateDataTableObject", " * * * * Creating blank DataTable object, in DataTableBuild, in Hla BulkInsert SETUP, during HlaProcessing"))
            using (timerCollection.InitialiseDisabledStopwatch("pGroupInsertSetup_CreateDataTable_OutsideForeach", " * * * * Outside the innermost foreach of method, in DataTableBuild, in Hla BulkInsert SETUP, during HlaProcessing"))
            using (timerCollection.InitialiseDisabledStopwatch("pGroupInsertSetup_CreateDataTable_InsideForeach", " * * * * Inside the innermost foreach of method, in DataTableBuild, in Hla BulkInsert SETUP, during HlaProcessing"))
            using (timerCollection.InitialiseDisabledStopwatch("pGroupInsertSetup_FetchPGroupId", " * * * * Fetch PGroup Id, in DataTableBuild, in Hla BulkInsert SETUP, during HlaProcessing") )
            using (timerCollection.InitialiseDisabledStopwatch("pGroupInsertSetup_AddRowsToDataTable", " * * * * Raw DataTable Row Add, in DataTableBuild, in Hla BulkInsert SETUP, during HlaProcessing") )
            using (timerCollection.InitialiseStopwatch("pGroupInsertSetup_DeleteExistingRecordsTimer", " * * * Delete Existing records, in Hla BulkInsert SETUP, during HlaProcessing") )
            using (timerCollection.InitialiseStopwatch("pGroupLinearWaitTimer", " * * Linear wait on HlaInsert operation, during HlaProcessing") )
            using (timerCollection.InitialiseStopwatch("pGroupDbInsertTimer", " * * * Parallel Write time on HlaInsert operation, during HlaProcessing", null, summaryReportWithThreadingCount))
            {
                // We only store the last Id in each batch so we only need to keep one Id per batch.
                var completedDonors = new FixedSizedQueue<int>(NumberOfBatchesOverlapOnRestart);

                foreach (var donorBatch in batchedDonors)
                {
                    // When continuing a donor import there will be some overlap of donors to ensure all donors are processed. 
                    // This ensures we do not end up with duplicate p-groups in the matching hla tables
                    // We do not want to attempt to remove p-groups for all batches as it would be detrimental to performance, so we limit it to the first two batches
                    var shouldRemovePGroups = donorBatch.First().DonorId <= lastDonorIdSuspectedOfBeingReprocessed;

                    using (timerCollection.TimeInnerOperation("batchProgressTimer"))
                    {
                        var failedDonorsFromBatch = await UpdateDonorBatch(
                            donorBatch,
                            hlaNomenclatureVersion,
                            shouldRemovePGroups,
                            timerCollection
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
            LongStopwatchCollection timerCollection)
        {
            if (shouldRemovePGroups)
            {
                await donorImportRepository.RemovePGroupsForDonorBatch(donorBatch.Select(d => d.DonorId));
            }
            var donorHlaExpander = donorHlaExpanderFactory.BuildForSpecifiedHlaNomenclatureVersion(hlaNomenclatureVersion);

            var timedInnerOperation = timerCollection.TimeInnerOperation("hlaExpansionTimer");
            var hlaExpansionResults = await donorHlaExpander.ExpandDonorHlaBatchAsync(donorBatch, HlaFailureEventName);
            timedInnerOperation.Dispose();

            using (timerCollection.TimeInnerOperation("newPGroupInsertion"))
            {
                EnsureAllPGroupsExist(hlaExpansionResults.ProcessingResults, timerCollection);
            }

            await donorImportRepository.AddMatchingPGroupsForExistingDonorBatch(
                hlaExpansionResults.ProcessingResults,
                settings.DataRefreshDonorUpdatesShouldBeFullyTransactional,
                timerCollection);

            return hlaExpansionResults.FailedDonors;
        }

        /// <remarks>
        /// See notes in FindOrCreatePGroupIds.
        /// In practice this will never do anything in Prod code, because of the InsertPGroups prep step, below.
        /// But it means that during tests the DonorUpdate code behaves more like
        /// "the real thing", since the PGroups have already been inserted into the DB.
        ///
        /// Note that over the course of a 2M donor import, this is flattening a total of ~1B records, which
        /// ends up taking 2-3 minutes. The EnsureAllPGroupsExist check also takes 2-3 minutes.
        /// </remarks>
        private void EnsureAllPGroupsExist(
            IReadOnlyCollection<DonorInfoWithExpandedHla> donorsWithHlas,
            LongStopwatchCollection timerCollection
            )
        {
            var block1 = timerCollection.TimeInnerOperation("newPGroupInsertion_Flattening");
            var allPGroups = donorsWithHlas
                .SelectMany(d =>
                    d.MatchingHla?.ToEnumerable().SelectMany(hla =>
                        hla?.MatchingPGroups ?? new string[0]
                    ) ?? new List<string>()
                ).ToList();
            block1?.Dispose();

            pGroupRepository.EnsureAllPGroupsExist(allPGroups, timerCollection);
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