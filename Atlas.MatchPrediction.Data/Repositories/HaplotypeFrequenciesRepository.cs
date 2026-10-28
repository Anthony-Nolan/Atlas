using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.MatchPrediction.Data.Repositories
{
    public interface IHaplotypeFrequenciesRepository
    {
        Task AddHaplotypeFrequencies(int haplotypeFrequencySetId, IEnumerable<HaplotypeFrequency> haplotypeFrequencies);

        /// <summary>
        /// The frequencies of the set, in the order of their ids: the order of the import. The order is part of the
        /// output of match prediction: it sets the tie-break of truncation (see <c>FrequencySetCacheEntry</c>).
        /// </summary>
        Task<List<LightweightHaplotypeFrequencyRecord>> GetAllHaplotypeFrequencies(int setId);

        Task RemoveHaplotypeFrequencies(int setId);
    }

    public class HaplotypeFrequenciesRepository : IHaplotypeFrequenciesRepository
    {
        private readonly string connectionString;

        public HaplotypeFrequenciesRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task AddHaplotypeFrequencies(int haplotypeFrequencySetId, IEnumerable<HaplotypeFrequency> haplotypeFrequencies)
        {
            haplotypeFrequencies = haplotypeFrequencies.ToList();
            if (!haplotypeFrequencies.Any())
            {
                return;
            }

            var dataTable = BuildFrequencyInsertDataTable(haplotypeFrequencySetId, haplotypeFrequencies);

            try
            {
                using var sqlBulk = BuildFrequencySqlBulkCopy();
                await sqlBulk.WriteToServerAsync(dataTable);
            }
            catch
            {
                await RemoveHaplotypeFrequencies(haplotypeFrequencySetId);
                throw;
            }
        }

        public async Task RemoveHaplotypeFrequencies(int setId)
        {
            var sql = $"DELETE FROM {HaplotypeFrequency.QualifiedTableName} WHERE Set_Id = @{nameof(setId)}";
            await using (var conn = new SqlConnection(connectionString))
            {
                await conn.ExecuteAsync(sql, new { setId });
            }
        }

        private SqlBulkCopy BuildFrequencySqlBulkCopy()
        {
            var sqlBulk = new SqlBulkCopy(connectionString)
                {BulkCopyTimeout = 3600, BatchSize = 10000, DestinationTableName = HaplotypeFrequency.QualifiedTableName};

            sqlBulk.ColumnMappings.Add(nameof(HaplotypeFrequency.Id), nameof(HaplotypeFrequency.Id));
            sqlBulk.ColumnMappings.Add(nameof(HaplotypeFrequency.Frequency), nameof(HaplotypeFrequency.Frequency));
            sqlBulk.ColumnMappings.Add(nameof(HaplotypeFrequency.A), nameof(HaplotypeFrequency.A));
            sqlBulk.ColumnMappings.Add(nameof(HaplotypeFrequency.B), nameof(HaplotypeFrequency.B));
            sqlBulk.ColumnMappings.Add(nameof(HaplotypeFrequency.C), nameof(HaplotypeFrequency.C));
            sqlBulk.ColumnMappings.Add(nameof(HaplotypeFrequency.DQB1), nameof(HaplotypeFrequency.DQB1));
            sqlBulk.ColumnMappings.Add(nameof(HaplotypeFrequency.DRB1), nameof(HaplotypeFrequency.DRB1));
            sqlBulk.ColumnMappings.Add(HaplotypeFrequency.SetIdColumnName, HaplotypeFrequency.SetIdColumnName);
            sqlBulk.ColumnMappings.Add(nameof(HaplotypeFrequency.TypingCategory), nameof(HaplotypeFrequency.TypingCategory));

            return sqlBulk;
        }

        private static DataTable BuildFrequencyInsertDataTable(int setId, IEnumerable<HaplotypeFrequency> frequencies)
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add(nameof(HaplotypeFrequency.Id));
            dataTable.Columns.Add(nameof(HaplotypeFrequency.Frequency));
            dataTable.Columns.Add(nameof(HaplotypeFrequency.A));
            dataTable.Columns.Add(nameof(HaplotypeFrequency.B));
            dataTable.Columns.Add(nameof(HaplotypeFrequency.C));
            dataTable.Columns.Add(nameof(HaplotypeFrequency.DQB1));
            dataTable.Columns.Add(nameof(HaplotypeFrequency.DRB1));
            dataTable.Columns.Add(HaplotypeFrequency.SetIdColumnName);
            dataTable.Columns.Add(nameof(HaplotypeFrequency.TypingCategory));

            foreach (var frequency in frequencies)
            {
                dataTable.Rows.Add(
                    0,
                    frequency.Frequency,
                    frequency.A,
                    frequency.B,
                    frequency.C,
                    frequency.DQB1,
                    frequency.DRB1,
                    setId,
                    (int) frequency.TypingCategory
                );
            }

            return dataTable;
        }

        /// <inheritdoc />
        /// <remarks>
        /// <para>
        /// <c>ORDER BY Id</c> fixes the order: without it, a plan on another index, or a parallel plan, could give the rows in
        /// another order. It costs no sort: the index on the set id also has the id in its key, as SQL Server adds the
        /// clustered key to a non-unique index.
        /// </para>
        /// <para>The read uses a connection without MARS: see <see cref="WithoutMultipleActiveResultSets"/>.</para>
        /// </remarks>
        public async Task<List<LightweightHaplotypeFrequencyRecord>> GetAllHaplotypeFrequencies(int setId)
        {
            var sql = @$"
SELECT {nameof(HaplotypeFrequency.A)},
{nameof(HaplotypeFrequency.B)},
{nameof(HaplotypeFrequency.C)},
{nameof(HaplotypeFrequency.DQB1)},
{nameof(HaplotypeFrequency.DRB1)},
{nameof(HaplotypeFrequency.Frequency)},
{nameof(HaplotypeFrequency.TypingCategory)},
{HaplotypeFrequency.SetIdColumnName} AS {nameof(LightweightHaplotypeFrequencyRecord.SetId)}
FROM {HaplotypeFrequency.QualifiedTableName}
WHERE {HaplotypeFrequency.SetIdColumnName} = @setId
ORDER BY {nameof(HaplotypeFrequency.Id)}";
            var readConnectionString = WithoutMultipleActiveResultSets(connectionString);

            return await RetryConfig.AsyncRetryPolicy.ExecuteAsync(async () =>
            {
                await using (var conn = new SqlConnection(readConnectionString))
                {
                    var frequencyModels = await conn.QueryAsync<LightweightHaplotypeFrequencyRecord>(sql, new {setId}, commandTimeout: 600);
                    return frequencyModels.ToList();
                }
            });
        }

        /// <summary>
        /// The connection string with MARS (Multiple Active Result Sets) off. With MARS, SQL Server sends a few packets and then
        /// waits for the client to acknowledge them (https://github.com/dotnet/SqlClient/issues/1283), so a read of many rows
        /// waits for one network round trip for each few packets. The read of a whole set needs no MARS: it opens its own
        /// connection, and runs one query. As this connection string differs from that of the other connections, the read
        /// must not run in a transaction scope with them: the transaction would become a distributed one. An empty connection
        /// string stays empty, so that a host without one fails as before.
        /// </summary>
        internal static string WithoutMultipleActiveResultSets(string connectionString) =>
            string.IsNullOrEmpty(connectionString)
                ? connectionString
                : new SqlConnectionStringBuilder(connectionString) { MultipleActiveResultSets = false }.ConnectionString;
    }
}