using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using HaplotypeHla = Atlas.Common.GeneticData.PhenotypeInfo.LociInfo<string>;

namespace Atlas.MatchPrediction.Data.Repositories
{
    public interface IHaplotypeFrequenciesRepository
    {
        Task AddHaplotypeFrequencies(int haplotypeFrequencySetId, IEnumerable<HaplotypeFrequency> haplotypeFrequencies);

        /// <summary>
        /// If HF already present, add frequencies together.
        /// </summary>
        Task AddOrUpdateHaplotypeFrequencies(int haplotypeFrequencySetId, IEnumerable<HaplotypeFrequency> haplotypeFrequencies);

        Task<Dictionary<HaplotypeHla, decimal>> GetHaplotypeFrequencies(IEnumerable<HaplotypeHla> haplotypes, int setId);
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

        /// <inheritdoc />
        public async Task AddOrUpdateHaplotypeFrequencies(int haplotypeFrequencySetId, IEnumerable<HaplotypeFrequency> haplotypeFrequencies)
        {
            haplotypeFrequencies = haplotypeFrequencies.ToList();
            if (!haplotypeFrequencies.Any())
            {
                return;
            }

            var existingFrequencies = await GetAllHaplotypeFrequencies(haplotypeFrequencySetId);

            var (updates, inserts) = haplotypeFrequencies.ReifyAndSplit(f => existingFrequencies.ContainsKey(f.Hla));

            // TODO: ATLAS-590: This might not be the most time-efficient way of performing this import.
            // Reading entire file in-memory and bulk inserting might be faster. 
            // Raise card to tackle efficiency as this seems to be "good enough"? (2 min locally for largest set)
            // TODO: ATLAS-590: Actually, this probably won't fly on devops - the import uses 12K SQL connections
            foreach (var update in updates)
            {
                var existingFrequency = existingFrequencies[update.Hla].Frequency;
                var combinedFrequency = existingFrequency + update.Frequency;
                const string sql = @"
UPDATE HaplotypeFrequencies
SET Frequency = @frequency 
WHERE A = @A AND B = @B AND C = @C AND DQB1 = @Dqb1 AND DRB1 = @Drb1 AND Set_Id = @setId;";

                await using (var conn = new SqlConnection(connectionString))
                {
                    await conn.ExecuteAsync(sql,
                        new {update.A, update.B, update.C, update.DQB1, update.DRB1, SetId = haplotypeFrequencySetId, Frequency = combinedFrequency}
                    );
                }
            }

            var dataTable = BuildFrequencyInsertDataTable(haplotypeFrequencySetId, inserts);

            using (var sqlBulk = BuildFrequencySqlBulkCopy())
            {
                await sqlBulk.WriteToServerAsync(dataTable);
            }
        }

        private SqlBulkCopy BuildFrequencySqlBulkCopy()
        {
            var sqlBulk = new SqlBulkCopy(connectionString)
                {BulkCopyTimeout = 3600, BatchSize = 10000, DestinationTableName = "HaplotypeFrequencies"};

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

        public async Task<Dictionary<HaplotypeHla, decimal>> GetHaplotypeFrequencies(IEnumerable<HaplotypeHla> haplotypes, int setId)
        {
            var haplotypeInfo = new Dictionary<HaplotypeHla, decimal>();
            var distinctHaplotypes = haplotypes.ToList().Distinct();

            // TODO: ATLAS-2: Investigate if quicker to run multiple queries vs collated one to fetch everything in one go.
            var sql = @$"
                SELECT Frequency 
                FROM HaplotypeFrequencies
                WHERE 
                    A = @A AND
                    B = @B AND
                    C = @C AND
                    DQB1 = @Dqb1 AND
                    DRB1 = @Drb1 AND
                    Set_Id = @setId
                ";

            await using (var conn = new SqlConnection(connectionString))
            {
                foreach (var haplotype in distinctHaplotypes)
                {
                    var frequency = await conn.QueryFirstOrDefaultAsync<decimal>(
                        sql,
                        new {haplotype.A, haplotype.B, haplotype.C, haplotype.Dqb1, haplotype.Drb1, setId},
                        commandTimeout: 300);
                    haplotypeInfo.Add(haplotype, frequency);
                }
            }

            return haplotypeInfo;
        }

        /// <inheritdoc />
        public async Task<Dictionary<HaplotypeHla, HaplotypeFrequency>> GetAllHaplotypeFrequencies(int setId)
        {
            const string sql = "SELECT * FROM HaplotypeFrequencies WHERE Set_Id = @setId";
            await using (var conn = new SqlConnection(connectionString))
            {
                var frequencyModels = await conn.QueryAsync<HaplotypeFrequency>(sql, new {setId}, commandTimeout: 600);
                return frequencyModels.ToDictionary(
                    f => f.Haplotype(),
                    f => f
                );
            }
        }
    }
}