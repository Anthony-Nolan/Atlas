using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using MoreLinq;

namespace Atlas.MatchingAlgorithm.Data.Repositories
{
    /// <summary>
    /// Provides methods indicating which donors have already been imported / processed 
    /// </summary>
    public interface IDataRefreshRepository
    {
        Task<int> GetDonorCount();
        Task<int> GetDonorCountLessThan(int initialDonorId);
        Task<List<List<DonorInfo>>> NewOrderedDonorBatchesToImport(int batchSize, bool continueExistingImport);
    }

    public class DataRefreshRepository : Repository, IDataRefreshRepository
    {
        public const int NumberOfBatchesOverlapOnRestart = 2;

        public DataRefreshRepository(IConnectionStringProvider connectionStringProvider) : base(connectionStringProvider)
        {
        }

        public async Task<int> GetDonorCount()
        {
            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                return await conn.QueryFirstAsync<int>(@"SELECT COUNT(*) FROM DONORS");
            }
        }

        public async Task<int> GetDonorCountLessThan(int initialDonorId)
        {
            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                return await conn.QueryFirstAsync<int>($@"SELECT COUNT(*) FROM DONORS WHERE DonorId < {initialDonorId}");
            }
        }

        public async Task<List<List<DonorInfo>>> NewOrderedDonorBatchesToImport(int batchSize, bool continueExistingImport)
        {
            var sql = await DetermineAppropriateOrderedSqlQuery(continueExistingImport, batchSize);

            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                // Note 1:
                // Creating a cmdDef object, with Flag Buffered, seems to be the only neat way to use QueryAsync with buffered.
                //
                // Note 2:
                // Using buffered=true and ".ToList()" aren't really necessary here - those are the default settings and behaviours.
                // See here: https://stackoverflow.com/a/13026708/1662268
                // We've specified them, though, so that the code is completely explicit about what's going on.
                // Note that we *might* want to stream this data! In which case buffered should be set to false, and the .ToList() removed.
                var orderedQuery = new CommandDefinition(sql, commandTimeout: 3600, flags: CommandFlags.Buffered);
                var orderedDbDonors = await conn.QueryAsync<Donor>(orderedQuery);
                var donorInfos = orderedDbDonors.Select(donor => donor.ToDonorInfo());
                var batches = donorInfos.Batch(batchSize).Select(infosInBatch => infosInBatch.ToList()).ToList();
                return batches;
            }
        }

        private async Task<string> DetermineAppropriateOrderedSqlQuery(bool continueExistingImport, int batchSize)
        {
            var nonFilteredSqlQuery = "SELECT * FROM Donors ORDER BY DonorId ASC";
            if (!continueExistingImport)
            {
                return nonFilteredSqlQuery;
            }

            var donorToContinueFrom = await DetermineFirstDonorToImportInThisPass(batchSize);

            if (donorToContinueFrom == null)
            {
                return nonFilteredSqlQuery;
            }

            return $@"SELECT * FROM Donors WHERE DonorId > {donorToContinueFrom} ORDER BY DonorId ASC";
        }

        private async Task<int?> DetermineFirstDonorToImportInThisPass(int batchSize)
        {
            var highestDonorId = await DetermineHighestDonorIdForWhichHlaHasBeenProcessed();

            // Having identified this highest Donor that MIGHT have been fully imported, we locate a donor
            // several batches backwards in time. Since we don't start a new batch until the previous one
            // was successfully completed, this means that even if the last batch was left in a mess, the
            // Donor that we've just identified is guaranteed to be clean.
            // So we'll start again from there, cleaning out anything since then.
            var donorToContinueFrom = await DetermineDonorIdNDonorsBeforeThis(highestDonorId, batchSize * NumberOfBatchesOverlapOnRestart);

            // As noted above, this donor should be completely clean. So let's double check that.
            if (donorToContinueFrom != null)
            {
                await VerifyThatDonorExistsInAllRequiredTables(donorToContinueFrom.Value);
            }

            return donorToContinueFrom;
        }

        /// <summary>
        /// Make a reasonable guess at the last DonorId to have been processed.
        /// We can't rely on any single table, in case the last batch failed part-way through
        /// and not all tables were written fully written.
        /// Instead, we identify the highest Id in each of the tables that we expect to be edited,
        /// and take the min of those Ids.
        /// </summary>
        private async Task<int> DetermineHighestDonorIdForWhichHlaHasBeenProcessed()
        {
            await using (var connection = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                var maxDonorPresentInAllRequiredTables = await connection.QuerySingleOrDefaultAsync<int>(@"
SELECT MIN(MaxId) FROM (
    SELECT MAX(DonorId) AS MaxId FROM Donors
    UNION ALL
    SELECT MAX(DonorId) AS MaxId FROM MatchingHlaAtDrb1
    UNION ALL
    SELECT MAX(DonorId) AS MaxId FROM MatchingHlaAtB
    UNION ALL
    SELECT MAX(DonorId) AS MaxId FROM MatchingHlaAtA
) AS temp
");
                return maxDonorPresentInAllRequiredTables;
            }
        }

        private async Task VerifyThatDonorExistsInAllRequiredTables(int donorIdToVerify)
        {
            await using (var connection = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                var idPresentInAllTables = await connection.QuerySingleAsync<int>($@"
IF (
        EXISTS(SELECT * FROM Donors WHERE DonorId = {donorIdToVerify})
        AND
        EXISTS(SELECT * FROM MatchingHlaAtDrb1 WHERE DonorId = {donorIdToVerify})
        AND
        EXISTS(SELECT * FROM MatchingHlaAtB WHERE DonorId = {donorIdToVerify})
        AND
        EXISTS(SELECT * FROM MatchingHlaAtA WHERE DonorId = {donorIdToVerify})
)
BEGIN
    SELECT 1
END
ELSE
BEGIN
    SELECT 0
END
");
                if (idPresentInAllTables == 0)
                {
                    throw new InvalidOperationException("When attempting to continue an import, inconsistent DonorId existence was found. DonorId in question: " + donorIdToVerify);
                }
            }
        }

        private async Task<int?> DetermineDonorIdNDonorsBeforeThis(int highestDonorId, int n)
        {
            // We can't guarantee that DonorIds will be sequential. Indeed gaps have been actively observed!
            // So we can't just subtract N from the Id - we have to actually look it up.
            // This query still only takes ~4 seconds on 2M records, so we should be fine.
            var sql = $@"
-- The LAG has to be done in a sub-query!
-- If you combine it directly with the WHERE clause, the WHERE applies first
-- so the LAG always returns null, because it's only looking at 1 row.
WITH DonorIdsWithLaggedDonorIds AS (
    SELECT
        DonorId,
        LAG(DonorId, {n}) OVER (ORDER BY DonorId ASC) AS LaggedDonorId
        FROM Donors
)
SELECT LaggedDonorId
    FROM DonorIdsWithLaggedDonorIds
    WHERE DonorId = {highestDonorId}";

            await using (var connection = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                // Note that this might return null, if the import failed very early on.
                // In that case return null and expect the calling code to handle it.
                return await connection.QuerySingleAsync<int?>(sql);
            }
        }
    }
}