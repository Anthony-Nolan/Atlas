using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Common.Config;
using Atlas.MatchingAlgorithm.Data.Extensions;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils;
using Atlas.Common.Utils.Extensions;

// ReSharper disable InconsistentNaming

namespace Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates
{
    public class DonorUpdateRepository : DonorUpdateRepositoryBase, IDonorUpdateRepository
    {
        private readonly IHlaImportRepository hlaImportRepository;

        public DonorUpdateRepository(
            IHlaImportRepository hlaImportRepository,
            IConnectionStringProvider connectionStringProvider,
            ILogger logger)
            : base(connectionStringProvider, logger)
        {
            this.hlaImportRepository = hlaImportRepository;
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

        public async Task InsertBatchOfDonorsWithExpandedHla(
            IEnumerable<DonorInfoWithExpandedHla> donors,
            bool runAllHlaInsertionsInASingleTransactionScope)
        {
            donors = donors.ToList();

            if (!donors.Any())
            {
                return;
            }

            var hlaLookup = await hlaImportRepository.ImportHla(donors.ToList());
            var processedDonorRecords = donors.Select(d => d.ToDonorInfoForPreProcessing(hla => hlaLookup[hla]));
            using (var transactionScope = new OptionalAsyncTransactionScope(runAllHlaInsertionsInASingleTransactionScope))
            {
                await InsertBatchOfDonors(donors);
                await AddMatchingRelationsForExistingDonorBatch(processedDonorRecords, runAllHlaInsertionsInASingleTransactionScope);
                transactionScope.Complete();
            }
        }

        // This implementation has *not* been aggressively tuned for performance, yet.
        // Feel free to make changes.
        public async Task UpdateDonorBatch(IEnumerable<DonorInfoWithExpandedHla> donorsToUpdate, bool runAllHlaInsertionsInASingleTransactionScope)
        {
            donorsToUpdate = donorsToUpdate.ToList();

            if (!donorsToUpdate.Any())
            {
                return;
            }

            using (var outerTransactionScope = new OptionalAsyncTransactionScope(runAllHlaInsertionsInASingleTransactionScope))
            {
                var donorsWhereHlaHasChanged = new List<(DonorInfoWithExpandedHla, ISet<Locus>)>();

                using (var innerNonHlaTransactionScope = new AsyncTransactionScope())
                await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
                {
                    conn.Open();
                    //TODO: ATLAS-501. Once we've established that this is a complete waste of time ... Delete this extra DB query!
                    var existingDonors = (await conn.QueryAsync<Donor>($@"
                    SELECT * FROM Donors 
                    WHERE DonorId IN ({string.Join(",", donorsToUpdate.Select(d => d.DonorId))})
                    ", commandTimeout: 300)
                        ).ToDictionary(d => d.DonorId); 

                    await SetAvailabilityOfDonorBatch(donorsToUpdate.Select(d => d.DonorId), true, conn);


                    foreach (var donorToUpdate in donorsToUpdate)
                    {
                        var donorExists = existingDonors.TryGetValue(donorToUpdate.DonorId, out var existingDonor);
                        if (!donorExists)
                        {
                            // Shouldn't really happen - it suggests that 2 processes are updating the DB at the same time!
                            // Previous iterations of this code just dropped these cases entirely".
                            // Note that we already ran exactly the same query in DonorService.CreateOrUpdateDonorsWithHla() and split
                            // the donors based on this, so every donor in this set has already had its presence confirmed.
                            // And we don't (I believe?) have any process for deleting donors or changing their IDs, so this really
                            // should be completely impossible.
                            // TODO: ATLAS-501. Investigate whether this ever actually happens and decide what should happen if it does?
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
                            donorsWhereHlaHasChanged.Add((donorToUpdate, changedLoci));
                            await UpdateDonorHla(donorToUpdate, conn);
                        }
                    }

                    // The code for the final step (updating the PGroups) uses its own Connection,
                    // and we mustn't have multiple connections open at once otherwise the
                    // TransactionScope will cry. So close this Connection now.
                    conn.Close();
                    innerNonHlaTransactionScope.Complete();
                }

                var hlaLookup = await hlaImportRepository.ImportHla(donorsWhereHlaHasChanged.Select(d => d.Item1).ToList());
                var donorUpdates = donorsWhereHlaHasChanged.Select(d =>
                {
                    var donorUpdate = d.Item1.ToDonorInfoForPreProcessing(hla => hlaLookup[hla]);
                    return new DonorWithChangedMatchingLoci(donorUpdate, d.Item2);
                }).ToList();

                await UpsertMatchingPGroupsAtSpecifiedLoci(donorUpdates, false, runAllHlaInsertionsInASingleTransactionScope);
                outerTransactionScope.Complete();
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

        private static HashSet<Locus> GetChangedMatchingOnlyLoci(DonorInfo existingDonorInfo, DonorInfo incomingDonorInfo)
        {
            return LocusSettings.MatchingOnlyLoci.Where(locus =>
            {
                var existingNames = existingDonorInfo.HlaNames.GetLocus(locus);
                var incomingNames = incomingDonorInfo.HlaNames.GetLocus(locus);
                return !existingNames.Equals(incomingNames); //LocusInfo implements IEquatable.
            }).ToHashSet();
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
                            DonorType = {(int) donorInfo.DonorType}
                        WHERE DonorId = {donorInfo.DonorId}
                        ", commandTimeout: 600);
        }

        // If we can identify all donors that need their type updating in one go, then this approach is drastically faster.
        // ~0.04 seconds to update 333 records vs. ~1.2 seconds to updated 333 records even in a single connection.
        private static async Task UpdateDonorTypes(List<DonorInfo> donorInfos, IDbConnection connection)
        {
            var updatedDonorTypeMaps = donorInfos.Select(d => $" WHEN {d.DonorId} THEN {(int) d.DonorType} ").StringJoinWithNewline();
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
    }
}