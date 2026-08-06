using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchingAlgorithm.Common.Config;
using Atlas.MatchingAlgorithm.Data.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Services;
using Atlas.MatchingAlgorithm.Data.Settings;
using Dapper;
using Microsoft.Data.SqlClient;

// ReSharper disable InconsistentNaming

namespace Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates
{
    public abstract class DonorUpdateRepositoryBase : Repository
    {
        protected readonly IAtlasLogger logger;
        private readonly DataRefreshRepositorySettings settings;

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

        protected DonorUpdateRepositoryBase(
            IConnectionStringProvider connectionStringProvider,
            IAtlasLogger logger,
            DataRefreshRepositorySettings settings) : base(connectionStringProvider)
        {
            this.logger = logger;
            this.settings = settings ?? new DataRefreshRepositorySettings();
        }

        public async Task InsertBatchOfDonors(IEnumerable<DonorInfo> donors)
        {
            var donorInfos = donors.ToList();

            if (!donorInfos.Any())
            {
                return;
            }

            var dataTable = BuildDonorInsertDataTable(donorInfos);

            await BulkInsertDataTable("Donors", dataTable, donorInsertDataTableColumnNames);
        }

        public async Task AddMatchingRelationsForExistingDonorBatch(
            IEnumerable<DonorInfoForHlaPreProcessing> donorInfos,
            bool runAllHlaInsertionsInASingleTransactionScope)
        {
            var donorsWithUpdatesAtEveryLocus = donorInfos
                .Select(info => new DonorWithChangedMatchingLoci(info, LocusSettings.MatchingOnlyLoci))
                .ToList();

            using (logger.TimeOperationAsMetric(
                DataRefreshMetrics.DurationMsMetric,
                DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_UpsertOverall)))
            {
                await UpsertMatchingPGroupsAtSpecifiedLoci(
                    donorsWithUpdatesAtEveryLocus,
                    true,
                    runAllHlaInsertionsInASingleTransactionScope);
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
            bool runAllHlaInsertionsInASingleTransactionScope)
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
                        Task upsertTask;
                        using (logger.TimeOperationAsMetric(
                            DataRefreshMetrics.DurationMsMetric,
                            DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_BulkInsertSetup, locus.ToString())))
                        {
                            upsertTask = UpsertMatchingPGroupsAtLocus(
                                donorsWhichChangedAtThisLocus,
                                locus,
                                isKnownToBeCreate);
                            perLocusUpsertTasks.Add(upsertTask);
                        }

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
                            using (logger.TimeOperationAsMetric(
                                DataRefreshMetrics.DurationMsMetric,
                                DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_BlockingWaitOnDbInsert, locus.ToString())))
                            {
                                await upsertTask;
                            }
                        }
                    }
                }

                // Note that we may have already awaited these tasks to support TransactionScope.
                // In that case this `WhenAll` is a no-op. But it makes the difference
                // between the two cases easy to define.
                using (logger.TimeOperationAsMetric(
                    DataRefreshMetrics.DurationMsMetric,
                    DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_BlockingWaitOnDbInsert)))
                {
                    await Task.WhenAll(perLocusUpsertTasks);
                }

                transactionScope.Complete();
            }
        }

        private async Task UpsertMatchingPGroupsAtLocus(
            List<DonorInfoForHlaPreProcessing> donors,
            Locus locus,
            bool isKnownToBeCreate)
        {
            var matchingTableName = MatchingHla.TableName(locus);

            DataTable dataTable;
            using (logger.TimeOperationAsMetric(
                DataRefreshMetrics.DurationMsMetric,
                DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_BuildDataTable, locus.ToString())))
            {
                dataTable = BuildPerLocusPGroupDataTable(donors, locus);
            }

            // Read BEFORE the scope is opened, so it describes what this write inherited rather than what it created.
            // See Operation_AmbientTransactionOnEntry: on the refresh path a 1 here means a sibling locus' scope has
            // leaked into our execution context, so `Required` silently joins it instead of starting a transaction of
            // our own - which would put all five bulk-copy connections in one transaction and promote it.
            logger.SendMetric(
                DataRefreshMetrics.CountMetric,
                Transaction.Current == null ? 0 : 1,
                DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_AmbientTransactionOnEntry, locus.ToString()));

            // Timed on its own because it is one of the three candidate homes for the time that BulkInsertSetup
            // measures as a block but does not attribute - the others being BuildSqlBulkCopy and BulkCopySyncPrologue.
            // Constructing the scope is what makes a transaction ambient, so it is not self-evidently free.
            AsyncTransactionScope transactionScope;
            using (logger.TimeOperationAsMetric(
                DataRefreshMetrics.DurationMsMetric,
                DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_TransactionScopeSetup, locus.ToString())))
            {
                transactionScope = new AsyncTransactionScope();
            }

            using (transactionScope)
            {
                using (logger.TimeOperationAsMetric(
                    DataRefreshMetrics.DurationMsMetric,
                    DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_DeleteExistingRecords, locus.ToString())))
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

                // Counting the rows as well as timing the write is what turns "DbBulkInsert took N ms" into
                // "ms per million rows" - the only form in which this number is comparable between loci, between
                // runs, and between DEV and LIVE. It also finally pins the MatchingHlaAt* row counts, open since
                // Phase A. dataTable.Rows.Count is already materialised; this costs nothing.
                logger.SendMetric(
                    DataRefreshMetrics.CountMetric,
                    dataTable.Rows.Count,
                    DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_MatchingHlaRowsWritten, locus.ToString()));

                using (logger.TimeOperationAsMetric(
                    DataRefreshMetrics.DurationMsMetric,
                    DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_DbBulkInsert, locus.ToString())))
                {
                    await BulkInsertDataTable(
                        matchingTableName,
                        dataTable,
                        donorPGroupDataTableColumnNames,
                        timeout: 14400,
                        locus: locus.ToString());
                }

                // Read AFTER the write, because a transaction promotes when a SECOND connection enlists in it -
                // reading this any earlier would report 0 whether or not it went on to promote.
                var distributedId = Transaction.Current?.TransactionInformation.DistributedIdentifier ?? Guid.Empty;
                logger.SendMetric(
                    DataRefreshMetrics.CountMetric,
                    distributedId == Guid.Empty ? 0 : 1,
                    DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_DistributedTransactionPromotions, locus.ToString()));

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
        /// So this method needs to be very aggressively tuned.
        ///
        /// The whole-method duration is timed by the caller as the <c>BuildDataTable</c> operation of the
        /// <see cref="DataRefreshMetrics.DurationMsMetric"/> metric. Per-row timing is deliberately NOT done here:
        /// at &gt;1B rows even a near-free timing call adds up to tens of minutes, and a pre-aggregated metric per
        /// batch already gives the distribution we need without instrumenting the innermost loop.
        /// </remarks>
        protected DataTable BuildPerLocusPGroupDataTable(
            List<DonorInfoForHlaPreProcessing> donors,
            Locus locus)
        {
            var dataTable = new DataTable();
            foreach (var columnName in donorPGroupDataTableColumnNames)
            {
                dataTable.Columns.Add(columnName);
            }

            dataTable.BeginLoadData();
            foreach (var donor in donors)
            {
                donor.HlaNameIds.GetLocus(locus).EachPosition((position, hlaNameId) =>
                {
                    if (hlaNameId == null)
                    {
                        return;
                    }

                    // Data should be written as "TypePosition" so we can guarantee control over the backing int values for this enum
                    var positionId = (int) position.ToTypePosition();

                    dataTable.Rows.Add(0, donor.DonorId, positionId, hlaNameId);
                });
            }

            dataTable.EndLoadData();

            return dataTable;
        }

        #region BulkInsertDataTable

        /// <summary>
        /// Opens a new connection and performs a bulk insert wrapped in a transaction.
        /// If columnNames provided, sets up a map from dataTable to SQL, assuming a 1:1 mapping between dataTable and SQL column names  
        /// </summary>
        /// <param name="locus">
        /// Locus dimension for the two timings below. Defaults to <see cref="DataRefreshMetrics.Locus_All"/>, which is
        /// what the stage-40 Donors insert reports under - so this method decomposes that write for free as well.
        /// </param>
        private async Task BulkInsertDataTable(
            string tableName,
            DataTable dataTable,
            string[] columnNames,
            int timeout = 3600,
            string locus = DataRefreshMetrics.Locus_All)
        {
            SqlBulkCopy sqlBulk;
            using (logger.TimeOperationAsMetric(
                DataRefreshMetrics.DurationMsMetric,
                DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_BuildSqlBulkCopy, locus)))
            {
                sqlBulk = BuildSqlBulkCopy(tableName, columnNames, timeout);
            }

            using (sqlBulk)
            {
                // Deliberately NOT `using (timer) { await sqlBulk.WriteToServerAsync(dataTable); }`. That would close
                // the timer after the await and simply re-measure DbBulkInsert, which the caller already has.
                //
                // What is missing is the part of WriteToServerAsync that runs SYNCHRONOUSLY on the calling thread
                // before its first true await - connection open, enlistment in the ambient transaction, and the
                // bulk-load metadata exchange. That part is exactly what the caller's BulkInsertSetup span captures
                // and cannot attribute, because BulkInsertSetup times the task-CREATING call and so ends at this
                // method's first true await. Capturing the task and closing the timer before awaiting it isolates it.
                //
                // `var t = X(); await t;` is precisely what `await X()` compiles to, so this is a measurement, not a
                // behaviour change.
                Task writeToServer;
                using (logger.TimeOperationAsMetric(
                    DataRefreshMetrics.DurationMsMetric,
                    DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_BulkCopySyncPrologue, locus)))
                {
                    writeToServer = sqlBulk.WriteToServerAsync(dataTable);
                }

                await writeToServer;
            }
        }

        private SqlBulkCopy BuildSqlBulkCopy(string tableName, string[] columnNames, int timeout = 3600)
        {
            var bulkCopy = new SqlBulkCopy(ConnectionStringProvider.GetConnectionString(), SqlBulkCopyOptions.UseInternalTransaction)
            {
                BatchSize = settings.SqlBulkCopyBatchSize,
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

        #endregion
    }
}