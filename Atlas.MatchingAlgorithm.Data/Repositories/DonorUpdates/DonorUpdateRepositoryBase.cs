using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
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
            var perLocusUpsertTasks = new List<Task>();
            foreach (var locus in LocusSettings.MatchingOnlyLoci)
            {
                var donorsWhichChangedAtThisLocus = donors
                    .Where(d => d.ChangedMatchingLoci.Contains(locus))
                    .Select(d => d.DonorInfo)
                    .ToList();

                if (donorsWhichChangedAtThisLocus.Any())
                {
                    var task = UpsertMatchingPGroupsAtLocus(donorsWhichChangedAtThisLocus, locus, isKnownToBeCreate);
                    perLocusUpsertTasks.Add(task);
                }
            }

            await Task.WhenAll(perLocusUpsertTasks);
        }

        private async Task UpsertMatchingPGroupsAtLocus(List<DonorInfoWithExpandedHla> donors, Locus locus, bool isKnownToBeCreate)
        {
            var matchingTableName = MatchingTableNameHelper.MatchingTableName(locus);
            var dataTable = BuildPerLocusPGroupDataTable(donors, locus);

            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                conn.Open();
                var transaction = conn.BeginTransaction();

                if (!isKnownToBeCreate)
                {
                    var deleteSql = $@"
                    DELETE FROM {matchingTableName}
                    WHERE DonorId IN ({donors.Select(d => d.DonorId.ToString()).StringJoin(",")})
                    ";
                    await conn.ExecuteAsync(deleteSql, null, transaction, commandTimeout: 600);
                }

                await BulkInsertDataTable(
                    matchingTableName,
                    dataTable,
                    donorPGroupDataTableColumnNames,
                    existingTransaction: transaction,
                    timeout: 14400);

                transaction.Commit();
                conn.Close();
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
            SqlConnection existingConnection = null,
            SqlTransaction existingTransaction = null,
            int timeout = 3600)
        {
            var providedExistingConnection = existingConnection != null;
            var providedExistingTransaction = existingTransaction != null;

            var transactionToUse = DetermineConnectionAndTransactionForBulkInsert(existingConnection, existingTransaction);
            //Begin Implied Using Block
                using (var sqlBulk = BuildSqlBulkCopy(tableName, columnNames, transactionToUse, timeout))
                {
                    await sqlBulk.WriteToServerAsync(dataTable);
                }
            //End Implied Using Block
            ResolveTransactionAndConnectionAsAppropriate(transactionToUse, providedExistingConnection, providedExistingTransaction);
        }

        /// <remarks>
        /// This code **MUST** stay in sync with the matching code in <see cref="ResolveTransactionAndConnectionAsAppropriate"/>
        /// </remarks>
        private SqlTransaction DetermineConnectionAndTransactionForBulkInsert(SqlConnection existingConnection, SqlTransaction existingTransaction)
        {
            var providedExistingConnection = existingConnection != null;
            var providedExistingTransaction = existingTransaction != null;

            if (providedExistingConnection &&
                providedExistingTransaction &&
                existingTransaction.Connection != existingConnection)
            {
                throw new InvalidOperationException("You cannot provide both an existing Connection to use AND an existing, unrelated Transaction to use!");
            }

            if (providedExistingTransaction)
            {
                return existingTransaction;
            }

            if (providedExistingConnection)
            {
                return existingConnection.BeginTransaction();
            }

            var connection = new SqlConnection(ConnectionStringProvider.GetConnectionString());
            connection.Open();
            var transactionOnNewConnection = connection.BeginTransaction();

            return transactionOnNewConnection;
        }

        /// <remarks>
        /// This code **MUST** stay in sync with the matching code in <see cref="DetermineConnectionAndTransactionForBulkInsert"/>
        /// </remarks>
        private void ResolveTransactionAndConnectionAsAppropriate(
            SqlTransaction transactionUsed,
            bool existingConnectionWasProvided,
            bool existingTransactionWasProvided)
        {
            if (existingTransactionWasProvided)
            {
                // Do nothing!
                // The calling code gave us these objects and so
                // A) is responsible for managing them.
                // and
                // B) wants to continue using them.
                // We mustn't close them.
                return;
            }

            if (existingConnectionWasProvided)
            {
                //We created our own Transaction, so commit that.
                transactionUsed.Commit();

                // But the calling code gave us this connection and so
                // A) is responsible for managing it.
                // and
                // B) wants to continue using it.
                // We mustn't close it.
                return;
            }

            // We made these objects so we're responsible for closing them.
            var connection = transactionUsed.Connection;
            transactionUsed.Commit();
            connection.Close();
            connection.Dispose();
        }

        private SqlBulkCopy BuildSqlBulkCopy(string tableName, string[] columnNames, SqlTransaction transaction, int timeout = 3600)
        {
            var bulkCopy = new SqlBulkCopy(transaction.Connection, SqlBulkCopyOptions.Default, transaction)
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