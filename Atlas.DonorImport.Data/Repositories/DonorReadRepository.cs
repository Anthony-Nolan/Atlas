using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Data.Models;
using Dapper;

namespace Atlas.DonorImport.Data.Repositories
{
    public interface IDonorReadRepository
    {
        public IEnumerable<Donor> StreamAllDonors();
        public Task<IReadOnlyDictionary<string, Donor>> GetDonorsByExternalDonorCodes(ICollection<string> externalDonorCodes);
        public Task<IReadOnlyDictionary<int, Donor>> GetDonorsByIds(ICollection<int> donorIds);
        public Task<IReadOnlyDictionary<string, int>> GetDonorIdsByExternalDonorCodes(ICollection<string> externalDonorCodes);
        public Task<IReadOnlyDictionary<string, int>> GetDonorIdsUpdatedSince(DateTimeOffset cutoffDate);
    }

    public class DonorReadRepository : DonorRepositoryBase, IDonorReadRepository
    {
        private const int DonorReadBatchSize = 1500;

        /// <inheritdoc />
        public DonorReadRepository(string connectionString) : base(connectionString)
        {
        }

        public IEnumerable<Donor> StreamAllDonors()
        {
            var sql = $"SELECT {Donor.ColumnNamesForRead.StringJoin(",")} FROM {Donor.QualifiedTableName}";
            using (var connection = NewConnection())
            {
                // With "buffered: false" this should avoid loading all donors into memory before returning.
                // This is necessary because we start to have issues running out of memory on a dataset of around 2M donors.
                // Pro: Smaller memory footprint.
                // Con: Longer open connection, could cause timeouts if not by not fully consumed in time.
                var donorStream = connection.Query<Donor>(sql, buffered: false, commandTimeout: 7200);

                // Unfortunately, if you don't do this, then the connection gets closed as soon as
                // you return the lazy enumerable, which then kills the query. So you have to do this, 
                // which will ensure that the connection isn't closed until the end of the stream.
                foreach (var donor in donorStream)
                {
                    yield return donor;
                }
            }
        }

        public async Task<IReadOnlyDictionary<string, Donor>> GetDonorsByExternalDonorCodes(ICollection<string> externalDonorCodes)
        {
            var sql = @$"
SELECT {Donor.ColumnNamesForRead.StringJoin(",")} FROM {Donor.QualifiedTableName}
WHERE {nameof(Donor.ExternalDonorCode)} IN @codes
";

            await using (var connection = NewConnection())
            {
                var donors = await externalDonorCodes.ProcessInBatchesAsync(
                    DonorReadBatchSize,
                    async codes => await connection.QueryAsync<Donor>(sql, new {codes})
                );
                return donors.ToDictionary(d => d.ExternalDonorCode, d => d);
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<int, Donor>> GetDonorsByIds(ICollection<int> donorIds)
        {
            var sql = @$"
SELECT {Donor.ColumnNamesForRead.StringJoin(",")} FROM {Donor.QualifiedTableName}
WHERE {nameof(Donor.AtlasId)} IN @ids
";

            await using (var connection = NewConnection())
            {
                var donors = await donorIds.ProcessInBatchesAsync(
                    DonorReadBatchSize,
                    async ids => await connection.QueryAsync<Donor>(sql, new {ids})
                );
                return donors.ToDictionary(d => d.AtlasId, d => d);
            }
        }

        public async Task<IReadOnlyDictionary<string, int>> GetDonorIdsByExternalDonorCodes(ICollection<string> externalDonorCodes)
        {
            var sql = @$"
SELECT {nameof(Donor.AtlasId)}, {nameof(Donor.ExternalDonorCode)} FROM {Donor.QualifiedTableName}
WHERE {nameof(Donor.ExternalDonorCode)} IN @codes
";
            await using (var connection = NewConnection())
            {
                var donors = (await externalDonorCodes.ProcessInBatchesAsync(
                    DonorReadBatchSize,
                    async codes => await connection.QueryAsync<(int?, string)>(sql, new {codes})
                )).ToList();
                if (donors.Any(d => d.Item1 == null))
                {
                    var notFoundDonors = donors.Where(d => d.Item1 == null).Select(d => d.Item2);
                    throw new Exception($"External Donor Codes {notFoundDonors.StringJoin(",")} not found in database.");
                }
                return donors.ToDictionary(d => d.Item2, d => d.Item1.Value);
            }
        }

        public async Task<IReadOnlyDictionary<string, int>> GetDonorIdsUpdatedSince(DateTimeOffset cutoffDate)
        {
            var sql = $@"
SELECT {nameof(Donor.AtlasId)}, {nameof(Donor.ExternalDonorCode)} FROM {Donor.QualifiedTableName}
WHERE LastUpdated >= @{nameof(cutoffDate)}
";

            await using (var connection = NewConnection())
            {
                var donors = await connection.QueryAsync<Donor>(sql, new {cutoffDate});
                return donors.ToDictionary(d => d.ExternalDonorCode, d => d.AtlasId);
            }
        }
    }
}