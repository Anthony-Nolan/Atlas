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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        Task UpdateDonorHla(
            string hlaNomenclatureVersion,
            Func<int, Task> updateLastSafelyProcessedDonorId,
            int? lastProcessedDonor = null,
            bool continueExistingImport = false);
    }

    public class HlaProcessor : IHlaProcessor
    {
        private const int BatchSize = 2000; // At 1k this definitely works fine. At 4k it's been seen throwing OOM Exceptions
        private const int BatchProgressReportingPeriod = 10; // Emit a human-readable progress/ETA trace every N batches.
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

        public const int NumberOfBatchesOverlapOnRestart = 3;

        public HlaProcessor(
            IMatchingAlgorithmImportLogger logger,
            IDonorHlaExpanderFactory donorHlaExpanderFactory,
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory,
            IFailedDonorsNotificationSender failedDonorsNotificationSender,
            IDormantRepositoryFactory repositoryFactory,
            DataRefreshSettings settings)
        {
            this.logger = logger;
            this.donorHlaExpanderFactory = donorHlaExpanderFactory;
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
            this.failedDonorsNotificationSender = failedDonorsNotificationSender;
            this.settings = settings;
            donorImportRepository = repositoryFactory.GetDonorImportRepository();
            dataRefreshRepository = repositoryFactory.GetDataRefreshRepository();
            pGroupRepository = repositoryFactory.GetPGroupRepository();
            hlaImportRepository = repositoryFactory.GetHlaImportRepository();
        }

        public async Task UpdateDonorHla(
            string hlaNomenclatureVersion,
            Func<int, Task> updateLastSafelyProcessedDonorId,
            int? lastProcessedDonor,
            bool continueExistingImport)
        {
            await PerformUpfrontSetup(hlaNomenclatureVersion);

            try
            {
                await PerformHlaUpdate(hlaNomenclatureVersion, updateLastSafelyProcessedDonorId, lastProcessedDonor, continueExistingImport);
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
            bool continueExistingProcessing)
        {
            var totalDonorCount = await dataRefreshRepository.GetDonorCount();
            var batchedDonors = dataRefreshRepository.NewOrderedDonorBatchesToImport(BatchSize, lastProcessedDonor);

            var overlapBatches = continueExistingProcessing
                ? await dataRefreshRepository.GetOrderedDonorBatches(NumberOfBatchesOverlapOnRestart, BatchSize, lastProcessedDonor ?? 0)
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
            var totalBatches = totalDonorCount / BatchSize;
            long batchesProcessed = 0;
            var stageStartTimestamp = Stopwatch.GetTimestamp();

            using (logger.TimeOperationAsMetric(
                       DataRefreshMetrics.DurationMsMetric,
                       DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_HlaProcessingStageTotal)
                   ))
            {
                // We only store the last Id in each batch so we only need to keep one Id per batch.
                var completedDonors = new FixedSizedQueue<int>(NumberOfBatchesOverlapOnRestart);

                await foreach (var donorBatch in batchedDonors)
                {
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
                        var failedDonorsFromBatch = await UpdateDonorBatch(donorBatch, hlaNomenclatureVersion);
                        failedDonors.AddRange(failedDonorsFromBatch);
                    }

                    completedDonors.Enqueue(donorBatch.Last().DonorId);

                    if (completedDonors.Count >= NumberOfBatchesOverlapOnRestart)
                    {
                        await updateLastSafelyProcessedDonorId(completedDonors.Peek());
                    }

                    if (++batchesProcessed % BatchProgressReportingPeriod == 0)
                    {
                        LogHlaProcessingProgress(batchesProcessed, totalBatches, stageStartTimestamp);
                    }
                }
            }

            if (failedDonors.Any())
            {
                await failedDonorsNotificationSender.SendFailedDonorsAlert(failedDonors, HlaFailureEventName, Priority.Low);
            }
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