using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchingAlgorithm.Common.Config;
using Atlas.MatchingAlgorithm.Data.Helpers;
using Atlas.MatchingAlgorithm.Data.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using Microsoft.Data.SqlClient;

// ReSharper disable InconsistentNaming

namespace Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates
{
    public abstract class DonorUpdateRepositoryBase : Repository
    {
        protected readonly IAtlasLogger logger;

        private const string DonorsTableName = "Donors";

        private const int DefaultBulkInsertTimeoutInSeconds = 3600;

        /// <summary>
        /// The matching HLA tables are the largest writes in the system, hence a far longer timeout than
        /// <see cref="DefaultBulkInsertTimeoutInSeconds"/>. Shared with <see cref="BuildReusableBulkCopies"/>, which
        /// pre-builds those tables' bulk copies, so that the two cannot drift apart.
        /// </summary>
        private const int MatchingHlaBulkInsertTimeoutInSeconds = 14400;

        /// <summary>
        /// Non-null only between <see cref="OpenBulkWriteSession"/> and the disposal of what it returned.
        /// </summary>
        private BulkCopySession activeBulkCopySession;

        // The order of these matters when setting up the datatable - if re-ordering, also re-order datatable contents
        private readonly string[] donorInsertDataTableColumnNames =
        {
            "Id",
            "DonorId",
            "DonorType",
            "A_1",
            "A_2",
            "B_1",
            "B_2",
            "C_1",
            "C_2",
            "DPB1_1",
            "DPB1_2",
            "DQB1_1",
            "DQB1_2",
            "DRB1_1",
            "DRB1_2",
            nameof(Donor.ExternalDonorCode),
            nameof(Donor.EthnicityCode),
            nameof(Donor.RegistryCode)
        };

        // The order of these matters when setting up the datatable - if re-ordering, also re-order datatable contents
        private readonly string[] donorPGroupDataTableColumnNames =
        {
            "Id",
            "DonorId",
            "TypePosition",
            "HlaNameId"
        };

        protected DonorUpdateRepositoryBase(IConnectionStringProvider connectionStringProvider, IAtlasLogger logger) : base(connectionStringProvider)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Opens a session over which this repository's bulk copies are reused, rather than rebuilt per write.
        /// Dispose the returned handle to close it.
        /// </summary>
        /// <remarks>
        /// A <see cref="SqlBulkCopy"/> asks the server to describe its destination table before sending any rows, and
        /// that description is cached per instance, so rebuilding one per write pays for a round trip that only ever
        /// returns the same answer. Over a full data refresh that came to over an hour of waiting on the destination
        /// metadata catalogue queries for a few minutes of server time.
        ///
        /// Not every write can take part - see <see cref="GetReusableBulkCopy"/> - so this is an optimisation that a
        /// caller opts into for a whole stage, not a change to how any individual write behaves.
        /// </remarks>
        public IDisposable OpenBulkWriteSession()
        {
            if (activeBulkCopySession != null)
            {
                throw new InvalidOperationException($"A bulk write session is already open on this {GetType().Name}.");
            }

            activeBulkCopySession = new BulkCopySession(this);
            return activeBulkCopySession;
        }

        public async Task InsertBatchOfDonors(IEnumerable<DonorInfo> donors)
        {
            var donorInfos = donors.ToList();

            if (!donorInfos.Any())
            {
                return;
            }

            var dataTable = BuildDonorInsertDataTable(donorInfos);

            await BulkInsertDataTable(DonorsTableName, dataTable, donorInsertDataTableColumnNames);
        }

        public async Task AddMatchingRelationsForExistingDonorBatch(
            IEnumerable<DonorInfoForHlaPreProcessing> donorInfos,
            bool runAllHlaInsertionsInASingleTransactionScope,
            LongStopwatchCollection timerCollection = null)
        {
            var donorsWithUpdatesAtEveryLocus = donorInfos
                .Select(info => new DonorWithChangedMatchingLoci(info, LocusSettings.MatchingOnlyLoci))
                .ToList();

            using (timerCollection?.TimeInnerOperation(DataRefreshTimingKeys.HlaUpsert_Overall_TimerKey))
            {
                await UpsertMatchingPGroupsAtSpecifiedLoci(
                    donorsWithUpdatesAtEveryLocus,
                    true,
                    runAllHlaInsertionsInASingleTransactionScope,
                    timerCollection);
            }
        }

