using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Atlas.Common.GeneticData;
using Atlas.Common.Utils;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchingAlgorithm.Common.Config;
using Atlas.MatchingAlgorithm.Data.Helpers;
using Atlas.MatchingAlgorithm.Data.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using EnumStringValues;
using Microsoft.Data.SqlClient;

// ReSharper disable InconsistentNaming

namespace Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates
{
    public abstract class DonorUpdateRepositoryBase : Repository
    {
        protected readonly IPGroupRepository pGroupRepository;

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
        };

        // The order of these matters when setting up the datatable - if re-ordering, also re-order datatable contents
        private readonly string[] donorPGroupDataTableColumnNames =
        {
            "Id",
            "DonorId",
            "TypePosition",
            "PGroup_Id"
        };

        protected DonorUpdateRepositoryBase(
            IPGroupRepository pGroupRepository,
            IConnectionStringProvider connectionStringProvider) : base(connectionStringProvider)
        {
            this.pGroupRepository = pGroupRepository;
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

        public async Task AddMatchingPGroupsForExistingDonorBatch(IEnumerable<DonorInfoWithExpandedHla> donorInfos)
        {
            var donorsWithUpdatesAtEveryLocus = donorInfos
                .Select(info => new DonorWithChangedMatchingLoci(info, LocusSettings.MatchingOnlyLoci))
                .ToList();

            await UpsertMatchingPGroupsAtSpecifiedLoci(donorsWithUpdatesAtEveryLocus, true);
        }

        protected class DonorWithChangedMatchingLoci
        {
            public DonorInfoWithExpandedHla DonorInfo { get; }
            public HashSet<Locus> ChangedMatchingLoci { get; }

            public DonorWithChangedMatchingLoci(DonorInfoWithExpandedHla donorInfo, HashSet<Locus> changedMatchingLoci)
            {
                DonorInfo = donorInfo;
                ChangedMatchingLoci = changedMatchingLoci;
            }
        }

        protected async Task UpsertMatchingPGroupsAtSpecifiedLoci(List<DonorWithChangedMatchingLoci> donors, bool isKnownToBeCreate)
        {
            using (var transactionScope = new AsyncTransactionScope())
            {
                foreach (var locus in LocusSettings.MatchingOnlyLoci)
                {
                    var donorsWhichChangedAtThisLocus = donors
                        .Where(d => d.ChangedMatchingLoci.Contains(locus))
                        .Select(d => d.DonorInfo)
                        .ToList();

                    if (donorsWhichChangedAtThisLocus.Any())
                    {
                        // This is a bit sad.
                        // BulkInserting to unrelated tables, should be an easy win for
                        // "don't await the Tasks separately, use Task.WhenAll() and let them run in parallel".
                        // And that DOES work ... if you can start separate connections for each one.
                        //
                        // But our TransactionScope requires that there only be a single connection at a time.
                        // And for reasons unknown, if you WhenAll() with a shared transaction you lose all
                        // the perf benefits.
                        // 
                        // See here for more detail of the tests done, the perf results achieved and the probable
                        // cause of the problem.
                        // https://stackoverflow.com/questions/62970038/performance-of-multiple-parallel-async-sqlbulkcopy-inserts-against-different
                        await UpsertMatchingPGroupsAtLocus(donorsWhichChangedAtThisLocus, locus, isKnownToBeCreate);
                    }
                }
                transactionScope.Complete();
            }
        }

        private async Task UpsertMatchingPGroupsAtLocus(List<DonorInfoWithExpandedHla> donors, Locus locus, bool isKnownToBeCreate)
        {
            var matchingTableName = MatchingTableNameHelper.MatchingTableName(locus);
            var dataTable = BuildPerLocusPGroupDataTable(donors, locus);

            using (var transactionScope = new AsyncTransactionScope())
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

                await BulkInsertDataTable(
                    matchingTableName,
                    dataTable,
                    donorPGroupDataTableColumnNames,
                    timeout: 14400);

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
                    donor.HlaNames.Drb1.Position2);
            }

            return dataTable;
        }

        protected DataTable BuildPerLocusPGroupDataTable(IEnumerable<DonorInfoWithExpandedHla> donors, Locus locus)
        {
            var dataTable = new DataTable();
            foreach (var columnName in donorPGroupDataTableColumnNames)
            {
                dataTable.Columns.Add(columnName);
            }

            // Data should be written as "TypePosition" so we can guarantee control over the backing int values for this enum
            var dbPositionIdDictionary = EnumExtensions.EnumerateValues<LocusPosition>().ToDictionary(p => p, p => (int) p.ToTypePosition());
            foreach (var donor in donors)
            {
                donor.MatchingHla.GetLocus(locus).EachPosition((position, hlaAtLocusPosition) =>
                {
                    if (hlaAtLocusPosition == null)
                    {
                        return;
                    }

                    var positionId = dbPositionIdDictionary[position];

                    foreach (var pGroup in hlaAtLocusPosition.MatchingPGroups)
                    {
                        dataTable.Rows.Add(
                            0,
                            donor.DonorId,
                            positionId,
                            pGroupRepository.FindOrCreatePGroup(pGroup));
                    }
                });
            }

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