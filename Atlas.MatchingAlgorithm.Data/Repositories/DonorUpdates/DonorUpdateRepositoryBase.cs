using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Common.Config;
using Atlas.MatchingAlgorithm.Data.Helpers;
using Atlas.MatchingAlgorithm.Data.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Services;
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
            donorInfos = donorInfos.ToList();
            await Task.WhenAll(LocusSettings.MatchingOnlyLoci.Select(l => AddMatchingGroupsForExistingDonorBatchAtLocus(donorInfos, l)));
        }

        private async Task AddMatchingGroupsForExistingDonorBatchAtLocus(IEnumerable<DonorInfoWithExpandedHla> donors, Locus locus)
        {
            var matchingTableName = MatchingTableNameHelper.MatchingTableName(locus);
            var dataTable = BuildPerLocusPGroupDataTable(donors, locus);
            await BulkInsertDataTable(matchingTableName, dataTable, donorPGroupDataTableColumnNames);
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

        /// <summary>
        /// Opens a new connection and performs a bulk insert wrapped in a transaction.
        /// If columnNames provided, sets up a map from dataTable to SQL, assuming a 1:1 mapping between dataTable and SQL column names  
        /// </summary>
        private async Task BulkInsertDataTable(
            string tableName,
            DataTable dataTable,
            IEnumerable<string> columnNames = null)
        {
            columnNames ??= new List<string>();
            await using (var connection = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                connection.Open();
                var transaction = connection.BeginTransaction();
                using (var sqlBulk = BuildSqlBulkCopy(tableName, connection, transaction))
                {
                    foreach (var columnName in columnNames)
                    {
                        // Relies on setting up the data table with column names matching the database columns.
                        sqlBulk.ColumnMappings.Add(columnName, columnName);
                    }

                    await sqlBulk.WriteToServerAsync(dataTable);
                }

                transaction.Commit();
                connection.Close();
            }
        }

        protected SqlBulkCopy BuildSqlBulkCopy(string tableName, SqlConnection connection, SqlTransaction transaction, int timeout = 3600)
        {
            return new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)
            {
                BatchSize = 10000,
                DestinationTableName = tableName,
                BulkCopyTimeout = timeout
            };
        }
    }
}