        protected class DonorWithChangedMatchingLoci
        {
            public DonorInfoForHlaPreProcessing DonorInfo { get; }
            public ISet<Locus> ChangedMatchingLoci { get; }

            public DonorWithChangedMatchingLoci(DonorInfoForHlaPreProcessing donorInfo, ISet<Locus> changedMatchingLoci)
            {
                DonorInfo = donorInfo;
                ChangedMatchingLoci = changedMatchingLoci;
            }
        }

        protected async Task UpsertMatchingPGroupsAtSpecifiedLoci(
            List<DonorWithChangedMatchingLoci> donors,
            bool isKnownToBeCreate,
            bool runAllHlaInsertionsInASingleTransactionScope,
            LongStopwatchCollection timerCollection = null)
        {
            using (var transactionScope = new OptionalAsyncTransactionScope(runAllHlaInsertionsInASingleTransactionScope))
            {
                var perLocusUpsertTasks = new List<Task>();
                foreach (var locus in LocusSettings.MatchingOnlyLoci)
                {
                    var donorsWhichChangedAtThisLocus = donors
                        .Where(d => d.ChangedMatchingLoci.Contains(locus))
                        .Select(d => d.DonorInfo)
                        .ToList();

                    if (donorsWhichChangedAtThisLocus.Any())
                    {
                        var insertSetupOperationTimer =
                            timerCollection?.TimeInnerOperation(DataRefreshTimingKeys.HlaUpsert_BulkInsertSetup_Overall_TimerKey);
                        var upsertTask = UpsertMatchingPGroupsAtLocus(
                            donorsWhichChangedAtThisLocus,
                            locus,
                            isKnownToBeCreate,
                            timerCollection);
                        perLocusUpsertTasks.Add(upsertTask);
                        insertSetupOperationTimer?.Dispose();

                        // This is a bit sad.
                        // BulkInserting to unrelated tables, should be an easy win for
                        // "don't await the Tasks separately, use Task.WhenAll() and let them run in parallel".
                        // And that DOES work ... if you can start separate connections for each one.
                        //
                        // But currently our TransactionScope requires that there only be a single connection at a
                        // time, due to limitations of .NET Core 3. See ATLAS-562 for more notes.
                        //
                        // Due to the nature of MARS, if you WhenAll() with a shared transaction you lose all
                        // the perf benefits.
                        // 
                        // See here for more detail of the tests done, the perf results achieved and the probable
                        // cause of the problem.
                        // https://stackoverflow.com/questions/62970038/performance-of-multiple-parallel-async-sqlbulkcopy-inserts-against-different
                        if (runAllHlaInsertionsInASingleTransactionScope)
                        {
                            using (timerCollection?.TimeInnerOperation(DataRefreshTimingKeys.HlaUpsert_BlockingWait_TimerKey))
                            {
                                await upsertTask;
                            }
                        }
                    }
                }

                // Note that we may have already awaited these tasks to support TransactionScope.
                // In that case this `WhenAll` is a no-op. But it makes the difference
                // between the two cases easy to define.
                using (timerCollection?.TimeInnerOperation(DataRefreshTimingKeys.HlaUpsert_BlockingWait_TimerKey))
                {
                    await Task.WhenAll(perLocusUpsertTasks);
                }

                transactionScope.Complete();
            }
        }

