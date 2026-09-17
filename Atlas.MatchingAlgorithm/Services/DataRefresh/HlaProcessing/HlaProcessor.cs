using Atlas.Client.Models.SupportMessages;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Utils;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// <summary>
        /// Historic hard-coded value; used whenever <see cref="DataRefreshSettings.HlaProcessingBatchSize"/> is unset.
        /// At 1k this definitely works fine. At 4k it's been seen throwing OOM Exceptions - though that claim is
        /// undated folklore, which the runtime sampler's WorkingSetMb finally makes checkable.
        /// </summary>
        public const int DefaultBatchSize = 2000;

        /// <summary>Emit a human-readable progress/ETA trace every N batches.</summary>
        public const int DefaultBatchProgressReportingPeriod = 10;

        private const string HlaFailureEventName = "Imported Donor Hla Processing Failure(s) in the Matching Algorithm's DataRefresh";

        private readonly IMatchingAlgorithmImportLogger logger;
        private readonly IDonorHlaExpanderFactory donorHlaExpanderFactory;
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;
        private readonly IFailedDonorsNotificationSender failedDonorsNotificationSender;
        private readonly DataRefreshSettings settings;
        private readonly IDonorImportRepository donorImportRepository;
        private readonly IDataRefreshRepository dataRefreshRepository;
        private readonly IPGroupRepository pGroupRepository;
        private readonly IHlaImportRepository hlaImportRepository;
        private readonly IMacDictionary macDictionary;
        private readonly IDataRefreshPipelineGauges pipelineGauges;
        private readonly int batchSize;
        private readonly int batchProgressReportingPeriod;

        public const int NumberOfBatchesOverlapOnRestart = 3;

        /// <summary>
        /// How many donor pages the read side may run ahead of the processing side.
        /// </summary>
        /// <remarks>
        /// Processing measured roughly twice the read, so in steady state the read side is always waiting and a depth
        /// of 1 would recover nearly all of the available time. The second rung is there only to absorb variance in
        /// individual page read times, and is cheap: a page is raw <see cref="DonorInfo"/>, an order of magnitude
        /// smaller than the expanded HLA the processing side builds from it, so a rung costs far less than the
        /// <see cref="DefaultBatchSize"/> of 2000 rows suggests. Deeper buys nothing while processing remains the slower side
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
            IMacDictionary macDictionary,
            IDataRefreshPipelineGauges pipelineGauges)
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
            hlaImportRepository = repositoryFactory.GetHlaImportRepository();
            batchSize = settings?.HlaProcessingBatchSize ?? DefaultBatchSize;
            batchProgressReportingPeriod = settings?.BatchProgressReportingPeriod ?? DefaultBatchProgressReportingPeriod;

            // Defaulted rather than required - see DonorImporter for why an instrument must not be able to stop a run.
            this.pipelineGauges = pipelineGauges ?? new DataRefreshPipelineGauges();
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
                // Dimensioned so this lands in the same exception query as every other refresh failure - an
                // undimensioned SendException falls outside it and reads as "it never happened".
                logger.SendException(e, LogLevel.Critical, new Dictionary<string, string>
                {
                    ["DataRefreshStage"] = nameof(DataRefreshStage.DonorHlaProcessing),
                    ["Disposition"] = "Rethrown to the stage runner"
                });
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
            var batchedDonors = dataRefreshRepository.NewOrderedDonorBatchesToImport(batchSize, lastProcessedDonor, cancellationToken);

            var overlapBatches = continueExistingProcessing
                ? await dataRefreshRepository.GetOrderedDonorBatches(NumberOfBatchesOverlapOnRestart, batchSize, lastProcessedDonor ?? 0)
                : new List<List<DonorInfo>>();

            var (donorsPreviouslyProcessed, _) = continueExistingProcessing
                ? await DetermineProgressAndReprocessingBoundaries(overlapBatches)
                : (0, 0);
            var failedDonors = new List<FailedDonorInfo>();
            var donorsToImport = totalDonorCount - donorsPreviouslyProcessed;

            if (continueExistingProcessing)
            {
                logger.SendTrace($"Hla Processing continuing. {donorsPreviouslyProcessed} donors previously processed. {donorsToImport} remain.");
            }

            // Timings below are emitted as pre-aggregated Application Insights metrics (DataRefreshMetrics.DurationMsMetric)
            // rather than as Trace summaries. The old LongStopwatchCollection wrote all of its summaries as Traces in one
            // synchronous burst when this using-block unwound at stage completion; the isolated worker's adaptive sampling
            // (which host.json's excludedTypes does NOT govern for direct-to-App-Insights worker logs) then dropped them,
            // since they shared one OperationId. Metrics are never sampled, so they always survive.
            var totalBatches = totalDonorCount / batchSize;
            var stageStartTimestamp = Stopwatch.GetTimestamp();

            using (logger.TimeOperationAsMetric(
                       DataRefreshMetrics.DurationMsMetric,
                       DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_HlaProcessingStageTotal)
                   ))
            // One session for the whole stage, not one per batch - see IDonorImportRepository.OpenBulkWriteSession.
            // Wraps the whole pipeline rather than the processing side alone: every write happens on that side, so the
            // session is still opened and closed exactly once, and is never touched by the concurrent read side.
            using (donorImportRepository.OpenBulkWriteSession())
            {
                failedDonors.AddRange(await RunHlaProcessingPipeline(
                    batchedDonors,
                    hlaNomenclatureVersion,
                    updateLastSafelyProcessedDonorId,
                    totalBatches,
                    stageStartTimestamp,
                    cancellationToken));
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
            long totalBatches,
            long stageStartTimestamp,
            CancellationToken cancellationToken) =>
            PrefetchPipeline.Run<List<DonorInfo>, List<FailedDonorInfo>>(
                ChannelDepth,
                (writer, queueDepth, token) => ReadDonorBatches(batchedDonors, writer, queueDepth, token),
                (reader, token) => ProcessDonorBatches(
                    reader, hlaNomenclatureVersion, updateLastSafelyProcessedDonorId, totalBatches, stageStartTimestamp, token),
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
                // Timed twice deliberately. The metric always survives - pre-aggregated, so never sampled and
                // independent of the deployed log level - and is what the stage's occupancy is computed from. The
                // Verbose Trace beside it adds the queue depth, which a low-cardinality metric dimension cannot carry.
                using (logger.TimeOperationAsMetric(
                           DataRefreshMetrics.DurationMsMetric,
                           DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_HlaDonorBatchRead)
                       ))
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
            long totalBatches,
            long stageStartTimestamp,
            CancellationToken cancellationToken)
        {
            var failedDonors = new List<FailedDonorInfo>();

            // We only store the last Id in each batch so we only need to keep one Id per batch.
            var completedDonors = new FixedSizedQueue<int>(NumberOfBatchesOverlapOnRestart);

            // Counted on this side rather than in PerformHlaUpdate: it is batches *processed* that the progress line and
            // its ETA are about, and the read head now runs up to ChannelDepth + 1 pages ahead of that.
            long batchesProcessed = 0;

            await foreach (var donorBatch in reader.ReadAllAsync(cancellationToken))
            {
                // Checked here as well as by ReadAllAsync, and this is the check that matters: reading an
                // already-queued batch completes without ever consulting the token, so this is the only thing standing
                // between a cancelled refresh and another batch being written. Do not remove it as redundant.
                // Prefetched batches behind it are discarded rather than drained, which is safe precisely because this
                // stage keeps a checkpoint - it simply stays where it is, and those donors are read again on resume.
                cancellationToken.ThrowIfCancellationRequested();

                // How much the read side had ready behind this batch. Read by the runtime sampler on its own interval,
                // and the only thing that distinguishes a prefetch that is keeping up from one that is starving this
                // loop - the two are identical in the stage's wall clock.
                pipelineGauges.HlaBatchPrefetch.RecordDepthOnTake(reader.Count);

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
                using (logger.TimeOperationAsMetric(
                           DataRefreshMetrics.DurationMsMetric,
                           DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_BatchProcessing)
                       ))
                {
                    // Sanity counter - see DonorsPerImportBatch. Named distinctly from stage 40's so a short
                    // final batch in one loop cannot skew the other loop's per-batch distribution.
                    logger.SendMetric(
                        DataRefreshMetrics.CountMetric,
                        donorBatch.Count,
                        DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_DonorsPerHlaBatch));

                    var failedDonorsFromBatch = (await UpdateDonorBatch(donorBatch, hlaNomenclatureVersion)).ToList();

                    logger.SendMetric(
                        DataRefreshMetrics.CountMetric,
                        failedDonorsFromBatch.Count,
                        DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_FailedDonorsPerHlaBatch));

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

                if (++batchesProcessed % batchProgressReportingPeriod == 0)
                {
                    LogHlaProcessingProgress(batchesProcessed, totalBatches, stageStartTimestamp);
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
        /// <returns>A collection of donors that failed the import process.</returns>
        private async Task<IEnumerable<FailedDonorInfo>> UpdateDonorBatch(
            List<DonorInfo> donorBatch,
            string hlaNomenclatureVersion)
        {
            var donorHlaExpander = donorHlaExpanderFactory.BuildForSpecifiedHlaNomenclatureVersion(hlaNomenclatureVersion);

            var hlaExpansionResults = await logger.RunTimedAsMetricAsync(
                DataRefreshMetrics.DurationMsMetric,
                DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_HlaExpansion),
                () => donorHlaExpander.ExpandDonorHlaBatchAsync(donorBatch, HlaFailureEventName)
            );

            // ImportHla is (per the spike profile) the largest single slice of stage-50 user-code, so time the whole call
            // as one operation here; IHlaImportRepository decomposes it further into its DB-read / CPU / DB-write parts.
            var hlaNameLookup = await logger.RunTimedAsMetricAsync(
                DataRefreshMetrics.DurationMsMetric,
                DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_ImportHlaOverall),
                () => hlaImportRepository.ImportHla(hlaExpansionResults.ProcessingResults)
            );

            var donorEntries = hlaExpansionResults.ProcessingResults.Select(r => r.ToDonorInfoForPreProcessing(hlaName => hlaNameLookup[hlaName]));

            await donorImportRepository.AddMatchingRelationsForExistingDonorBatch(
                donorEntries,
                settings.DataRefreshDonorUpdatesShouldBeFullyTransactional
            );

            return hlaExpansionResults.FailedDonors;
        }

        /// <summary>
        /// Emits a low-frequency, human-readable progress line (with a linearly-extrapolated ETA) as a Trace.
        /// This is a genuine log line - unlike the timing measurements, it is fine for it to be sampled - and replaces
        /// the old LongOperationLoggingStopwatch "Progress:" reporting.
        /// </summary>
        private void LogHlaProcessingProgress(long batchesProcessed, long totalBatches, long stageStartTimestamp)
        {
            var elapsed = Stopwatch.GetElapsedTime(stageStartTimestamp);
            var fractionComplete = totalBatches > 0 ? (double)batchesProcessed / totalBatches : 0;

            var message = $"HLA Processing progress: {batchesProcessed}/{totalBatches} batches";
            if (fractionComplete > 0)
            {
                var projectedTotal = TimeSpan.FromTicks((long)(elapsed.Ticks / fractionComplete));
                var projectedCompletion = DateTime.UtcNow.Add(projectedTotal - elapsed);
                message += $" ({fractionComplete:P1}). Projected completion: {projectedCompletion:u}.";
            }

            logger.SendTrace(message);
        }

        private async Task PerformUpfrontSetup(string hlaNomenclatureVersion)
        {
            try
            {
                // The only two stage-50 setup numbers there are. They used to be RunTimed Traces, i.e. sampleable -
                // and the worker's adaptive sampling is exactly what lost the old timing traces. As metrics they
                // always survive, and they can be read in the same query as everything else in the stage.
                using (logger.TimeOperationAsMetric(
                           DataRefreshMetrics.DurationMsMetric,
                           DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_HmdPreWarm)))
                {
                    // Cloud tables are cached for performance reasons
                    var dictionaryCacheControl = hlaMetadataDictionaryFactory.BuildCacheControl(hlaNomenclatureVersion);
                    await dictionaryCacheControl.PreWarmAllCaches();
                }

                using (logger.TimeOperationAsMetric(
                           DataRefreshMetrics.DurationMsMetric,
                           DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_MacPreWarm)))
                {
                    // Donor HLA is riddled with MACs, and expanding one requires its definition. Without this, each
                    // distinct MAC costs its own storage request during expansion - ~567k of them on a full refresh.
                    // One streamed pass over the MAC table up front replaces the lot.
                    await macDictionary.PreWarmAllMacs();
                }

                using (logger.TimeOperationAsMetric(
                           DataRefreshMetrics.DurationMsMetric,
                           DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_UpfrontPGroupInsert)))
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
                logger.SendException(e, LogLevel.Critical, new Dictionary<string, string>
                {
                    ["DataRefreshStage"] = nameof(DataRefreshStage.DonorHlaProcessing),
                    ["Disposition"] = "Failed during upfront setup (HMD pre-warm / p-group insert); rethrown"
                });
                throw;
            }
        }
    }
}