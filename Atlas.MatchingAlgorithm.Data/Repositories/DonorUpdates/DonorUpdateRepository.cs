using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Common.Config;
using Atlas.MatchingAlgorithm.Common.Repositories;
using Atlas.MatchingAlgorithm.Data.Extensions;
using Atlas.MatchingAlgorithm.Data.Helpers;
using Atlas.MatchingAlgorithm.Data.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;

// ReSharper disable InconsistentNaming

namespace Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates
{
    public class DonorUpdateRepository : DonorUpdateRepositoryBase, IDonorUpdateRepository
    {
        private class DonorWithChangedMatchingLoci
        {
            public DonorInfoWithExpandedHla DonorInfo { get; }
            public IEnumerable<Locus> ChangedMatchingLoci { get; }

            public DonorWithChangedMatchingLoci(DonorInfoWithExpandedHla donorInfo, IEnumerable<Locus> changedMatchingLoci)
            {
                DonorInfo = donorInfo;
                ChangedMatchingLoci = changedMatchingLoci;
            }
        }

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

        public async Task InsertBatchOfDonorsWithExpandedHla(IEnumerable<DonorInfoWithExpandedHla> donors)
        {
            donors = donors.ToList();

            if (!donors.Any())
            {
                return;
            }

            await InsertBatchOfDonors(donors);
            await AddMatchingPGroupsForExistingDonorBatch(donors);
        }

        // This implementation has *not* been aggressively tuned for performance, yet.
        // Feel free to make changes.
        public async Task UpdateDonorBatch(IEnumerable<DonorInfoWithExpandedHla> donorsToUpdate)
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
                    ).ToDictionary(d => d.DonorId);

                await SetAvailabilityOfDonorBatch(donorsToUpdate.Select(d => d.DonorId), true, conn);

                var donorsWhereHlaHasChanged = new List<DonorWithChangedMatchingLoci>();

                foreach (var donorToUpdate in donorsToUpdate)
                {
                    var donorExists = existingDonors.TryGetValue(donorToUpdate.DonorId, out var existingDonor);
                    if (!donorExists)
                    {
                        // Shouldn't really happen - it suggests that 2 processes are updating the DB at the same time!
                        // Existing code just drops these cases entirely.
                        continue;
                    }

                    if (DonorTypeHasChanged(existingDonor, donorToUpdate))
                    {
                        await UpdateDonorType(donorToUpdate, conn);
                    }

                    var existingDonorResult = existingDonor.ToDonorInfo();
                    if (DonorHlaHasChanged(existingDonorResult, donorToUpdate))
                    {
                        var changedLoci = GetChangedMatchingOnlyLoci(existingDonorResult, donorToUpdate);
                        donorsWhereHlaHasChanged.Add(new DonorWithChangedMatchingLoci(donorToUpdate, changedLoci));
                        await UpdateDonorHla(donorToUpdate, conn);
                    }
                }

                await ReplaceMatchingGroupsForExistingDonorBatch(donorsWhereHlaHasChanged);

                conn.Close();
            }
        }

        private static bool DonorTypeHasChanged(Donor existingDonor, DonorInfo donorInfo)
        {
            return existingDonor.DonorType != donorInfo.DonorType;
        }

        private static bool DonorHlaHasChanged(DonorInfo existingDonorInfo, DonorInfo incomingDonorInfo)
        {
            return !existingDonorInfo.HlaNames.Equals(incomingDonorInfo.HlaNames); //PhenotypeInfo implements IEquatable.
        }

        private static IEnumerable<Locus> GetChangedMatchingOnlyLoci(DonorInfo existingDonorInfo, DonorInfo incomingDonorInfo)
        {
            return LocusSettings.MatchingOnlyLoci.Where(locus =>
            {
                var existingNames = existingDonorInfo.HlaNames.GetLocus(locus);
                var incomingNames = incomingDonorInfo.HlaNames.GetLocus(locus);
                return !existingNames.Equals(incomingNames); //LocusInfo implements IEquatable.
            });
        }

        /// <summary>
        /// Sets all listed donors to be available or not, using the provided, open connection.
        /// </summary>
        /// <param name="donorIds">Donors to update.</param>
        /// <param name="isAvailableForSearch">Value to update availability to.</param>
        /// <param name="conn">An **open** SqlConnection which the caller is responsible for closing</param>
        private static async Task SetAvailabilityOfDonorBatch(IEnumerable<int> donorIds, bool isAvailableForSearch, SqlConnection conn)
        {
            var availabilityAsString = isAvailableForSearch ? "1" : "0";

            var donorIdsAsString = string.Join(",", donorIds);
            await conn.ExecuteAsync(
                $"UPDATE Donors SET IsAvailableForSearch = {availabilityAsString} WHERE DonorId IN ({donorIdsAsString})",
                commandTimeout: 600);
        }

        private static async Task UpdateDonorType(DonorInfo donorInfo, IDbConnection connection)
        {
            await connection.ExecuteAsync($@"
                        UPDATE Donors 
                        SET 
                            DonorType = {(int)donorInfo.DonorType}
                        WHERE DonorId = {donorInfo.DonorId}
                        ", commandTimeout: 600);
        }

        // If we can identify all donors that need their type updating in one go, then this approach is drastically faster.
        // ~0.04 seconds to update 333 records vs. ~1.2 seconds to updated 333 records even in a single connection.
        private static async Task UpdateDonorTypes(List<DonorInfo> donorInfos, IDbConnection connection)
        {
            var updatedDonorTypeMaps = donorInfos.Select(d => $" WHEN {d.DonorId} THEN {(int)d.DonorType} ").StringJoinWithNewline();
            var allUpdatedDonorIds = donorInfos.Select(d => d.DonorId.ToString()).StringJoin(", ");

            await connection.ExecuteAsync($@"
                    UPDATE Donors
                    SET DonorType = (
                        CASE(DonorId)
                            {updatedDonorTypeMaps}
                            ELSE DonorType
                        END)
                        WHERE DonorId IN (
                            {allUpdatedDonorIds}
                        )
                        ", commandTimeout: 600);
        }


        private static async Task UpdateDonorHla(DonorInfo donorInfo, IDbConnection connection)
        {
            var donor = donorInfo.ToDonor();

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

        private async Task ReplaceMatchingGroupsForExistingDonorBatch(IEnumerable<DonorWithChangedMatchingLoci> donorsWithChangedMatchingLoci)
        {
            var donors = donorsWithChangedMatchingLoci.ToList();

            if (!donors.Any())
            {
                return;
            }

            await Task.WhenAll(LocusSettings.MatchingOnlyLoci.Select(async locus =>
                {
                    var donorsToUpdate = donors
                        .Where(d => d.ChangedMatchingLoci.Contains(locus))
                        .Select(d => d.DonorInfo)
                        .ToList();

                    if (donorsToUpdate.Any())
                    {
                        await ReplaceMatchingGroupsForExistingDonorBatchAtLocus(donorsToUpdate, locus);
                    }
                }
            ));
        }

        private async Task ReplaceMatchingGroupsForExistingDonorBatchAtLocus(IEnumerable<DonorInfoWithExpandedHla> donors, Locus locus)
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

        private DataTable CreateDonorDataTableForLocus(IEnumerable<DonorInfoWithExpandedHla> donors, Locus locus)
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

                    foreach (var pGroup in h.MatchingPGroups)
                    {
                        // Data should be written as "TypePosition" so we can guarantee control over the backing int values for this enum
                        dt.Rows.Add(0, donor.DonorId, (int)p.ToTypePosition(), pGroupRepository.FindOrCreatePGroup(pGroup));
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