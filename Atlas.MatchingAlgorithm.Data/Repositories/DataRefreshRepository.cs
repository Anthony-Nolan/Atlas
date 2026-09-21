using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MoreLinq;
using DonorBatch = System.Collections.Generic.List<Atlas.MatchingAlgorithm.Data.Models.DonorInfo.DonorInfo>;

namespace Atlas.MatchingAlgorithm.Data.Repositories
{
    /// <summary>
    /// Provides methods indicating which donors have already been imported / processed 
    /// </summary>
    public interface IDataRefreshRepository
    {
        Task<int> GetDonorCount();
        Task<int> GetDonorCountLessThan(int initialDonorId);
        /// <param name="cancellationToken">
        /// Observed before each page's query is issued, and by the query itself. Consumers prefetch pages ahead of
        /// their own processing, so an abandoned enumeration would otherwise go on querying - and holding a connection
        /// to - a database its caller has already stopped caring about.
        /// </param>
        IAsyncEnumerable<DonorBatch> NewOrderedDonorBatchesToImport(
            int batchSize,
            int? lastProcessedDonor,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Unlike <see cref="NewOrderedDonorBatchesToImport"/>, fetches all donors in memory rather than lazily evaluating.
        /// As such, this should only be used to calculate the overlap in a continued refresh, with a small number of batches.
        /// </summary>
        /// <param name="numberOfBatches"></param>
        /// <param name="batchSize"></param>
        /// <param name="lastProcessedDonor"></param>
        /// <returns></returns>
        Task<List<DonorBatch>> GetOrderedDonorBatches(int numberOfBatches, int batchSize, int lastProcessedDonor);
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

        /// <inheritdoc />
        /// <remarks>
        /// <see cref="EnumeratorCancellationAttribute"/> is what makes the token reach this body at all: without it the
        /// token handed to <c>GetAsyncEnumerator</c> is silently dropped, and a caller that stopped enumerating still
        /// paid for one more page query to run to completion before its result was discarded.
        /// </remarks>
        public async IAsyncEnumerable<DonorBatch> NewOrderedDonorBatchesToImport(
            int batchSize,
            int? lastProcessedDonor,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            const string sql = "SELECT top(@batchSize) * FROM Donors WHERE DonorId > @lastProcessedDonor ORDER BY DonorId ASC";
            lastProcessedDonor ??= 0;
            bool hasFoundAllDonors;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
                {
                    var pageQuery = new CommandDefinition(
                        sql,
                        parameters: new {batchSize, lastProcessedDonor},
                        cancellationToken: cancellationToken);
                    var orderedDbDonorBatch = await conn.QueryAsync<Donor>(pageQuery);
                    var donorInfoBatch = orderedDbDonorBatch.Select(donor => donor.ToDonorInfo()).ToList();
                    lastProcessedDonor = donorInfoBatch.LastOrDefault()?.DonorId;
                    hasFoundAllDonors = !donorInfoBatch.Any();
                    yield return donorInfoBatch;
                }
            } while (!hasFoundAllDonors);
        }

        public async Task<List<DonorBatch>> GetOrderedDonorBatches(int numberOfBatches, int batchSize, int lastProcessedDonor)
        {
            const string sql = "SELECT top(@numberToTake) * FROM Donors WHERE DonorId > @lastProcessedDonor ORDER BY DonorId ASC";
            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                // Note 1:
                // Creating a cmdDef object, with Flag Buffered, seems to be the only neat way to use QueryAsync with buffered.
                //
                // Note 2:
                // Using buffered=true and ".ToList()" aren't really necessary here - those are the default settings and behaviours.
                // See here: https://stackoverflow.com/a/13026708/1662268
                // We've specified them, though, so that the code is completely explicit about what's going on.
                var orderedQuery = new CommandDefinition(
                    sql,
                    commandTimeout: 3600,
                    flags: CommandFlags.Buffered,
                    parameters: new {lastProcessedDonor, numberToTake = numberOfBatches * batchSize}
                );
                var orderedDbDonors =
                    await conn.QueryAsync<Donor>(orderedQuery);
                var donorInfos = orderedDbDonors.Select(donor => donor.ToDonorInfo());
                var batches = donorInfos.Batch(batchSize).Select(infosInBatch => infosInBatch.ToList()).ToList();
                return batches;
            }
        }
    }
}