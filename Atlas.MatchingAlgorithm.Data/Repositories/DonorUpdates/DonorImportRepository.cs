using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchingAlgorithm.Common.Repositories;
using Atlas.MatchingAlgorithm.Data.Models;
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
        Task FullHlaRefreshSetUp();

        /// <summary>
        /// Performs any work necessary after a full donor import has been run.
        /// e.g. Re-adding indexes in a SQL implementation
        /// </summary>
        Task FullHlaRefreshTearDown();

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
        private const string MatchingHlaTable_IndexName_PGroupIdAndDonorId = "IX_PGroup_Id_DonorId__TypePosition";
        private const string MatchingHlaTable_IndexName_DonorId = "IX_DonorId__PGroup_Id_TypePosition";

        private const string DropAllDonorsSql = @"
TRUNCATE TABLE [Donors]
";
        
        private const string DropAllPreProcessedDonorHlaSql = @"
TRUNCATE TABLE [MatchingHlaAtA]
TRUNCATE TABLE [MatchingHlaAtB]
TRUNCATE TABLE [MatchingHlaAtC]
TRUNCATE TABLE [MatchingHlaAtDrb1]
TRUNCATE TABLE [MatchingHlaAtDqb1]
";

        public DonorImportRepository(
            IPGroupRepository pGroupRepository,
            IConnectionStringProvider connectionStringProvider) : base(pGroupRepository, connectionStringProvider)
        {
        }

        public async Task FullHlaRefreshSetUp()
        {
            var indexRemovalSql = $@"
DROP INDEX IF EXISTS {MatchingHlaTable_IndexName_PGroupIdAndDonorId} ON MatchingHlaAtA;
DROP INDEX IF EXISTS {MatchingHlaTable_IndexName_PGroupIdAndDonorId} ON MatchingHlaAtB;
DROP INDEX IF EXISTS {MatchingHlaTable_IndexName_PGroupIdAndDonorId} ON MatchingHlaAtC;
DROP INDEX IF EXISTS {MatchingHlaTable_IndexName_PGroupIdAndDonorId} ON MatchingHlaAtDrb1;
DROP INDEX IF EXISTS {MatchingHlaTable_IndexName_PGroupIdAndDonorId} ON MatchingHlaAtDqb1;
DROP INDEX IF EXISTS {MatchingHlaTable_IndexName_DonorId} ON MatchingHlaAtA;
DROP INDEX IF EXISTS {MatchingHlaTable_IndexName_DonorId} ON MatchingHlaAtB;
DROP INDEX IF EXISTS {MatchingHlaTable_IndexName_DonorId} ON MatchingHlaAtC;
DROP INDEX IF EXISTS {MatchingHlaTable_IndexName_DonorId} ON MatchingHlaAtDrb1;
DROP INDEX IF EXISTS {MatchingHlaTable_IndexName_DonorId} ON MatchingHlaAtDqb1;
";
            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                await conn.ExecuteAsync(indexRemovalSql, commandTimeout: 600);
            }
        }

        public string SqlForPGroupIndexFor(string tableName)
        {
            return SqlForIndex(
                MatchingHlaTable_IndexName_PGroupIdAndDonorId,
                tableName,
                new[] { "DonorId", "PGroup_Id" },
                new[] { "TypePosition" }
            );
        }

        public string SqlForDonorIndexFor(string tableName)
        {
            return SqlForIndex(
                MatchingHlaTable_IndexName_DonorId,
                tableName,
                new[] {"DonorId"},
                new[] {"TypePosition", "PGroup_Id"}
            );
        }

        public string SqlForIndex(string indexName, string tableName, string[] indexColumns, string[] includeColumns)
        {
            var indexColumnsString = indexColumns.StringJoin(", ");
            var includeColumnsString = includeColumns.StringJoin(", ");
            return $@"
IF NOT EXIST (SELECT * FROM sys.indexes WHERE name='{indexName}' AND object_id = OBJECT_ID('dbo.{tableName}'))
BEGIN
    CREATE INDEX {indexName}
        ON {tableName} ({indexColumnsString})
        INCLUDE ({includeColumnsString})
END
";
        }

        public async Task FullHlaRefreshTearDown()
        {
            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                var tables = new[] {"MatchingHlaAtA", "MatchingHlaAtB", "MatchingHlaAtC", "MatchingHlaAtDrb1", "MatchingHlaAtDqb1"};
                foreach (var table in tables)
                {
                    var pGroupIndexSql = SqlForPGroupIndexFor(table);
                    await conn.ExecuteAsync(pGroupIndexSql, commandTimeout: 7200);
                    
                    var donorIndexSql = SqlForDonorIndexFor(table);
                    await conn.ExecuteAsync(donorIndexSql, commandTimeout: 7200);
                }
            }
        }

        public async Task RemoveAllDonorInformation()
        {
            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                await conn.ExecuteAsync(DropAllPreProcessedDonorHlaSql, commandTimeout: 300);
                await conn.ExecuteAsync(DropAllDonorsSql, commandTimeout: 300);
            }
        }

        /// <inheritdoc />
        public async Task RemoveAllProcessedDonorHla()
        {
            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                await conn.ExecuteAsync(DropAllPreProcessedDonorHlaSql, commandTimeout: 300);
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
            await RemovePGroupsForDonorBatchAtLocus(donorIds, "MatchingHlaAtA");
            await RemovePGroupsForDonorBatchAtLocus(donorIds, "MatchingHlaAtB");
            await RemovePGroupsForDonorBatchAtLocus(donorIds, "MatchingHlaAtC");
            await RemovePGroupsForDonorBatchAtLocus(donorIds, "MatchingHlaAtDqb1");
            await RemovePGroupsForDonorBatchAtLocus(donorIds, "MatchingHlaAtDrb1");
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