        private async Task UpsertMatchingPGroupsAtLocus(
            List<DonorInfoForHlaPreProcessing> donors,
            Locus locus,
            bool isKnownToBeCreate,
            LongStopwatchCollection timerCollection = null)
        {
            var matchingTableName = MatchingHla.TableName(locus);

            var buildDataTableTimer =
                timerCollection?.TimeInnerOperation(DataRefreshTimingKeys.HlaUpsert_BulkInsertSetup_BuildDataTable_Overall_TimerKey);
            var dataTable = BuildPerLocusPGroupDataTable(donors, locus, timerCollection);
            buildDataTableTimer?.Dispose();

            // The scope is here to make the DELETE below and the insert that follows it one atomic unit. On the create
            // path there is no DELETE to be atomic with, and the insert is already atomic in its own right: the
            // UseInternalTransaction option commits per bulk-copy batch, and one donor batch's rows at one locus
            // (2 * HlaProcessor.BatchSize at the very most) never reach the 10,000 row batch size.
            //
            // Skipping it there is also what leaves the reusable bulk copies usable - see GetReusableBulkCopy. Where a
            // caller asked for the whole upsert to be transactional, the outer scope opened by
            // UpsertMatchingPGroupsAtSpecifiedLoci is ambient regardless, and this scope only ever joined it.
            using (var transactionScope = new OptionalAsyncTransactionScope(!isKnownToBeCreate))
            {
                using (timerCollection?.TimeInnerOperation(DataRefreshTimingKeys.HlaUpsert_BulkInsertSetup_DeleteExistingRecords_TimerKey))
                {
                    if (!isKnownToBeCreate)
                    {
                        var deleteSql = $@"
                            DELETE FROM {matchingTableName}
                            WHERE DonorId IN ({donors.Select(d => d.DonorId.ToString()).StringJoin(",")})
                            ";
                        await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
                        {
                            await conn.ExecuteAsync(deleteSql, null, commandTimeout: 600);
                        }
                    }
                }

                await BulkInsertDataTable(
                    matchingTableName,
                    dataTable,
                    donorPGroupDataTableColumnNames,
                    timeout: MatchingHlaBulkInsertTimeoutInSeconds,
                    timerCollection?.GetStopwatch(DataRefreshTimingKeys.HlaUpsert_DtWriteExecution_TimerKey));

                transactionScope.Complete();
            }
        }

        private DataTable BuildDonorInsertDataTable(IEnumerable<DonorInfo> donorInfos)
        {
            var dataTable = new DataTable();
            foreach (var columnName in donorInsertDataTableColumnNames)
            {
                dataTable.Columns.Add(columnName);
            }

            foreach (var donor in donorInfos)
            {
                dataTable.Rows.Add(
                    0,
                    donor.DonorId,
                    (int) donor.DonorType,
                    donor.HlaNames.A.Position1,
                    donor.HlaNames.A.Position2,
                    donor.HlaNames.B.Position1,
                    donor.HlaNames.B.Position2,
                    donor.HlaNames.C.Position1,
                    donor.HlaNames.C.Position2,
                    donor.HlaNames.Dpb1.Position1,
                    donor.HlaNames.Dpb1.Position2,
                    donor.HlaNames.Dqb1.Position1,
                    donor.HlaNames.Dqb1.Position2,
                    donor.HlaNames.Drb1.Position1,
                    donor.HlaNames.Drb1.Position2,
                    donor.ExternalDonorCode,
                    donor.EthnicityCode,
                    donor.RegistryCode);
            }

            return dataTable;
        }

