using Dapper;
using Nova.SearchAlgorithm.Common.Config;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Common.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Data.Entity;
using Nova.SearchAlgorithm.Data.Extensions;
using Nova.SearchAlgorithm.Data.Helpers;
using Nova.SearchAlgorithm.Data.Services;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable InconsistentNaming

namespace Nova.SearchAlgorithm.Data.Repositories.DonorUpdates
{
    public class DonorUpdateRepository : DonorUpdateRepositoryBase, IDonorUpdateRepository
    {
        public DonorUpdateRepository(IPGroupRepository pGroupRepository, IConnectionStringProvider connectionStringProvider) : base(pGroupRepository,
            connectionStringProvider)
        {
        }

        public async Task SetDonorBatchAsUnavailableForSearch(IEnumerable<int> donorIds)
        {
            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                conn.Open();
                await SetAvailabilityOfDonorBatch(donorIds, false, conn);
                conn.Close();
            }
        }

        public async Task InsertBatchOfDonorsWithExpandedHla(IEnumerable<InputDonorWithExpandedHla> donors)
        {
            donors = donors.ToList();

            if (!donors.Any())
            {
                return;
            }

            await InsertBatchOfDonors(donors.Select(d => d.ToInputDonor()));
            await AddMatchingPGroupsForExistingDonorBatch(donors);
        }

        // Performance may not be sufficient to efficiently import large quantities of donors.
        // Consider re-writing this if we prove to need to process large donor batches
        public async Task UpdateDonorBatch(IEnumerable<InputDonorWithExpandedHla> donorsToUpdate)
        {
            donorsToUpdate = donorsToUpdate.ToList();

            if (!donorsToUpdate.Any())
            {
                return;
            }

            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                conn.Open();

                var existingDonors = (await conn.QueryAsync<Donor>($@"
                    SELECT * FROM Donors 
                    WHERE DonorId IN ({string.Join(",", donorsToUpdate.Select(d => d.DonorId))})
                    ", commandTimeout: 300)
                    ).ToList();

                await SetAvailabilityOfDonorBatch(existingDonors.Select(d => d.DonorId), true, conn);

                var donorsWhereHlaHasChanged = new List<InputDonorWithExpandedHla>();

                foreach (var existingDonor in existingDonors)
                {
                    var existingDonorResult = existingDonor.ToDonorResult();
                    var donorToUpdate = donorsToUpdate.Single(d => d.DonorId == existingDonorResult.DonorId);

                    if (DonorInfoHasChanged(existingDonor, donorToUpdate))
                    {
                        await UpdateDonorInfo(donorToUpdate, conn);
                    }

                    if (DonorHlaHasChanged(existingDonorResult, donorToUpdate))
                    {
                        donorsWhereHlaHasChanged.Add(donorToUpdate);
                        await UpdateDonorHla(donorToUpdate, conn);
                    }
                }

                await ReplaceMatchingGroupsForExistingDonorBatch(donorsWhereHlaHasChanged);

                conn.Close();
            }
        }

        private static bool DonorInfoHasChanged(Donor existingDonor, InputDonorWithExpandedHla inputDonor)
        {
            return existingDonor.RegistryCode != inputDonor.RegistryCode ||
                   existingDonor.DonorType != inputDonor.DonorType;
        }

        private static bool DonorHlaHasChanged(DonorResult existingDonorResult, InputDonorWithExpandedHla inputDonor)
        {
            return !existingDonorResult.HlaNames.Equals(inputDonor.ToInputDonor().HlaNames);
        }

        private static async Task SetAvailabilityOfDonorBatch(IEnumerable<int> donorIds, bool isAvailableForSearch, SqlConnection conn)
        {
            var availabilityAsString = isAvailableForSearch ? "1" : "0";

            var transaction = conn.BeginTransaction();

            var donorIdsAsString = string.Join(",", donorIds);
            await conn.ExecuteAsync(
                $"UPDATE Donors SET IsAvailableForSearch = {availabilityAsString} WHERE DonorId IN ({donorIdsAsString})",
                null, transaction, commandTimeout: 600);

            transaction.Commit();
        }

        /// <summary>
        /// Updates donor fields not related to availability or HLA.
        /// </summary>
        private static async Task UpdateDonorInfo(InputDonorWithExpandedHla donor, IDbConnection connection)
        {
            await connection.ExecuteAsync($@"
                        UPDATE Donors 
                        SET 
                            DonorType = {(int) donor.DonorType},
                            RegistryCode = {(int) donor.RegistryCode}
                        WHERE DonorId = {donor.DonorId}
                        ", commandTimeout: 600);
        }

        private static async Task UpdateDonorHla(InputDonorWithExpandedHla inputDonorWithExpandedHla, IDbConnection connection)
        {
            var donor = inputDonorWithExpandedHla.ToDonor();

            const string sql = @"
                        UPDATE Donors
                        SET
                            A_1 = @A_1,
                            A_2 = @A_2,
                            B_1 = @B_1,
                            B_2 = @B_2,
                            C_1 = @C_1,
                            C_2 = @C_2,
                            DRB1_1 = @DRB1_1,
                            DRB1_2 = @DRB1_2,
                            DQB1_1 = @DQB1_1,
                            DQB1_2 = @DQB1_2,
                            DPB1_1 = @DPB1_1,
                            DPB1_2 = @DPB1_2
                        WHERE DonorId = @DonorId
                        ";
            await connection.ExecuteAsync(sql, donor, commandTimeout: 600);
        }

        private async Task ReplaceMatchingGroupsForExistingDonorBatch(IEnumerable<InputDonorWithExpandedHla> inputDonors)
        {
            var donors = inputDonors.ToList();

            if (!donors.Any())
            {
                return;
            }

            await Task.WhenAll(LocusSettings.MatchingOnlyLoci.Select(l => ReplaceMatchingGroupsForExistingDonorBatchAtLocus(donors, l)));
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
                sqlBulk.BulkCopyTimeout = 14400;
                await sqlBulk.WriteToServerAsync(dataTable);
            }
        }
    }
}