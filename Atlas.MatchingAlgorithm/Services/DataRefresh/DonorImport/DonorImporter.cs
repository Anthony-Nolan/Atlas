using Atlas.Client.Models.SupportMessages;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Models;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Mapping;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.MatchingAlgorithm.Services.Donors;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
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
        /// <param name="cancellationToken">
        /// Cancelled if the data refresh loses its run-level lease. Observed only between batch writes, so an
        /// interrupted import always stops on a batch boundary; batches already buffered by the read side are discarded.
        /// </param>
        Task ImportDonors(bool shouldMarkDonorsAsUpdated = false, CancellationToken cancellationToken = default);
    }

    public class DonorImporter : IDonorImporter
    {
        private const int BatchSize = 10000;

        /// <summary>
        /// How many reified batches the read side may run ahead of the write side. Read and write cost about the same,
        /// so one rung is enough to keep both busy and the rest only absorb variance. Each costs <see cref="BatchSize"/>
        /// reified donors against a stage already peaking near 4.6GB of a ~14GB worker, so re-measure that headroom
        /// before raising it.
        /// </summary>
        private const int ChannelDepth = 3;

        private const string ImportFailureEventName = "Donor Import Failure(s) in the Matching Algorithm's DataRefresh";

        /// <summary>
        /// Read durations, summed with the write traces from <see cref="InsertDonorBatch"/> and divided by the stage's
        /// wall clock, give the stage's occupancy: ~1 when fully serial, ~2 when the pipeline is working.
        /// </summary>
        private const string ReadBatchTimingMessage = "Read donor batch from master donor store";

        private readonly IDonorImportRepository matchingDonorImportRepository;
        private readonly IDonorManagementLogRepository donorManagementLogRepository;
        private readonly IDonorInfoConverter donorInfoConverter;
        private readonly IFailedDonorsNotificationSender failedDonorsNotificationSender;
        private readonly IMatchingAlgorithmImportLogger logger;
        private readonly IDonorReader donorReader;

        public DonorImporter(
            IDormantRepositoryFactory repositoryFactory,
            IDonorInfoConverter donorInfoConverter,
            IFailedDonorsNotificationSender failedDonorsNotificationSender,
            IMatchingAlgorithmImportLogger logger,
            IDonorReader donorReader)
        {
            matchingDonorImportRepository = repositoryFactory.GetDonorImportRepository();
            donorManagementLogRepository = repositoryFactory.GetDonorManagementLogRepository();
            this.donorInfoConverter = donorInfoConverter;
            this.failedDonorsNotificationSender = failedDonorsNotificationSender;
            this.logger = logger;
            this.donorReader = donorReader;
        }

        public async Task ImportDonors(bool shouldMarkDonorsAsUpdated, CancellationToken cancellationToken)
        {
            try
            {
                var allFailedDonors = await RunImportPipeline(shouldMarkDonorsAsUpdated, cancellationToken);

                await failedDonorsNotificationSender.SendFailedDonorsAlert(allFailedDonors, ImportFailureEventName, Priority.Medium);
                logger.SendTrace("Donor import is complete");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Deliberately not wrapped as a DonorImportHttpException. Cancellation means the refresh lost its lease,
                // which is recognised and handled distinctly further up the chain, and it is not an import failure.
                throw;
            }
            catch (Exception ex)
            {
                logger.SendTrace($"Donor Import Failed: {ex.Message}", LogLevel.Error);
                throw new DonorImportHttpException("Unable to complete donor import: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Reads donors from the master store and writes them to the matching database concurrently, rather than
        /// alternating between the two.
        /// </summary>
        /// <remarks>
        /// The two contend for nothing - the read is network-bound on the master store, the write a bulk insert into a
        /// different database - so serially the stage costs read + write, and overlapped roughly max(read, write). On a
        /// 43.9M donor refresh, ~254 minutes against ~128.
        /// </remarks>
        private async Task<List<FailedDonorInfo>> RunImportPipeline(bool shouldMarkDonorsAsUpdated, CancellationToken cancellationToken)
        {
            var batches = Channel.CreateBounded<List<SearchableDonorInformation>>(
                new BoundedChannelOptions(ChannelDepth)
                {
                    SingleReader = true,
                    SingleWriter = true,
                    FullMode = BoundedChannelFullMode.Wait
                });

            // Linked, so a write-side failure tears the read side down too. Otherwise it would block forever on a full
            // channel, holding open the cross-database connection it enumerates.
            using var readCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var readTask = Task.Run(() => ReadDonorBatches(batches.Writer, readCancellation.Token), CancellationToken.None);

            try
            {
                return await WriteDonorBatches(batches.Reader, shouldMarkDonorsAsUpdated, cancellationToken);
            }
            finally
            {
                // Cancel before awaiting: a read side blocked on a full channel has to be released before it can
                // terminate. Awaiting at all is what stops this method returning, by any path, while a thread is still
                // enumerating the master donor store.
                await readCancellation.CancelAsync();
                await AwaitReadTaskQuietly(readTask);
            }
        }

        /// <summary>
        /// Drives the donor enumerator and hands reified batches to the write side.
        /// </summary>
        /// <remarks>
        /// <see cref="IDonorReader.StreamAllDonors"/> is a synchronous, unbuffered <see cref="IEnumerable{T}"/> over one
        /// open cross-database connection with no async equivalent, so the enumeration is pushed onto the pool rather
        /// than the enumerable forced to be async. Expect it to hold a pool thread for most of the stage, not just for
        /// each blocking read: the thread is only given back while the channel is full, and the read being the slower
        /// side means it rarely is. Accepted rather than given a dedicated thread - it is one thread, on a worker
        /// measured at 1.7% of 4 vCPUs here. Revisit if the write side is ever seen waiting on thread injection.
        /// </remarks>
        private async Task ReadDonorBatches(ChannelWriter<List<SearchableDonorInformation>> writer, CancellationToken cancellationToken)
        {
            try
            {
                var donorsStream = donorReader.StreamAllDonors().Select(d => d.MapImportDonorToMatchingUpdateDonor());

                // An explicit enumerator rather than a foreach, so that MoveNext can be timed - it is where donors are
                // pulled out of SQL and the projection above is evaluated, and a foreach would bury that in its own
                // hidden MoveNext. Now that read and write overlap, the read cost can no longer be inferred by
                // subtracting write timings from the stage's wall clock, so it has to be measured directly.
                using var donorBatches = donorsStream.Batch(BatchSize).GetEnumerator();

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    bool hasNextBatch;
                    using (logger.RunTimed(ReadBatchTimingMessage, LogLevel.Verbose))
                    {
                        hasNextBatch = donorBatches.MoveNext();
                    }

                    if (!hasNextBatch)
                    {
                        break;
                    }

                    // Reified on this thread deliberately: handing over a lazy sequence would move the cost of reading
                    // those donors onto the write side, which is the serialisation this pipeline exists to remove.
                    await writer.WriteAsync(donorBatches.Current.ToList(), cancellationToken);
                }

                writer.TryComplete();
            }
            catch (Exception e)
            {
                // The channel is how a read-side failure reaches the write side, and the only way it does - rethrowing
                // as well would fault this task with the same exception, for whichever of the two paths won. Note that
                // ReadAllAsync surfaces it unwrapped, unlike ReadAsync, so it keeps its type: that is what lets
                // ImportDonors still tell cancellation from failure now the exception crosses threads to get there.
                writer.TryComplete(e);
            }
        }

        /// <summary>
        /// Takes reified batches from the read side and writes them to the matching database.
        /// </summary>
        private async Task<List<FailedDonorInfo>> WriteDonorBatches(
            ChannelReader<List<SearchableDonorInformation>> reader,
            bool shouldMarkDonorsAsUpdated,
            CancellationToken cancellationToken)
        {
            var allFailedDonors = new List<FailedDonorInfo>();

            await foreach (var reifiedDonorBatch in reader.ReadAllAsync(cancellationToken))
            {
                // Checked here as well as by ReadAllAsync, and this is the check that matters: reading an
                // already-buffered batch completes without ever consulting the token. Batches behind it are discarded
                // rather than drained - the stage keeps no checkpoint, and once the lease is lost the matching database
                // belongs to another invocation.
                cancellationToken.ThrowIfCancellationRequested();

                var failedDonors = await InsertDonorBatch(
                    reifiedDonorBatch, shouldMarkDonorsAsUpdated, reader.CanCount ? reader.Count : null);
                allFailedDonors.AddRange(failedDonors);
            }

            return allFailedDonors;
        }

        private async Task AwaitReadTaskQuietly(Task readTask)
        {
            try
            {
                await readTask;
            }
            catch (Exception e)
            {
                // Read-side failures reach the caller through the channel, so anything here was either already reported
                // by that route or is the cancellation just requested. Either way it must not displace the exception
                // already propagating out of the pipeline.
                logger.SendTrace($"Donor read task ended with an exception: {e}", LogLevel.Verbose);
            }
        }

        /// <param name="donors">Batch of donors to insert into the matching database.</param>
        /// <param name="shouldMarkDonorsAsUpdated"></param>
        /// <param name="queueDepth">
        ///     Batches the read side had ready when this write began, or null if the channel cannot report it. Logged
        ///     beside the write's own duration: a depth sitting at zero means the write side is starved and the
        ///     pipeline is delivering nothing, which is otherwise indistinguishable from success until the stage as a
        ///     whole fails to speed up.
        /// </param>
        /// <param name="batchFetchTime">
        ///     Time at which this batch were fetched from the master donor store, to be used as the "last updated" time of these donors.
        ///     It is slightly more correct to use the fetch time than the insert time, in the case of a race condition where a new update is published between
        ///     fetching a batch from the donor store, and inserting it into the donor management log table.
        /// </param>
        /// <returns>Details of donors in the batch that failed import</returns>
        private async Task<IEnumerable<FailedDonorInfo>> InsertDonorBatch(
            List<SearchableDonorInformation> donors,
            bool shouldMarkDonorsAsUpdated,
            int? queueDepth)
        {
            using (logger.RunTimed($"Import donor batch (BatchSize: {BatchSize}, QueueDepth: {queueDepth?.ToString() ?? "unknown"})",
                       LogLevel.Verbose))
            {
                var donorInfoConversionResult = await donorInfoConverter.ConvertDonorInfoAsync(donors, ImportFailureEventName);
                await matchingDonorImportRepository.InsertBatchOfDonors(donorInfoConversionResult.ProcessingResults);

                if (shouldMarkDonorsAsUpdated)
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

                return donorInfoConversionResult.FailedDonors;
            }
        }
    }
}