        /// <summary>
        /// Builds the dataTable to add the Donor's HLAs to the Database.
        /// </summary>
        /// <remarks>
        /// This is actually the pinch point of DataRefresh!
        /// Largely because we will be adding >1B rows to the DataTable over the course of the Refresh.
        ///
        /// So this method needs to be very aggressively tuned. Note that by default the timing is all
        /// turned off, as it introduces a significant overhead!
        /// When it's surpassing 1B operations, the timing an operation appears to take nearly 20 minutes!
        /// See HlaProcessor to re-enable it.
        /// </remarks>
        protected DataTable BuildPerLocusPGroupDataTable(
            List<DonorInfoForHlaPreProcessing> donors,
            Locus locus,
            LongStopwatchCollection timers = null)
        {
            var createDataTableObjectTimer =
                timers?.TimeInnerOperation(DataRefreshTimingKeys.HlaUpsert_BulkInsertSetup_BuildDataTable_CreateDtObject_TimerKey);
            var dataTable = new DataTable();
            foreach (var columnName in donorPGroupDataTableColumnNames)
            {
                dataTable.Columns.Add(columnName);
            }

            createDataTableObjectTimer?.Dispose();

            dataTable.BeginLoadData();
            //During a 2M donor dataRefresh. This line (outside the loop) is run ~5.6K times.
            foreach (var donor in donors)
            {
                donor.HlaNameIds.GetLocus(locus).EachPosition((position, hlaNameId) =>
                {
                    //During a 2M donor dataRefresh. This line (inside all these loops, but before the filter) is run ~22.2M times.
                    if (hlaNameId == null)
                    {
                        return;
                    }
                    //During a 2M donor dataRefresh. This line (after the filter) is run ~18.1M times.

                    // Data should be written as "TypePosition" so we can guarantee control over the backing int values for this enum
                    var positionId = (int) position.ToTypePosition();

                    using (timers?.TimeInnerOperation(DataRefreshTimingKeys.HlaUpsert_BulkInsertSetup_BuildDataTable_AddRowToDt_TimerKey))
                    {
                        dataTable.Rows.Add(0, donor.DonorId, positionId, hlaNameId);
                    }
                });
            }

            dataTable.EndLoadData();

            return dataTable;
        }

        #region BulkInsertDataTable

        /// <summary>
        /// Performs a bulk insert wrapped in a transaction, over the session's bulk copy for this table where there is
        /// one it can use, and otherwise over a newly built one on a new connection.
        /// If columnNames provided, sets up a map from dataTable to SQL, assuming a 1:1 mapping between dataTable and SQL column names  
        /// </summary>
        private async Task BulkInsertDataTable(
            string tableName,
            DataTable dataTable,
            string[] columnNames,
            int timeout = DefaultBulkInsertTimeoutInSeconds,
            ILongOperationLoggingStopwatch longLoopDbWriteTimer = null)
        {
            using (longLoopDbWriteTimer?.TimeInnerOperation())
            {
                var reusableBulkCopy = GetReusableBulkCopy(tableName);
                if (reusableBulkCopy != null)
                {
                    await reusableBulkCopy.WriteToServerAsync(dataTable);
                    return;
                }

                using (var sqlBulk = BuildSqlBulkCopy(tableName, columnNames, timeout))
                {
                    await sqlBulk.WriteToServerAsync(dataTable);
                }
            }
        }

        /// <summary>
        /// The open session's bulk copy for <paramref name="tableName"/>, or null if this write has to build its own.
        /// </summary>
        /// <remarks>
        /// A reused bulk copy holds its connection open until it is disposed: <see cref="SqlBulkCopy"/> opens an owned
        /// connection before its first write and has no path that closes one again. A connection enlists in the
        /// ambient transaction when it is opened, and never again - so a reused instance takes part in whichever
        /// <see cref="TransactionScope"/> its first write happened to run under, and in none of the scopes after it.
        /// Once that first transaction ends, SqlClient's default "Implicit Unbind" binding detaches the connection
        /// from it silently, or throws "The transaction associated with the current connection has completed but has
        /// not been disposed" if the write lands before the transaction object itself is disposed. So writes made under
        /// an ambient transaction keep building an instance of their own, and behave as they did before sessions existed.
        ///
        /// The silent path is the dangerous one, and it was measured rather than assumed: reusing an instance
        /// regardless of the ambient transaction, and then rolling that transaction back, leaves the rows written - so
        /// a run with DataRefreshDonorUpdatesShouldBeFullyTransactional set would lose transactionality from its
        /// second batch onwards, with nothing in the logs to say so. That is what
        /// InsertBatchOfDonors_WithinOneBulkWriteSession_UnderATransactionThatRollsBack_WritesNothing pins down, and
        /// it is the only test that fails if the check below is dropped.
        /// </remarks>
        private SqlBulkCopy GetReusableBulkCopy(string tableName) =>
            Transaction.Current == null ? activeBulkCopySession?.BulkCopyFor(tableName) : null;

