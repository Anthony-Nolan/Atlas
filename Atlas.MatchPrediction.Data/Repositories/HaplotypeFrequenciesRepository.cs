using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using HaplotypeHla = Atlas.Common.GeneticData.PhenotypeInfo.LociInfo<string>;

namespace Atlas.MatchPrediction.Data.Repositories
{
    public interface IHaplotypeFrequenciesRepository
    {
        Task AddHaplotypeFrequencies(int haplotypeFrequencySetId, IEnumerable<HaplotypeFrequency> haplotypeFrequencies);
        Task<Dictionary<HaplotypeHla, decimal>> GetHaplotypeFrequencies(IEnumerable<HaplotypeHla> haplotypes, int setId);
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
            var sqlBulk = new SqlBulkCopy(connectionString) {BulkCopyTimeout = 3600, BatchSize = 10000, DestinationTableName = "HaplotypeFrequencies"};

            sqlBulk.ColumnMappings.Add(nameof(HaplotypeFrequency.Id), nameof(HaplotypeFrequency.Id));
            sqlBulk.ColumnMappings.Add(nameof(HaplotypeFrequency.Frequency), nameof(HaplotypeFrequency.Frequency));
            sqlBulk.ColumnMappings.Add(nameof(HaplotypeFrequency.A), nameof(HaplotypeFrequency.A));
            sqlBulk.ColumnMappings.Add(nameof(HaplotypeFrequency.B), nameof(HaplotypeFrequency.B));
            sqlBulk.ColumnMappings.Add(nameof(HaplotypeFrequency.C), nameof(HaplotypeFrequency.C));
            sqlBulk.ColumnMappings.Add(nameof(HaplotypeFrequency.DQB1), nameof(HaplotypeFrequency.DQB1));
            sqlBulk.ColumnMappings.Add(nameof(HaplotypeFrequency.DRB1), nameof(HaplotypeFrequency.DRB1));
            sqlBulk.ColumnMappings.Add(HaplotypeFrequency.SetIdColumnName, HaplotypeFrequency.SetIdColumnName);

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
                    setId
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

            using (var conn = new SqlConnection(connectionString))
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
    }
}