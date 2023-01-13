using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using HaplotypeHla = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.LociInfo<string>;

namespace Atlas.MatchPrediction.Data.Repositories
{
    public interface IHaplotypeFrequenciesRepository
    {
        Task AddHaplotypeFrequencies(int haplotypeFrequencySetId, IEnumerable<HaplotypeFrequency> haplotypeFrequencies);

        Task<Dictionary<HaplotypeHla, HaplotypeFrequency>> GetAllHaplotypeFrequencies(int setId);
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

            using (var sqlBulk = BuildFrequencySqlBulkCopy())
            {
                await sqlBulk.WriteToServerAsync(dataTable);
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
        public async Task<Dictionary<HaplotypeHla, HaplotypeFrequency>> GetAllHaplotypeFrequencies(int setId)
        {
            var sql = @$"
SELECT {nameof(HaplotypeFrequency.Id)},
{nameof(HaplotypeFrequency.A)},
{nameof(HaplotypeFrequency.B)},
{nameof(HaplotypeFrequency.C)},
{nameof(HaplotypeFrequency.DQB1)},
{nameof(HaplotypeFrequency.DRB1)}, 
{nameof(HaplotypeFrequency.Frequency)}, 
{nameof(HaplotypeFrequency.TypingCategory)},
{HaplotypeFrequency.SetIdColumnName}
FROM {HaplotypeFrequency.QualifiedTableName} 
WHERE {HaplotypeFrequency.SetIdColumnName} = @setId";

            return await RetryConfig.AsyncRetryPolicy.ExecuteAsync(async () =>
            {
                await using (var conn = new SqlConnection(connectionString))
                {
                    var frequencyModels = await conn.QueryAsync<HaplotypeFrequency>(sql, new {setId}, commandTimeout: 600);
                    return frequencyModels.ToDictionary(
                        f => f.Haplotype(),
                        f => f
                    );
                }
            });
        }
    }
}