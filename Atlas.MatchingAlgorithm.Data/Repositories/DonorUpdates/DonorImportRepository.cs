using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchingAlgorithm.Common.Repositories;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using Microsoft.Data.SqlClient;

// ReSharper disable InconsistentNaming

namespace Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates
{
    /// <summary>
    /// Responsible for one-off, full donor imports
    /// </summary>
    public interface IDonorImportRepository
    {
        /// <summary>
        /// Performs any upfront work necessary to run a full donor import with reasonable performance.
        /// e.g. Removing indexes in a SQL implementation
        /// </summary>
        Task RemoveHlaTableIndexes();

        /// <summary>
        /// Performs any work necessary after a full donor import has been run.
        /// e.g. Re-adding indexes in a SQL implementation
        /// </summary>
        Task CreateHlaTableIndexes();

        /// <summary>
        /// Removes all donors, and all pre-processed data
        /// </summary>
        Task RemoveAllDonorInformation();

        /// <summary>
        /// Removes all donor pre-processed data, without removing donors themselves.
        /// </summary>
        Task RemoveAllProcessedDonorHla();

        /// <summary>
        /// Insert a batch of donors into the database.
        /// This does _not_ refresh or create the hla matches.
        /// </summary>
        Task InsertBatchOfDonors(IEnumerable<DonorInfo> donors);

        /// <summary>
        /// Adds pre-processed matching p-groups for a batch of donors
        /// Used when adding donors
        /// </summary>
        Task AddMatchingPGroupsForExistingDonorBatch(IEnumerable<DonorInfoWithExpandedHla> donors);

        Task RemovePGroupsForDonorBatch(IEnumerable<int> donorIds);
    }

    public class DonorImportRepository : DonorUpdateRepositoryBase, IDonorImportRepository
    {
        public DonorImportRepository(
            IPGroupRepository pGroupRepository,
            IConnectionStringProvider connectionStringProvider) : base(pGroupRepository, connectionStringProvider)
        {
        }

        private const string MatchingHlaTable_IndexName_PGroupIdAndDonorId = "IX_PGroup_Id_DonorId__TypePosition";
        private const string MatchingHlaTable_IndexName_DonorId = "IX_DonorId__PGroup_Id_TypePosition";
        private static readonly string[] HlaTables = {"MatchingHlaAtA", "MatchingHlaAtB", "MatchingHlaAtC", "MatchingHlaAtDrb1", "MatchingHlaAtDqb1"};
        
        private const string DropAllDonorsSql = @"TRUNCATE TABLE [Donors]";
        private string GetDropAllPreProcessedDonorHlaSql() => HlaTables.Select(table => $"TRUNCATE TABLE [{table}];").StringJoinWithNewline();

        private string GetPGroupIndexSqlFor(string tableName)
        {
            return GetIndexCreationSqlFor(
                MatchingHlaTable_IndexName_PGroupIdAndDonorId,
                tableName,
                new[] { "DonorId", "PGroup_Id" },
                new[] { "TypePosition" }
            );
        }

        private string GetDonorIdIndexSqlFor(string tableName)
        {
            return GetIndexCreationSqlFor(
                MatchingHlaTable_IndexName_DonorId,
                tableName,
                new[] {"DonorId"},
                new[] {"TypePosition", "PGroup_Id"}
            );
        }

        /// <param name="indexName">Name to use for index</param>
        /// <param name="tableName">Name of table (in default schema) to create index on</param>
        /// <param name="indexColumns">Columns to be part of the index itself. Must not be null or empty.</param>
        /// <param name="includeColumns">Columns to be 'INCLUDE'd as secondary columns of the index. Must not be null or empty</param>
        /// <returns>Conditional CREATE statement, which will create the index if it doesn't already exist.</returns>
        private string GetIndexCreationSqlFor(string indexName, string tableName, string[] indexColumns, string[] includeColumns)
        {
            var indexColumnsString = indexColumns.StringJoin(", ");
            var includeColumnsString = includeColumns.StringJoin(", ");
            return $@"
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='{indexName}' AND object_id = OBJECT_ID('dbo.{tableName}'))
BEGIN
    CREATE INDEX {indexName}
        ON {tableName} ({indexColumnsString})
        INCLUDE ({includeColumnsString})
END
";
        }

        /// <param name="indexName">Name to use for index</param>
        /// <param name="tableName">Name of table (in default schema) to create index on</param>
        /// <returns>Conditional DELETE IF EXISTS statement, which will delete the index if it currently exists.</returns>
        private string GetIndexDeletionSqlFor(string indexName, string tableName)
        {
            return $@"DROP INDEX IF EXISTS {indexName} ON [{tableName}];";
        }

        public async Task CreateHlaTableIndexes()
        {
            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                foreach (var table in HlaTables)
                {
                    var pGroupIndexSql = GetPGroupIndexSqlFor(table);
                    await conn.ExecuteAsync(pGroupIndexSql, commandTimeout: 7200);

                    var donorIdIndexSql = GetDonorIdIndexSqlFor(table);
                    await conn.ExecuteAsync(donorIdIndexSql, commandTimeout: 7200);
                }
            }
        }

        public async Task RemoveHlaTableIndexes()
        {
            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                foreach (var table in HlaTables)
                {
                    var pGroupIndexSql = GetIndexDeletionSqlFor(MatchingHlaTable_IndexName_PGroupIdAndDonorId, table);
                    await conn.ExecuteAsync(pGroupIndexSql, commandTimeout: 300);

                    var donorIdIndexSql = GetIndexDeletionSqlFor(MatchingHlaTable_IndexName_DonorId, table);
                    await conn.ExecuteAsync(donorIdIndexSql, commandTimeout: 300);
                }
            }
        }

        public async Task RemoveAllDonorInformation()
        {
            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                await conn.ExecuteAsync(GetDropAllPreProcessedDonorHlaSql(), commandTimeout: 300);
                await conn.ExecuteAsync(DropAllDonorsSql, commandTimeout: 300);
            }
        }

        /// <inheritdoc />
        public async Task RemoveAllProcessedDonorHla()
        {
            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                await conn.ExecuteAsync(GetDropAllPreProcessedDonorHlaSql(), commandTimeout: 300);
            }
        }

        public new async Task InsertBatchOfDonors(IEnumerable<DonorInfo> donors)
        {
            await base.InsertBatchOfDonors(donors);
        }

        public new async Task AddMatchingPGroupsForExistingDonorBatch(IEnumerable<DonorInfoWithExpandedHla> donors)
        {
            await base.AddMatchingPGroupsForExistingDonorBatch(donors);
        }

        public async Task RemovePGroupsForDonorBatch(IEnumerable<int> donorIds)
        {
            donorIds = donorIds.ToList();
            foreach (var hlaTable in HlaTables)
            {
                await RemovePGroupsForDonorBatchAtLocus(donorIds, hlaTable);
            }
        }

        private async Task RemovePGroupsForDonorBatchAtLocus(IEnumerable<int> donorIds, string locusTableName)
        {
            var removalSql = $@"
DELETE FROM {locusTableName}
WHERE DonorId IN ({string.Join(",", donorIds)});
";

            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                await conn.ExecuteAsync(removalSql, commandTimeout: 600);
            }
        }
    }
}