        private SqlBulkCopy BuildSqlBulkCopy(string tableName, string[] columnNames, int timeout = DefaultBulkInsertTimeoutInSeconds)
        {
            // CacheMetadata is what makes reuse worth anything: without it a bulk copy re-describes its destination
            // table on every write, whether or not the instance is the same one as last time. It is safe here because
            // these tables' schemas are fixed for the life of a session - the data refresh drops and recreates their
            // indexes, which the cached column metadata does not describe - and because a session never outlives the
            // connection string it was opened against, so it cannot survive a swap of the transient database.
            const SqlBulkCopyOptions options = SqlBulkCopyOptions.UseInternalTransaction | SqlBulkCopyOptions.CacheMetadata;

            var bulkCopy = new SqlBulkCopy(ConnectionStringProvider.GetConnectionString(), options)
            {
                BatchSize = 10000,
                DestinationTableName = tableName,
                BulkCopyTimeout = timeout
            };

            foreach (var columnName in columnNames)
            {
                // Relies on setting up the data table with column names matching the database columns.
                bulkCopy.ColumnMappings.Add(columnName, columnName);
            }

            return bulkCopy;
        }

        /// <summary>
        /// One <see cref="SqlBulkCopy"/> per table this repository writes, built up front.
        /// </summary>
        /// <remarks>
        /// Eagerly, because a <see cref="SqlBulkCopy"/> costs nothing until its first write - it does not even open its
        /// connection - and because a cache populated up front needs no synchronising against the per-locus writes,
        /// which run concurrently. One instance per table also means no instance is ever shared between those writes:
        /// they go to one table each, and a <see cref="SqlBulkCopy"/> rejects a second concurrent write on the same
        /// instance. Nor do successive batches overlap, since <see cref="UpsertMatchingPGroupsAtSpecifiedLoci"/> awaits
        /// every locus' write before it returns.
        /// </remarks>
        private IReadOnlyDictionary<string, SqlBulkCopy> BuildReusableBulkCopies()
        {
            var bulkCopies = new Dictionary<string, SqlBulkCopy>
            {
                [DonorsTableName] = BuildSqlBulkCopy(DonorsTableName, donorInsertDataTableColumnNames)
            };

            foreach (var locus in LocusSettings.MatchingOnlyLoci)
            {
                var tableName = MatchingHla.TableName(locus);
                bulkCopies[tableName] = BuildSqlBulkCopy(
                    tableName,
                    donorPGroupDataTableColumnNames,
                    MatchingHlaBulkInsertTimeoutInSeconds);
            }

            return bulkCopies;
        }

        /// <summary>
        /// The bulk copies of one <see cref="OpenBulkWriteSession"/>, and their disposal.
        /// </summary>
        private sealed class BulkCopySession : IDisposable
        {
            private readonly DonorUpdateRepositoryBase repository;
            private readonly IReadOnlyDictionary<string, SqlBulkCopy> bulkCopiesByTableName;

            internal BulkCopySession(DonorUpdateRepositoryBase repository)
            {
                this.repository = repository;
                bulkCopiesByTableName = repository.BuildReusableBulkCopies();
            }

            /// <returns>This session's bulk copy for the table, or null if the session does not cover that table.</returns>
            internal SqlBulkCopy BulkCopyFor(string tableName) =>
                bulkCopiesByTableName.TryGetValue(tableName, out var bulkCopy) ? bulkCopy : null;

            public void Dispose()
            {
                foreach (var bulkCopy in bulkCopiesByTableName.Values)
                {
                    // SqlBulkCopy implements IDisposable explicitly, so this cast is the only way to reach Dispose
                    // other than a `using`. Disposing is what closes the connection the bulk copy has held open.
                    ((IDisposable) bulkCopy).Dispose();
                }

                repository.activeBulkCopySession = null;
            }
        }

        #endregion
    }
}