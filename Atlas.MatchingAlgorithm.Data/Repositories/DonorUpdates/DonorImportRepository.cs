using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using LoggingStopwatch;
using Microsoft.Data.SqlClient;
using static Atlas.Common.GeneticData.Locus;
using static Atlas.MatchingAlgorithm.Data.Helpers.MatchingTableNameHelper;

// ReSharper disable InconsistentNaming

namespace Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates
{
    /// <summary>
    /// Responsible for one-off, full donor imports
    /// </summary>
    public interface IDonorImportRepository
    {
        /// <summary>
        /// Remove Indexes from the SQL Tables, so that a full donor import
        /// can be run with reasonable performance.
        /// </summary>
        Task RemoveHlaTableIndexes();

        /// <summary>
        /// Reinstate the indexes which were previously removed, to avoid performance hits during Refresh.
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
        Task AddMatchingPGroupsForExistingDonorBatch(
            IEnumerable<DonorInfoWithExpandedHla> donors,
            bool runAllHlaInsertionsInASingleTransactionScope,
            LongStopwatchCollection timerCollection = null);
    }

    public class DonorImportRepository : DonorUpdateRepositoryBase, IDonorImportRepository
    {
        public DonorImportRepository(
            IPGroupRepository pGroupRepository,
            IConnectionStringProvider connectionStringProvider,
            ILogger logger) : base(pGroupRepository, connectionStringProvider, logger)
        {
        }

        private const string MatchingHlaTable_IndexName_PGroupIdAndDonorId = "IX_PGroup_Id_DonorId__TypePosition";
        private const string MatchingHlaTable_IndexName_DonorId = "IX_DonorId__PGroup_Id_TypePosition";

        private static readonly string[] HlaTables =
            {MatchingTableName(A), MatchingTableName(B), MatchingTableName(C), MatchingTableName(Drb1), MatchingTableName(Dqb1)};

        private const string DropAllDonorsSql = @"TRUNCATE TABLE [Donors]";
        private string BuildDropAllPreProcessedDonorHlaSql() => HlaTables.Select(table => $"TRUNCATE TABLE [{table}];").StringJoinWithNewline();

        private string BuildPGroupIndexSqlFor(string tableName)
        {
            return BuildIndexCreationSqlFor(
                MatchingHlaTable_IndexName_PGroupIdAndDonorId,
                tableName,
                new[] {"PGroup_Id", "DonorId"},
                new[] {"TypePosition"}
            );
        }

        private string BuildDonorIdIndexSqlFor(string tableName)
        {
            return BuildIndexCreationSqlFor(
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
        private string BuildIndexCreationSqlFor(string indexName, string tableName, string[] indexColumns, string[] includeColumns)
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
        private string BuildIndexDeletionSqlFor(string indexName, string tableName)
        {
            return $@"DROP INDEX IF EXISTS {indexName} ON [{tableName}];";
        }

        public async Task CreateHlaTableIndexes()
        {
            var logSettings = new LongLoggingSettings
            {
                ExpectedNumberOfIterations = HlaTables.Length * 2,
                InnerOperationLoggingPeriod = 1,
                ReportProjectedCompletionTime = false,
                ReportPercentageCompletion = false,
            };
            const string text = "Creating HLA Table Indexes. Tables in order: A, B, C, Drb1, Dqb1. PGroup then DonorId, for each table.";
            using (var timer = logger.RunLongOperationWithTimer(text, logSettings))
            {
                await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
                {
                    foreach (var table in HlaTables)
                    {
                        using (timer.TimeInnerOperation())
                        {
                            var pGroupIndexSql = BuildPGroupIndexSqlFor(table);
                            await conn.ExecuteAsync(pGroupIndexSql, commandTimeout: 43200);
                        }

                        using (timer.TimeInnerOperation())
                        {
                            var donorIdIndexSql = BuildDonorIdIndexSqlFor(table);
                            await conn.ExecuteAsync(donorIdIndexSql, commandTimeout: 43200);
                        }
                    }
                }
            }
        }

        public async Task RemoveHlaTableIndexes()
        {
            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                foreach (var table in HlaTables)
                {
                    var pGroupIndexSql = BuildIndexDeletionSqlFor(MatchingHlaTable_IndexName_PGroupIdAndDonorId, table);
                    await conn.ExecuteAsync(pGroupIndexSql, commandTimeout: 300);

                    var donorIdIndexSql = BuildIndexDeletionSqlFor(MatchingHlaTable_IndexName_DonorId, table);
                    await conn.ExecuteAsync(donorIdIndexSql, commandTimeout: 300);
                }
            }
        }

        public async Task RemoveAllDonorInformation()
        {
            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                await conn.ExecuteAsync(BuildDropAllPreProcessedDonorHlaSql(), commandTimeout: 300);
                await conn.ExecuteAsync(DropAllDonorsSql, commandTimeout: 300);
            }
        }

        /// <inheritdoc />
        public async Task RemoveAllProcessedDonorHla()
        {
            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                await conn.ExecuteAsync(BuildDropAllPreProcessedDonorHlaSql(), commandTimeout: 300);
            }
        }
    }
}