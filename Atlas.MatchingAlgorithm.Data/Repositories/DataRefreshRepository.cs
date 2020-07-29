using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.SystemFunctions;
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
        Task<List<List<DonorInfo>>> NewOrderedDonorBatchesToImport(int batchSize, int? lastProcessedDonor, bool continueExistingImport);
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

        public async Task<List<List<DonorInfo>>> NewOrderedDonorBatchesToImport(int batchSize, int? lastProcessedDonor, bool continueExistingImport)
        {
            var sql = DetermineAppropriateOrderedSqlQuery(continueExistingImport, lastProcessedDonor);

            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
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

        private static string DetermineAppropriateOrderedSqlQuery(bool continueExistingImport, int? lastProcessedDonor)
        {
            const string nonFilteredSqlQuery = "SELECT * FROM Donors ORDER BY DonorId ASC";
            if (!continueExistingImport || lastProcessedDonor == null)
            {
                return nonFilteredSqlQuery;
            }

            return $@"SELECT * FROM Donors WHERE DonorId > {lastProcessedDonor} ORDER BY DonorId ASC";
        }
    }
}