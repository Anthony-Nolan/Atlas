using Atlas.Client.Models.SupportMessages;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Utils;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Helpers;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;


namespace Atlas.MatchingAlgorithm.Services.DataRefresh.HlaProcessing
{
    public interface IHlaProcessor
    {
        /// <summary>
        /// For any donors with a higher id than the last updated donor:
        ///  - Fetches p-groups for all donor's hla
        ///  - Stores the pre-processed p-groups for use in matching
        /// </summary>
        /// <param name="cancellationToken">
        /// Cancelled if the data refresh loses its run-level lease. Observed between batches, never mid-batch, so the
        /// last-safely-processed donor marker stays consistent with what has actually been written. That guarantee
        /// rests on an explicit check in the processing loop, not on the enumeration's own token: donor pages are
        /// prefetched into a queue, and reading an already-queued page completes without ever consulting a token.
        /// </param>
        Task UpdateDonorHla(
            string hlaNomenclatureVersion,
            Func<int, Task> updateLastSafelyProcessedDonorId,
            int? lastProcessedDonor = null,
            bool continueExistingImport = false,
            CancellationToken cancellationToken = default);
    }

    public class HlaProcessor : IHlaProcessor
    {
        private const int BatchSize = 2000; // At 1k this definitely works fine. At 4k it's been seen throwing OOM Exceptions
        private const string HlaFailureEventName = "Imported Donor Hla Processing Failure(s) in the Matching Algorithm's DataRefresh";

        private readonly IMatchingAlgorithmImportLogger logger;
        private readonly IDonorHlaExpanderFactory donorHlaExpanderFactory;
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;
        private readonly IFailedDonorsNotificationSender failedDonorsNotificationSender;
        private readonly DataRefreshSettings settings;
        private readonly IDonorImportRepository donorImportRepository;
        private readonly IDataRefreshRepository dataRefreshRepository;
        private readonly IPGroupRepository pGroupRepository;
        private readonly IHlaNamesRepository hlaNamesRepository;
        private readonly IHlaImportRepository hlaImportRepository;
        private readonly IMacDictionary macDictionary;

        public const int NumberOfBatchesOverlapOnRestart = 3;

        /// <summary>
        /// How many donor pages the read side may run ahead of the processing side.
        /// </summary>
        /// <remarks>
        /// Processing measured roughly twice the read, so in steady state the read side is always waiting and a depth
        /// of 1 would recover nearly all of the available time. The second rung is there only to absorb variance in
        /// individual page read times, and is cheap: a page is raw <see cref="DonorInfo"/>, an order of magnitude
        /// smaller than the expanded HLA the processing side builds from it, so a rung costs far less than the
        /// <see cref="BatchSize"/> of 2000 rows suggests. Deeper buys nothing while processing remains the slower side
        /// - it would only let the read side finish further ahead and then idle.
        /// </remarks>
        private const int ChannelDepth = 2;

        /// <summary>
        /// Read durations, summed with the <c>batchProgress</c> inner-operation timings and divided by the stage's wall
        /// clock, give the stage's occupancy: ~1 when fully serial, ~2 when the pipeline is working. One trace per page
        /// is ~20k of them across a full refresh, five times what the donor import stage emits - hence Verbose.
        /// </summary>
        /// <remarks>
        /// Because it is Verbose, this does NOT reach App Insights on a default deployment: traces are filtered by
        /// <c>messageLogLevel >= configuredLogLevel</c> and every Functions app ships
        /// <c>ApplicationInsights:LogLevel = Info</c>. Taking the occupancy measurement therefore means raising that
        /// setting for the duration of the refresh being measured. That is deliberate - it keeps ~20k traces per
        /// refresh out of normal operation - but it does mean the numbers are opt-in rather than always available.
        /// </remarks>
        private const string ReadBatchTimingMessage = "Read donor batch from the transient database";

        public HlaProcessor(
            IMatchingAlgorithmImportLogger logger,
            IDonorHlaExpanderFactory donorHlaExpanderFactory,
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory,
            IFailedDonorsNotificationSender failedDonorsNotificationSender,
            IDormantRepositoryFactory repositoryFactory,
            DataRefreshSettings settings,
            IMacDictionary macDictionary)
        {
            this.logger = logger;
            this.donorHlaExpanderFactory = donorHlaExpanderFactory;
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
            this.failedDonorsNotificationSender = failedDonorsNotificationSender;
            this.settings = settings;
            this.macDictionary = macDictionary;
            donorImportRepository = repositoryFactory.GetDonorImportRepository();
            dataRefreshRepository = repositoryFactory.GetDataRefreshRepository();
            pGroupRepository = repositoryFactory.GetPGroupRepository();
            hlaNamesRepository = repositoryFactory.GetHlaNamesRepository();
            hlaImportRepository = repositoryFactory.GetHlaImportRepository();
        }

