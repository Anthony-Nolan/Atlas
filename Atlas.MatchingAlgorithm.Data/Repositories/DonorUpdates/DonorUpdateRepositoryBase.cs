using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
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

            using (var transactionScope = new AsyncTransactionScope())
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

                using (logger.TimeOperationAsMetric(
                    DataRefreshMetrics.DurationMsMetric,
                    DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_DbBulkInsert, locus.ToString())))
                {
                    await BulkInsertDataTable(
                        matchingTableName,
                        dataTable,
                        donorPGroupDataTableColumnNames,
                        timeout: 14400);
                }

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
        private async Task BulkInsertDataTable(
            string tableName,
            DataTable dataTable,
            string[] columnNames,
            int timeout = 3600)
        {
            using (var sqlBulk = BuildSqlBulkCopy(tableName, columnNames, timeout))
            {
                await sqlBulk.WriteToServerAsync(dataTable);
            }
        }

        private SqlBulkCopy BuildSqlBulkCopy(string tableName, string[] columnNames, int timeout = 3600)
        {
            var bulkCopy = new SqlBulkCopy(ConnectionStringProvider.GetConnectionString(), SqlBulkCopyOptions.UseInternalTransaction)
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

        #endregion
    }
}