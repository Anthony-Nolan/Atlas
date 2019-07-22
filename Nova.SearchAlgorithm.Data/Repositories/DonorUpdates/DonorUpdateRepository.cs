using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Nova.SearchAlgorithm.Common.Config;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Common.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Data.Entity;
using Nova.SearchAlgorithm.Data.Helpers;
using Nova.SearchAlgorithm.Data.Services;

// ReSharper disable InconsistentNaming

namespace Nova.SearchAlgorithm.Data.Repositories.DonorUpdates
{
    public class DonorUpdateRepository : DonorUpdateRepositoryBase, IDonorUpdateRepository
    {
        public DonorUpdateRepository(IPGroupRepository pGroupRepository, IConnectionStringProvider connectionStringProvider) : base(pGroupRepository, connectionStringProvider)
        {
        }

        public async Task InsertDonorWithExpandedHla(InputDonorWithExpandedHla donor)
        {
            await InsertBatchOfDonorsWithExpandedHla(new[] {donor});
        }

        public async Task InsertBatchOfDonorsWithExpandedHla(IEnumerable<InputDonorWithExpandedHla> donors)
        {
            donors = donors.ToList();
            await InsertBatchOfDonors(donors.Select(d => d.ToInputDonor()));
            await AddMatchingPGroupsForExistingDonorBatch(donors);
        }

        // Performance may not be sufficient to efficiently import large quantities of donors.
        // Consider re-writing this if we prove to need to process large donor batches
        public async Task UpdateBatchOfDonorsWithExpandedHla(IEnumerable<InputDonorWithExpandedHla> donors)
        {
            donors = donors.ToList();
            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                var existingDonors = await conn.QueryAsync<Donor>($@"
SELECT * FROM Donors 
WHERE DonorId IN ({string.Join(",", donors.Select(d => d.DonorId))})
");
                foreach (var existingDonor in existingDonors.ToList())
                {
                    existingDonor.CopyDataFrom(donors.Single(d => d.DonorId == existingDonor.DonorId));
                    await conn.ExecuteAsync($@"
UPDATE Donors 
SET DonorType = {((int) existingDonor.DonorType).ToString()},
RegistryCode = {((int) existingDonor.RegistryCode).ToString()},
A_1 = '{existingDonor.A_1}',
A_2 = '{existingDonor.A_2}',
B_1 = '{existingDonor.B_1}',
B_2 = '{existingDonor.B_2}',
C_1 = '{existingDonor.C_1}',
C_2 = '{existingDonor.C_2}',
DRB1_1 = '{existingDonor.DRB1_1}',
DRB1_2 = '{existingDonor.DRB1_2}',
DQB1_1 = '{existingDonor.DQB1_1}',
DQB1_2 = '{existingDonor.DQB1_2}',
DPB1_1 = '{existingDonor.DPB1_1}',
DPB1_2 = '{existingDonor.DPB1_2}'
WHERE DonorId = {existingDonor.DonorId}
");
                }
            }

            await ReplaceMatchingGroupsForExistingDonorBatch(donors);
        }

        public async Task DeleteDonorAndItsExpandedHla(int donorId)
        {
            using (var conn = new SqlConnection(connectionStringProvider.GetConnectionString()))
            {
                conn.Open();
                var transaction = conn.BeginTransaction();

                await DeleteMatchingGroupsForExistingDonor(donorId, conn, transaction);
                await conn.ExecuteAsync("DELETE Donors WHERE DonorId = @DonorId", new { DonorId = donorId }, transaction);

                transaction.Commit();
                conn.Close();
            }
        }
        
        private static async Task DeleteMatchingGroupsForExistingDonor(int donorId, IDbConnection connection, IDbTransaction transaction)
        {
            await Task.WhenAll(LocusSettings.MatchingOnlyLoci.Select(l => DeleteMatchingGroupsForExistingDonorAtLocus(l, donorId, connection, transaction)));
        }

        private static async Task DeleteMatchingGroupsForExistingDonorAtLocus(Locus locus, int donorId, IDbConnection connection, IDbTransaction transaction)
        {
            var matchingTableName = MatchingTableNameHelper.MatchingTableName(locus);
            var deleteSql = $@"DELETE FROM {matchingTableName} WHERE DonorId = @DonorId";
            await connection.ExecuteAsync(deleteSql, new { DonorId = donorId }, transaction);
        }

        private async Task ReplaceMatchingGroupsForExistingDonorBatch(IEnumerable<InputDonorWithExpandedHla> inputDonors)
        {
            await Task.WhenAll(LocusSettings.MatchingOnlyLoci.Select(l => ReplaceMatchingGroupsForExistingDonorBatchAtLocus(inputDonors, l)));
        }

        private async Task ReplaceMatchingGroupsForExistingDonorBatchAtLocus(IEnumerable<InputDonorWithExpandedHla> donors, Locus locus)
        {
            donors = donors.ToList();

            var matchingTableName = MatchingTableNameHelper.MatchingTableName(locus);
            var dataTable = CreateDonorDataTableForLocus(donors, locus);

            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                conn.Open();
                var transaction = conn.BeginTransaction();

                var deleteSql = $@"
DELETE FROM {matchingTableName}
WHERE DonorId IN ({string.Join(",", donors.Select(d => d.DonorId))})
";
                await conn.ExecuteAsync(deleteSql, null, transaction);
                await BulkInsertDataTable(conn, transaction, matchingTableName, dataTable);

                transaction.Commit();
                conn.Close();
            }
        }

        private DataTable CreateDonorDataTableForLocus(IEnumerable<InputDonorWithExpandedHla> donors, Locus locus)
        {
            var dt = new DataTable();
            dt.Columns.Add("Id");
            dt.Columns.Add("DonorId");
            dt.Columns.Add("TypePosition");
            dt.Columns.Add("PGroup_Id");

            foreach (var donor in donors)
            {
                donor.MatchingHla.EachPosition((l, p, h) =>
                {
                    if (h == null || l != locus)
                    {
                        return;
                    }

                    foreach (var pGroup in h.PGroups)
                    {
                        dt.Rows.Add(0, donor.DonorId, (int) p, pGroupRepository.FindOrCreatePGroup(pGroup));
                    }
                });
            }

            return dt;
        }

        private static async Task BulkInsertDataTable(SqlConnection conn, SqlTransaction transaction, string tableName, DataTable dataTable)
        {
            using (var sqlBulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, transaction))
            {
                sqlBulk.BatchSize = 10000;
                sqlBulk.DestinationTableName = tableName;
                await sqlBulk.WriteToServerAsync(dataTable);
            }
        }
    }
}