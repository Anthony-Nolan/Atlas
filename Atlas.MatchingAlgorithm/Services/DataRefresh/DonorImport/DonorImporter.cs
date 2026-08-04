using Atlas.Client.Models.SupportMessages;
using Atlas.Common.ApplicationInsights;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Mapping;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Settings;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IDonorImportRepository = Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates.IDonorImportRepository;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.DonorImport
{
    /// <summary>
    /// Responsible for fetching all eligible donors for the search algorithm.
    /// Only responsible for one off import of all donors into the matching algorithm's data store. For individual updates, <see cref="IDonorUpdateProcessor"/>
    /// </summary>
    public interface IDonorImporter
    {
        /// <summary>
        /// Fetches all donors and stores their data in the donor table
        /// Does not perform analysis of donor p-groups
        /// </summary>
        /// <param name="shouldMarkDonorsAsUpdated">
        /// When set, all donors will have corresponding entries added to the donor management log table.
        /// These entries are *created*, never updated, so this assumes the log table holds no entries for the donors
        /// being imported. See <see cref="DonorImporter.InsertDonorBatch"/> for why that holds during a data refresh.
        /// </param>
        Task ImportDonors(bool shouldMarkDonorsAsUpdated = false);
    }

    public class DonorImporter : IDonorImporter
    {
        /// <summary>Historic hard-coded value; used whenever <see cref="DataRefreshSettings.DonorImportBatchSize"/> is unset.</summary>
        public const int DefaultBatchSize = 10000;

        private const string ImportFailureEventName = "Donor Import Failure(s) in the Matching Algorithm's DataRefresh";

        private readonly IDonorImportRepository matchingDonorImportRepository;
        private readonly IDonorManagementLogRepository donorManagementLogRepository;
        private readonly IDonorInfoConverter donorInfoConverter;
        private readonly IFailedDonorsNotificationSender failedDonorsNotificationSender;
        private readonly IMatchingAlgorithmImportLogger logger;
        private readonly IDonorReader donorReader;
        private readonly int batchSize;

        public DonorImporter(
            IDormantRepositoryFactory repositoryFactory,
            IDonorInfoConverter donorInfoConverter,
            IFailedDonorsNotificationSender failedDonorsNotificationSender,
            IMatchingAlgorithmImportLogger logger,
            IDonorReader donorReader,
            DataRefreshSettings dataRefreshSettings)
        {
            matchingDonorImportRepository = repositoryFactory.GetDonorImportRepository();
            donorManagementLogRepository = repositoryFactory.GetDonorManagementLogRepository();
            this.donorInfoConverter = donorInfoConverter;
            this.failedDonorsNotificationSender = failedDonorsNotificationSender;
            this.logger = logger;
            this.donorReader = donorReader;
            batchSize = dataRefreshSettings?.DonorImportBatchSize ?? DefaultBatchSize;
        }

        public async Task ImportDonors(bool shouldMarkDonorsAsUpdated)
        {
            try
            {
                var allFailedDonors = new List<FailedDonorInfo>();
                var donorsStream = donorReader.StreamAllDonors().Select(d => d.MapImportDonorToMatchingUpdateDonor());

                // Whole-stage duration, emitted as a (never-sampled) pre-aggregated metric.
                using (logger.TimeOperationAsMetric(
                           DataRefreshMetrics.DurationMsMetric,
                           DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_DonorImportStageTotal)
                       ))
                {
                    // Deliberately an explicit enumerator rather than a foreach. StreamAllDonors() is a synchronous,
                    // unbuffered IEnumerable over one open cross-DB connection, so the cost of actually reading donors
                    // out of SQL lands on the OUTER enumerator's MoveNext - not on the .ToList() below, and not
                    // anywhere else we already measure. Timing MoveNext is therefore the only way to see it; before
                    // this it was the single largest unmeasured slice of the whole job, visible only as the residual
                    // between DonorImportStageTotal and the sum of the DonorImportBatch spans.
                    //
                    // Note this span also covers the lazy MapImportDonorToMatchingUpdateDonor projection above, which
                    // is evaluated per donor during MoveNext. That is a small CPU cost sitting inside a
                    // predominantly-IO measurement; the §2.3 reconciliation is what proves the split, not this comment.
                    using var donorBatches = donorsStream.Batch(batchSize).GetEnumerator();

                    while (true)
                    {
                        bool hasNextBatch;
                        using (logger.TimeOperationAsMetric(
                            DataRefreshMetrics.DurationMsMetric,
                            DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_DonorStreamRead)))
                        {
                            hasNextBatch = donorBatches.MoveNext();
                        }

                        if (!hasNextBatch)
                        {
                            break;
                        }

                        var reifiedDonorBatch = donorBatches.Current.ToList();
                        var failedDonors = await InsertDonorBatch(reifiedDonorBatch, shouldMarkDonorsAsUpdated);
                        allFailedDonors.AddRange(failedDonors);
                    }
                }

                await failedDonorsNotificationSender.SendFailedDonorsAlert(allFailedDonors, ImportFailureEventName, Priority.Medium);
                logger.SendTrace("Donor import is complete");
            }
            catch (Exception ex)
            {
                // Surface the full exception (type + stack) as queryable Exception telemetry, not just the message text,
                // so a stage-40 (DonorImport) failure lands in the App Insights `exceptions` table rather than being
                // buried in a Trace. Dimensioned so it is picked up by the same query as every other refresh exception -
                // an undimensioned SendException falls outside it and reads as "it never happened".
                // Behaviour is otherwise unchanged - we still wrap and rethrow.
                logger.SendException(ex, LogLevel.Error, new Dictionary<string, string>
                {
                    ["DataRefreshStage"] = nameof(DataRefreshStage.DonorImport),
                    ["Disposition"] = "Wrapped as DonorImportHttpException and rethrown to the stage runner"
                });
                throw new DonorImportHttpException("Unable to complete donor import: " + ex.Message, ex);
            }
        }

        /// <param name="donors">Batch of donors to insert into the matching database.</param>
        /// <param name="shouldMarkDonorsAsUpdated"></param>
        /// <returns>Details of donors in the batch that failed import</returns>
        private async Task<IEnumerable<FailedDonorInfo>> InsertDonorBatch(
            List<SearchableDonorInformation> donors,
            bool shouldMarkDonorsAsUpdated)
        {
            // Timings are emitted as pre-aggregated metrics (never sampled), split into their CPU (conversion) vs DB
            // (Donors bulk insert / management-log write) components, so a single customMetrics query can show whether
            // Data Refresh stage 40 (DonorImport) is bound by the per-donor conversion loop or by the SQL writes.
            using (logger.TimeOperationAsMetric(
                       DataRefreshMetrics.DurationMsMetric,
                       DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_DonorImportBatch)
                   ))
            {
                // Sanity counter: every per-batch average above is only meaningful if the batches are the size we
                // think they are. A short final batch (or a short-changed stream) shows up here and nowhere else.
                logger.SendMetric(
                    DataRefreshMetrics.CountMetric,
                    donors.Count,
                    DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_DonorsPerImportBatch));

                var donorInfoConversionResult = await logger.RunTimedAsMetricAsync(
                    DataRefreshMetrics.DurationMsMetric,
                    DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_DonorInfoConversion),
                    () => donorInfoConverter.ConvertDonorInfoAsync(donors, ImportFailureEventName)
                );

                logger.SendMetric(
                    DataRefreshMetrics.CountMetric,
                    donorInfoConversionResult.FailedDonors.Count,
                    DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_FailedDonorsPerImportBatch));

                using (logger.TimeOperationAsMetric(
                           DataRefreshMetrics.DurationMsMetric,
                           DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_DonorBulkInsert)
                       ))
                {
                    await matchingDonorImportRepository.InsertBatchOfDonors(donorInfoConversionResult.ProcessingResults);
                }

                if (shouldMarkDonorsAsUpdated)
                {
                    using (logger.TimeOperationAsMetric(
                               DataRefreshMetrics.DurationMsMetric,
                               DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_DonorManagementLogWrite)
                           ))
                    {
                        // Deliberately create-only, rather than upsert. The donor management log table is always truncated before this stage runs -
                        // either by DataRefreshStage.DataDeletion, or, when continuing an interrupted refresh, by this stage restarting from scratch
                        // (see DataRefreshRunner.ExecuteDataRefreshStage). So every donor in a refresh resolves to a "create", and asking the
                        // database which donors already have log entries can only ever return none.
                        // That read used to cost ~1hr of a ~15hr refresh: one non-parameterised `WHERE DonorId IN (<10,000 ids>)` query per batch,
                        // ~88KB of SQL text each, every one of them a fresh parse and plan.
                        // If a future change lets this stage run against a log table that was NOT truncated, this must go back to being an upsert -
                        // there is a unique index on DonorId, so a create-only write would throw instead of updating.
                        await donorManagementLogRepository.CreateDonorManagementLogBatch(donors.Select(d => new DonorManagementInfo
                            {
                                DonorId = d.DonorId,
                                UpdateDateTime = d.LastUpdated,
                                // This assumes that all updates come from a service bus message, which is incorrect for the initial donor import
                                // TODO: ATLAS-972: Confirm this is unused and remove
                                UpdateSequenceNumber = -1
                            }
                        ));
                    }
                }

                return donorInfoConversionResult.FailedDonors;
            }
        }
    }
}
