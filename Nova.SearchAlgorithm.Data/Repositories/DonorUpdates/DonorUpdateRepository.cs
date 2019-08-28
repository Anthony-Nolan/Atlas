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
        public DonorUpdateRepository(IPGroupRepository pGroupRepository, IConnectionStringProvider connectionStringProvider) : base(pGroupRepository,
            connectionStringProvider)
        {
        }

        public async Task SetDonorAsUnavailableForSearchBatch(IEnumerable<int> donorIds)
        {
            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                conn.Open();
                var transaction = conn.BeginTransaction();

                var donorIdsAsString = string.Join(",", donorIds);
                await conn.ExecuteAsync(
                    $"UPDATE Donors SET IsAvailableForSearch = 0 WHERE DonorId IN ({donorIdsAsString})",
                    null, transaction, commandTimeout: 600);

                transaction.Commit();
                conn.Close();
            }
        }

        public async Task InsertDonorWithExpandedHla(InputDonorWithExpandedHla donor)
        {
            await InsertBatchOfDonorsWithExpandedHla(new[] { donor });
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
                    ", commandTimeout: 300);

                foreach (var existingDonor in existingDonors.ToList())
                {
                    existingDonor.CopyDataFrom(donors.Single(d => d.DonorId == existingDonor.DonorId));
                    await conn.ExecuteAsync($@"
                        UPDATE Donors 
                        SET DonorType = {((int)existingDonor.DonorType).ToString()},
                        RegistryCode = {((int)existingDonor.RegistryCode).ToString()},
                        IsAvailableForSearch = 1,
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
                        ", commandTimeout: 600);
                }
            }

            await ReplaceMatchingGroupsForExistingDonorBatch(donors);
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
                await conn.ExecuteAsync(deleteSql, null, transaction, commandTimeout: 600);
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
                        dt.Rows.Add(0, donor.DonorId, (int)p, pGroupRepository.FindOrCreatePGroup(pGroup));
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
                sqlBulk.BulkCopyTimeout = 14400;
                await sqlBulk.WriteToServerAsync(dataTable);
            }
        }
    }
}