        public async Task UpdateDonorHla(
            string hlaNomenclatureVersion,
            Func<int, Task> updateLastSafelyProcessedDonorId,
            int? lastProcessedDonor,
            bool continueExistingImport,
            CancellationToken cancellationToken)
        {
            await PerformUpfrontSetup(hlaNomenclatureVersion);

            try
            {
                await PerformHlaUpdate(
                    hlaNomenclatureVersion, updateLastSafelyProcessedDonorId, lastProcessedDonor, continueExistingImport, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Not an HLA processing failure: the refresh lost its lease, and that is logged where it is recognised.
                throw;
            }
            catch (Exception e)
            {
                logger.SendException(e, LogLevel.Critical);
                throw;
            }
        }

        private async Task PerformHlaUpdate(
            string hlaNomenclatureVersion,
            Func<int, Task> updateLastSafelyProcessedDonorId,
            int? lastProcessedDonor,
            bool continueExistingProcessing,
            CancellationToken cancellationToken)
        {
            var totalDonorCount = await dataRefreshRepository.GetDonorCount();
            var batchedDonors = dataRefreshRepository.NewOrderedDonorBatchesToImport(BatchSize, lastProcessedDonor, cancellationToken);

            var overlapBatches = continueExistingProcessing
                ? await dataRefreshRepository.GetOrderedDonorBatches(NumberOfBatchesOverlapOnRestart, BatchSize, lastProcessedDonor ?? 0)
                : new List<List<DonorInfo>>();

            var (donorsPreviouslyProcessed, lastDonorIdSuspectedOfBeingReprocessed) = continueExistingProcessing
                ? await DetermineProgressAndReprocessingBoundaries(overlapBatches)
                : (0, 0);
            var failedDonors = new List<FailedDonorInfo>();
            var donorsToImport = totalDonorCount - donorsPreviouslyProcessed;

            if (continueExistingProcessing)
            {
                logger.SendTrace($"Hla Processing continuing. {donorsPreviouslyProcessed} donors previously processed. {donorsToImport} remain.");
            }

            var progressReports = new LongLoggingSettings
            {
                ExpectedNumberOfIterations = totalDonorCount / BatchSize,
                InnerOperationLoggingPeriod = 10, // Note this is every 10 *Batches*
                ReportPercentageCompletion = true,
                ReportProjectedCompletionTime = true
            };
            var summaryReportOnly = new LongLoggingSettings {InnerOperationLoggingPeriod = int.MaxValue, ReportOuterTimerStart = false};
            var summaryReportWithThreadingCount = new LongLoggingSettings
                {InnerOperationLoggingPeriod = int.MaxValue, ReportOuterTimerStart = false, ReportThreadCount = true, ReportPerThreadTime = false};

            var timerCollection = new LongStopwatchCollection((text, milliseconds) =>
                logger.SendTrace(text, props: new Dictionary<string, string> {{"Milliseconds", milliseconds.ToString()}}), summaryReportOnly);

            // @formatter:off
            using (timerCollection.InitialiseStopwatch(DataRefreshTimingKeys.BatchProgress_TimerKey, "Hla Batch Overall Processing. Inner Operation is UpdateDonorBatch", null, progressReports)) 
            using (timerCollection.InitialiseStopwatch(DataRefreshTimingKeys.HlaExpansion_TimerKey, " * Hla Expansion, during HlaProcessing")) 
            using (timerCollection.InitialiseStopwatch(DataRefreshTimingKeys.NewPGroupInsertion_Overall_TimerKey, " * Ensuring all PGroups exist in the DB, during HlaProcessing (no actual DB writing, just processing)")) 
            using (timerCollection.InitialiseStopwatch(DataRefreshTimingKeys.NewPGroupInsertion_Flattening_TimerKey, " * * Flatten the donors' PGroups, during EnsureAllPGroupsExist, during HlaProcessing")) 
            using (timerCollection.InitialiseStopwatch(DataRefreshTimingKeys.NewPGroupInsertion_FindNew_TimerKey, " * * Check PGroups against known dictionary, during EnsureAllPGroupsExist, during HlaProcessing"))
            using (timerCollection.InitialiseStopwatch(DataRefreshTimingKeys.NewHlaNameInsertion_Overall_TimerKey, " * * Check HLA Names against known dictionary, during HlaProcessing"))
            using (timerCollection.InitialiseStopwatch(DataRefreshTimingKeys.NewHlaNameInsertion_Flattening_TimerKey, " * * Flatten HLA Names, during HlaProcessing"))
            using (timerCollection.InitialiseStopwatch(DataRefreshTimingKeys.NewHlaNameInsertion_FindNew_TimerKey, " * * Check HLA Names against known dictionary, during EnsureAllHlaNamesExist, during HlaProcessing"))
            using (timerCollection.InitialiseStopwatch(DataRefreshTimingKeys.HlaUpsert_Overall_TimerKey, " * UpsertMatchingPGroupsAtSpecifiedLoci, during HlaProcessing")) 
            using (timerCollection.InitialiseStopwatch(DataRefreshTimingKeys.HlaUpsert_BulkInsertSetup_Overall_TimerKey, " * * Time setting up Hla BulkInsert statements, during HlaProcessing")) 
            using (timerCollection.InitialiseStopwatch(DataRefreshTimingKeys.HlaUpsert_BulkInsertSetup_BuildDataTable_Overall_TimerKey, " * * * Data Table Build, in Hla BulkInsert SETUP, during HlaProcessing"))
            using (timerCollection.InitialiseDisabledStopwatch(DataRefreshTimingKeys.HlaUpsert_BulkInsertSetup_BuildDataTable_CreateDtObject_TimerKey, " * * * * Creating blank DataTable object, in DataTableBuild, in Hla BulkInsert SETUP, during HlaProcessing"))
            using (timerCollection.InitialiseDisabledStopwatch(DataRefreshTimingKeys.HlaUpsert_BulkInsertSetup_BuildDataTable_OutsideForeach_TimerKey, " * * * * Outside the innermost foreach of method, in DataTableBuild, in Hla BulkInsert SETUP, during HlaProcessing"))
            using (timerCollection.InitialiseDisabledStopwatch(DataRefreshTimingKeys.HlaUpsert_BulkInsertSetup_BuildDataTable_InsideForeach_TimerKey, " * * * * Inside the innermost foreach of method, in DataTableBuild, in Hla BulkInsert SETUP, during HlaProcessing"))
            using (timerCollection.InitialiseDisabledStopwatch(DataRefreshTimingKeys.HlaUpsert_BulkInsertSetup_BuildDataTable_FetchPGroupId_TimerKey, " * * * * Fetch PGroup Id, in DataTableBuild, in Hla BulkInsert SETUP, during HlaProcessing") )
            using (timerCollection.InitialiseDisabledStopwatch(DataRefreshTimingKeys.HlaUpsert_BulkInsertSetup_BuildDataTable_AddRowToDt_TimerKey, " * * * * Raw DataTable Row Add, in DataTableBuild, in Hla BulkInsert SETUP, during HlaProcessing") )
            using (timerCollection.InitialiseStopwatch(DataRefreshTimingKeys.HlaUpsert_BulkInsertSetup_DeleteExistingRecords_TimerKey, " * * * Delete Existing records, in Hla BulkInsert SETUP, during HlaProcessing") )
            using (timerCollection.InitialiseStopwatch(DataRefreshTimingKeys.HlaUpsert_BlockingWait_TimerKey, " * * Time spent in `Task.WhenAll`, JUST waiting on HlaInsert tasks to Complete, during HlaProcessing") )
            using (timerCollection.InitialiseStopwatch(DataRefreshTimingKeys.HlaUpsert_DtWriteExecution_TimerKey, " * * * Total Time spent across all threads, writing BulkInserts during HlaInsert operation, during HlaProcessing", null, summaryReportWithThreadingCount))
                // @formatter:on
            {
                failedDonors.AddRange(await RunHlaProcessingPipeline(
                    batchedDonors, hlaNomenclatureVersion, updateLastSafelyProcessedDonorId, timerCollection, cancellationToken));
            }

            if (failedDonors.Any())
            {
                await failedDonorsNotificationSender.SendFailedDonorsAlert(failedDonors, HlaFailureEventName, Priority.Low);
            }
        }

        /// <summary>
        /// Reads pages of donors out of the transient database while previously-read pages are being expanded and
        /// written, rather than alternating between the two.
        /// </summary>
        /// <remarks>
        /// In the application the two contend for nothing - the read is a keyset-paged query against the Donors table,
        /// the processing is HLA expansion plus bulk inserts into the matching HLA tables - so serially the stage costs
        /// read + processing, and overlapped roughly max(read, processing). Processing is much the larger of the two, so
        /// the read is expected to disappear behind it almost entirely.
        /// <para>
        /// They do share one thing they did not before: the transient database instance itself. The page query and the
        /// bulk insert can now run against it concurrently, so the read is no longer guaranteed to have it to itself.
        /// The read is much the lighter of the two and hits a different, indexed access path, so contention is expected
        /// to be slight - but that is a prediction, and no unit test here settles it, because none of them touch a real
        /// database. It is the read durations traced by <see cref="ReadBatchTimingMessage"/> that would show it: if the
        /// instance is the constraint, they rise once the pipeline fills instead of staying flat.
        /// </para>
        /// </remarks>
        private Task<List<FailedDonorInfo>> RunHlaProcessingPipeline(
            IAsyncEnumerable<List<DonorInfo>> batchedDonors,
            string hlaNomenclatureVersion,
            Func<int, Task> updateLastSafelyProcessedDonorId,
            LongStopwatchCollection timerCollection,
            CancellationToken cancellationToken) =>
            PrefetchPipeline.Run<List<DonorInfo>, List<FailedDonorInfo>>(
                ChannelDepth,
                (writer, queueDepth, token) => ReadDonorBatches(batchedDonors, writer, queueDepth, token),
                (reader, token) => ProcessDonorBatches(
                    reader, hlaNomenclatureVersion, updateLastSafelyProcessedDonorId, timerCollection, token),
                logger,
                "Donor read during HLA processing",
                cancellationToken);

        /// <summary>
        /// Drives the paged donor query and hands each page to the processing side.
        /// </summary>
        private async Task ReadDonorBatches(
            IAsyncEnumerable<List<DonorInfo>> batchedDonors,
            ChannelWriter<List<DonorInfo>> writer,
            Func<int> queueDepth,
            CancellationToken cancellationToken)
        {
            // An explicit enumerator rather than a foreach, so that MoveNextAsync can be timed - it is where each
            // page's query runs and its rows are reified, and a foreach would bury that in its own hidden
            // MoveNextAsync. Now that read and processing overlap, the read cost can no longer be inferred by
            // subtracting the batchProgress inner timings from the stage's wall clock, so it has to be measured
            // directly. Each page is already reified by the repository, one query per page, so there is no lazy work
            // left here to accidentally push back onto the processing side either.
            await using var donorBatches = batchedDonors.GetAsyncEnumerator(cancellationToken);

            while (true)
            {
                // Depth as it stood before this page was fetched, logged beside the fetch's own duration. Sitting at
                // zero means the processing side consumed the previous page the instant it arrived, so the pipeline is
                // delivering nothing - which is otherwise indistinguishable from success until the stage as a whole
                // fails to speed up. A healthy read side finds it at capacity.
                var depthBeforeRead = queueDepth();

                bool hasNextBatch;
                using (logger.RunTimed($"{ReadBatchTimingMessage} (QueueDepth: {depthBeforeRead})", LogLevel.Verbose))
                {
                    hasNextBatch = await donorBatches.MoveNextAsync();
                }

                if (!hasNextBatch)
                {
                    return;
                }

                await writer.WriteAsync(donorBatches.Current, cancellationToken);
            }
        }

        /// <summary>
        /// Takes pages from the read side, expands and writes their HLA, and advances the resume checkpoint.
        /// </summary>
        private async Task<List<FailedDonorInfo>> ProcessDonorBatches(
            ChannelReader<List<DonorInfo>> reader,
            string hlaNomenclatureVersion,
            Func<int, Task> updateLastSafelyProcessedDonorId,
            LongStopwatchCollection timerCollection,
            CancellationToken cancellationToken)
        {
            var failedDonors = new List<FailedDonorInfo>();

            // We only store the last Id in each batch so we only need to keep one Id per batch.
            var completedDonors = new FixedSizedQueue<int>(NumberOfBatchesOverlapOnRestart);

            await foreach (var donorBatch in reader.ReadAllAsync(cancellationToken))
            {
                // Checked here as well as by ReadAllAsync, and this is the check that matters: reading an
                // already-queued batch completes without ever consulting the token, so this is the only thing standing
                // between a cancelled refresh and another batch being written. Do not remove it as redundant.
                // Prefetched batches behind it are discarded rather than drained, which is safe precisely because this
                // stage keeps a checkpoint - it simply stays where it is, and those donors are read again on resume.
                cancellationToken.ThrowIfCancellationRequested();

                // The paging enumerator signals exhaustion by yielding one final empty batch, so this is the normal
                // end-of-stream path rather than an anomaly.
                if (!donorBatch.Any())
                {
                    continue;
                }

                // When continuing a donor import there will be some overlap of donors to ensure all donors are processed.
                // In this case, we will end up with duplicate p-groups in the matching hla tables.
                // Deleting p-groups is not suitably performant (as it involves deleting from an un-indexed table with potentially billions of rows)
                // The only downside to allowing duplicate p-groups is that the table has some redundant data and is slightly larger than necessary -
                // But this is insignificant compared to the full size of this table regardless.
                using (timerCollection.TimeInnerOperation(DataRefreshTimingKeys.BatchProgress_TimerKey))
                {
                    var failedDonorsFromBatch = await UpdateDonorBatch(
                        donorBatch,
                        hlaNomenclatureVersion,
                        timerCollection
                    );
                    failedDonors.AddRange(failedDonorsFromBatch);
                }

                // Deliberately on this side of the pipeline, and never the read side. The checkpoint records what has
                // been written; the read head now runs up to ChannelDepth + 1 pages ahead of it, and advancing it for a
                // page that had merely been prefetched would silently skip those donors when the refresh resumes.
                completedDonors.Enqueue(donorBatch.Last().DonorId);

                if (completedDonors.Count >= NumberOfBatchesOverlapOnRestart)
                {
                    await updateLastSafelyProcessedDonorId(completedDonors.Peek());
                }
            }

            return failedDonors;
        }

        private async Task<(int, int)> DetermineProgressAndReprocessingBoundaries(IReadOnlyCollection<List<DonorInfo>> overlapBatches)
        {
            var initialDonorToReprocess = overlapBatches.First().First();

            // Literally, the following query counts donors that exist in Donors table, < DonorIdX, but since donors
            // are imported strictly in order, that's equivalent to the number of processed donors already handled.
            var donorsPreviouslyProcessed = await dataRefreshRepository.GetDonorCountLessThan(initialDonorToReprocess.DonorId);

            var overlapDonors = overlapBatches.Take(DataRefreshRepository.NumberOfBatchesOverlapOnRestart).ToList();
            var lastDonorIdInOverlap = overlapDonors.Last().Last().DonorId;

            return (donorsPreviouslyProcessed, lastDonorIdInOverlap);
        }

        /// <summary>
        /// Fetches Expanded HLA information for all donors in a batch, and stores the processed  information in the database.
        /// </summary>
        /// <param name="donorBatch">The collection of donors to update</param>
        /// <param name="hlaNomenclatureVersion">The version of the HLA Nomenclature to use to fetch expanded HLA information</param>
        /// <param name="timerCollection"></param>
        /// <returns>A collection of donors that failed the import process.</returns>
        private async Task<IEnumerable<FailedDonorInfo>> UpdateDonorBatch(
            List<DonorInfo> donorBatch,
            string hlaNomenclatureVersion,
            LongStopwatchCollection timerCollection)
        {
            var donorHlaExpander = donorHlaExpanderFactory.BuildForSpecifiedHlaNomenclatureVersion(hlaNomenclatureVersion);

            var timedInnerOperation = timerCollection.TimeInnerOperation(DataRefreshTimingKeys.HlaExpansion_TimerKey);
            var hlaExpansionResults = await donorHlaExpander.ExpandDonorHlaBatchAsync(donorBatch, HlaFailureEventName);
            timedInnerOperation.Dispose();

            var hlaNameLookup = await hlaImportRepository.ImportHla(hlaExpansionResults.ProcessingResults);

            var donorEntries = hlaExpansionResults.ProcessingResults.Select(r => r.ToDonorInfoForPreProcessing(hlaName => hlaNameLookup[hlaName]));

            await donorImportRepository.AddMatchingRelationsForExistingDonorBatch(
                donorEntries,
                settings.DataRefreshDonorUpdatesShouldBeFullyTransactional,
                timerCollection);

            return hlaExpansionResults.FailedDonors;
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

                using (logger.RunTimed("HLA PROCESSOR: Caching all MACs", LogLevel.Info, true))
                {
                    // Donor HLA is riddled with MACs, and expanding one requires its definition. Without this, each
                    // distinct MAC costs its own storage request during expansion - ~567k of them on a full refresh.
                    // One streamed pass over the MAC table up front replaces the lot.
                    await macDictionary.PreWarmAllMacs();
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
                    await pGroupRepository.InsertPGroups(pGroups);
                }
            }
            catch (Exception e)
            {
                logger.SendException(e, LogLevel.Critical);
                throw;
            }
        }
    }
}