using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using Microsoft.Data.SqlClient;
using static Atlas.Common.Public.Models.GeneticData.Locus;

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
        /// Removes all donors, and all pre-processed data - including the precomputed subject genotype sets
        /// (<see cref="SubjectGenotypeSetValue"/> / <see cref="DonorSubjectGenotypeSet"/>), and the run, batch and staging
        /// tables of the donor genotype precomputation stage
        /// </summary>
        Task RemoveAllDonorInformation();

        /// <summary>
        /// ATL-491 experiment only. Removes the precomputed subject genotype sets, and the run, batch and staging tables of
        /// the donor genotype precomputation stage. The donors and their processed HLA stay.
        /// <para>
        /// ATL-495 experiment only: then rebuilds the empty <c>SubjectGenotypeSetValues</c> and <c>DonorSubjectGenotypeSets</c>
        /// tables with <paramref name="bucketCount"/> hash partitions - see <c>DonorImportRepository.BuildReshapeGenotypeSetTablesSql</c>.
        /// </para>
        /// </summary>
        Task RemoveAllPrecomputedGenotypeSets(int bucketCount);

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
        /// Opens a session over which this repository's bulk copies are reused across writes, rather than rebuilt for
        /// each one. Intended to be held for the length of a data refresh stage, and disposed when the stage is done.
        /// </summary>
        IDisposable OpenBulkWriteSession();

        /// <summary>
        /// Adds pre-processed matching p-groups for a batch of donors
        /// Used when adding donors
        /// </summary>
        Task AddMatchingRelationsForExistingDonorBatch(
            IEnumerable<DonorInfoForHlaPreProcessing> donors,
            bool runAllHlaInsertionsInASingleTransactionScope,
            LongStopwatchCollection timerCollection = null);
    }

    public class DonorImportRepository : DonorUpdateRepositoryBase, IDonorImportRepository
    {
        public DonorImportRepository(
            IHlaNamesRepository hlaNamesRepository,
            IConnectionStringProvider connectionStringProvider,
            IAtlasLogger logger) : base(connectionStringProvider, logger)
        {
        }

        private const string HlaRelationTable_IndexName_PGroupIdAndHlaNameId = "IX_PGroupId_HlaNameId";
        private const string HlaRelationTable_IndexName_HlaNameId = "IX_HlaNameId";
        private const string MatchingHlaTable_IndexName_HlaNameIdAndDonorId = "IX_HlaNameId_DonorId__TypePosition";
        private const string MatchingHlaTable_IndexName_DonorId = "IX_DonorId__PGroupId_TypePosition";

        private static readonly string[] MatchingHlaTables =
        {
            MatchingHla.TableName(A),
            MatchingHla.TableName(B),
            MatchingHla.TableName(C),
            MatchingHla.TableName(Drb1),
            MatchingHla.TableName(Dqb1)
        };
        
        private static readonly string[] HlaRelationTables =
        {
            HlaNamePGroupRelation.TableName(A),
            HlaNamePGroupRelation.TableName(B),
            HlaNamePGroupRelation.TableName(C),
            HlaNamePGroupRelation.TableName(Drb1),
            HlaNamePGroupRelation.TableName(Dqb1)
        };

        private static readonly string[] AllHlaTables = MatchingHlaTables.Concat(HlaRelationTables).ToArray(); 

        private const string DropAllDonorsSql = @"TRUNCATE TABLE [Donors]";
        private const string DropAllDonorManagementLogsSql = @"TRUNCATE TABLE [DonorManagementLogs]";

        // Must not be removed from the data refresh wipe - see SubjectGenotypeSetValue for why.
        private const string DropAllSubjectGenotypeSetsSql = @"
TRUNCATE TABLE [DonorSubjectGenotypeSets];
TRUNCATE TABLE [SubjectGenotypeSetValues];";

        // Must not be removed from the data refresh wipe either. The build of the precomputation stage gives the group ids
        // itself, from 1, so the groups of a run that stopped before its staging data was removed would collide with the
        // next build on this database. Also, the stage keeps one run per database, and its state machine reads the run and
        // batch rows of that run only.
        private const string DropAllDonorGenotypePrecomputationRunsSql = @"
TRUNCATE TABLE [DonorGenotypePrecomputationGroupDonors];
TRUNCATE TABLE [DonorGenotypePrecomputationGroups];
TRUNCATE TABLE [DonorGenotypePrecomputationBatches];
TRUNCATE TABLE [DonorGenotypePrecomputationRuns];";

        private static string BuildDropAllPreProcessedDonorHlaSql() =>
            AllHlaTables.Select(table => $"TRUNCATE TABLE [{table}];").StringJoinWithNewline();

        private IEnumerable<string> BuildRelationIndexesSqlFor(string tableName)
        {
            yield return BuildIndexCreationSqlFor(
            HlaRelationTable_IndexName_PGroupIdAndHlaNameId,
            tableName,
            new[] { "PGroupId", "HlaNameId" });

            yield return BuildIndexCreationSqlFor(
            HlaRelationTable_IndexName_HlaNameId,
            tableName,
            new[] { "HlaNameId" },
            new[] { "PGroupId" });
        }

        private string BuildPGroupIndexSqlFor(string tableName) => BuildIndexCreationSqlFor(
            MatchingHlaTable_IndexName_HlaNameIdAndDonorId,
            tableName,
            new[] {"HlaNameId", "DonorId"},
            new[] {"TypePosition"}
        );

        private string BuildDonorIdIndexSqlFor(string tableName) => BuildIndexCreationSqlFor(
            MatchingHlaTable_IndexName_DonorId,
            tableName,
            new[] {"DonorId"},
            new[] {"TypePosition", "HlaNameId"}
        );

        /// <param name="indexName">Name to use for index</param>
        /// <param name="tableName">Name of table (in default schema) to create index on</param>
        /// <param name="indexColumns">Columns to be part of the index itself. Must not be null or empty.</param>
        /// <param name="includeColumns">Columns to be 'INCLUDE'd as secondary columns of the index. Must not be null or empty</param>
        /// <returns>Conditional CREATE statement, which will create the index if it doesn't already exist.</returns>
        private string BuildIndexCreationSqlFor(string indexName, string tableName, string[] indexColumns, string[] includeColumns = null)
        {
            includeColumns ??= new string[0];
            var indexColumnsString = indexColumns.StringJoin(", ");
            var includeColumnsString = includeColumns.StringJoin(", ");
            var includeSql = includeColumns.Any() ? $"INCLUDE ({includeColumnsString})" : "";
            return $@"
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='{indexName}' AND object_id = OBJECT_ID('dbo.{tableName}'))
BEGIN
    CREATE INDEX {indexName}
        ON {tableName} ({indexColumnsString})
        {includeSql}
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
                ExpectedNumberOfIterations = MatchingHlaTables.Length * 2,
                InnerOperationLoggingPeriod = 1,
                ReportProjectedCompletionTime = false,
                ReportPercentageCompletion = false,
            };
            const string text = "Creating HLA Table Indexes. Tables in order: A, B, C, Drb1, Dqb1. PGroup then DonorId, for each table.";
            using (var timer = logger.RunLongOperationWithTimer(text, logSettings))
            {
                await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
                {
                    foreach (var table in MatchingHlaTables)
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

                    foreach (var table in HlaRelationTables)
                    {
                        using (timer.TimeInnerOperation())
                        {
                            await CreateRelationIndexesFor(table, conn);
                        }
                    }
                }
            }
        }

        private async Task CreateRelationIndexesFor(string table, SqlConnection conn)
        {
            foreach (var indexSql in BuildRelationIndexesSqlFor(table))
            {
                await conn.ExecuteAsync(indexSql, commandTimeout: 43200);
            }
        }

        public async Task RemoveHlaTableIndexes()
        {
            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                foreach (var table in AllHlaTables)
                {
                    var pGroupIndexSql = BuildIndexDeletionSqlFor(MatchingHlaTable_IndexName_HlaNameIdAndDonorId, table);
                    await conn.ExecuteAsync(pGroupIndexSql, commandTimeout: 300);

                    var donorIdIndexSql = BuildIndexDeletionSqlFor(MatchingHlaTable_IndexName_DonorId, table);
                    await conn.ExecuteAsync(donorIdIndexSql, commandTimeout: 300);
                    
                    var hlaRelationIndexSql = BuildIndexDeletionSqlFor(HlaRelationTable_IndexName_PGroupIdAndHlaNameId, table);
                    await conn.ExecuteAsync(hlaRelationIndexSql, commandTimeout: 300);

                    hlaRelationIndexSql = BuildIndexDeletionSqlFor(HlaRelationTable_IndexName_HlaNameId, table);
                    await conn.ExecuteAsync(hlaRelationIndexSql, commandTimeout: 300);
                }
            }
        }

        public async Task RemoveAllDonorInformation()
        {
            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                await conn.ExecuteAsync(BuildDropAllPreProcessedDonorHlaSql(), commandTimeout: 300);
                await conn.ExecuteAsync(DropAllDonorsSql, commandTimeout: 300);
                await conn.ExecuteAsync(DropAllDonorManagementLogsSql, commandTimeout: 300);
                await conn.ExecuteAsync(DropAllSubjectGenotypeSetsSql, commandTimeout: 300);
                await conn.ExecuteAsync(DropAllDonorGenotypePrecomputationRunsSql, commandTimeout: 300);
            }
        }

        /// <inheritdoc />
        public async Task RemoveAllPrecomputedGenotypeSets(int bucketCount)
        {
            var reshapeSql = BuildReshapeGenotypeSetTablesSql(bucketCount);

            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                await conn.ExecuteAsync(DropAllSubjectGenotypeSetsSql, commandTimeout: 300);
                await conn.ExecuteAsync(reshapeSql, commandTimeout: 300);
                await conn.ExecuteAsync(DropAllDonorGenotypePrecomputationRunsSql, commandTimeout: 300);
            }
        }

        /// <summary>ATL-495 experiment only: the bucket of a row is <c>Id % MaxBucketCount</c>.</summary>
        internal const int MaxBucketCount = 32;

        /// <summary>ATL-495 experiment only: a table that <see cref="BuildReshapeGenotypeSetTablesSql"/> rebuilds.</summary>
        /// <param name="SequentialKeyInMigrations">
        /// The <c>OPTIMIZE_FOR_SEQUENTIAL_KEY</c> value that the migrations set on the primary key, put back with one bucket.
        /// </param>
        private record BucketedTable(string Name, string UniqueIndex, string UniqueIndexColumns, bool SequentialKeyInMigrations)
        {
            public string PrimaryKey => $"PK_{Name}";
            public string PartitionFunction => $"PF_{Name}_Bucket";
            public string PartitionScheme => $"PS_{Name}_Bucket";
        }

        private static readonly BucketedTable[] BucketedTables =
        {
            new("SubjectGenotypeSetValues", "IX_SubjectGenotypeSetValues_HlaTypingKey_HaplotypeFrequencySetId_AllowedLociKey",
                "HlaTypingKey, HaplotypeFrequencySetId, AllowedLociKey", SequentialKeyInMigrations: false),
            new("DonorSubjectGenotypeSets", "IX_DonorSubjectGenotypeSets_DonorId_AllowedLociKey",
                "DonorId, AllowedLociKey", SequentialKeyInMigrations: true),
        };

        /// <summary>
        /// ATL-495 experiment only. Rebuilds the empty <c>SubjectGenotypeSetValues</c> and <c>DonorSubjectGenotypeSets</c>
        /// tables, so that the inserts of the precomputation go to <paramref name="bucketCount"/> ends of each clustered key,
        /// not one.
        ///
        /// <para>
        /// For each table: a persisted column <c>Bucket = Id % 32</c>. The clustered primary key becomes <c>(Id, Bucket)</c>,
        /// partitioned on <c>Bucket</c>: buckets 0-31 go to <paramref name="bucketCount"/> partitions, each its own B-tree.
        /// The ids of one partition still go up, so each partition is filled at its end, as before. The unique index is not
        /// partitioned, so it still makes its key unique over the whole table. <paramref name="bucketCount"/> 1 puts back the
        /// shape of the migrations: a clustered key on <c>Id</c> only, and no <c>Bucket</c> column.
        /// </para>
        ///
        /// <para>
        /// The reads by <c>Id</c> do not change: without a <c>Bucket</c> predicate, a read seeks every partition. This is
        /// correct but not the fastest, which is good enough for the experiment. The donor rows are read by the unique index.
        /// </para>
        ///
        /// <para>
        /// One transaction: if a statement fails, the tables keep their old shape. Dropping the clustered key moves the heap
        /// to <c>PRIMARY</c>, because a heap keeps the partition scheme otherwise, and the scheme cannot be dropped.
        /// </para>
        /// </summary>
        internal static string BuildReshapeGenotypeSetTablesSql(int bucketCount)
        {
            if (bucketCount < 1 || bucketCount > MaxBucketCount || MaxBucketCount % bucketCount != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bucketCount), bucketCount,
                    $"The bucket count must divide {MaxBucketCount}: 1, 2, 4, 8, 16 or 32.");
            }

            var boundaries = string.Join(", ", Enumerable.Range(1, bucketCount - 1).Select(k => k * MaxBucketCount / bucketCount));

            string ReshapeSql(BucketedTable t)
            {
                var table = $"[dbo].[{t.Name}]";
                var newShapeSql = bucketCount == 1
                    ? $@"
ALTER TABLE {table} ADD CONSTRAINT {t.PrimaryKey} PRIMARY KEY CLUSTERED (Id) WITH (OPTIMIZE_FOR_SEQUENTIAL_KEY = {(t.SequentialKeyInMigrations ? "ON" : "OFF")}) ON [PRIMARY];"
                    : $@"
ALTER TABLE {table} ADD Bucket AS CAST(Id % {MaxBucketCount} AS tinyint) PERSISTED NOT NULL;
CREATE PARTITION FUNCTION {t.PartitionFunction} (tinyint) AS RANGE RIGHT FOR VALUES ({boundaries});
CREATE PARTITION SCHEME {t.PartitionScheme} AS PARTITION {t.PartitionFunction} ALL TO ([PRIMARY]);
ALTER TABLE {table} ADD CONSTRAINT {t.PrimaryKey} PRIMARY KEY CLUSTERED (Id, Bucket) WITH (OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON {t.PartitionScheme} (Bucket);";

                return $@"
DROP INDEX IF EXISTS {t.UniqueIndex} ON {table};
ALTER TABLE {table} DROP CONSTRAINT IF EXISTS {t.PrimaryKey} WITH (MOVE TO [PRIMARY]);
IF EXISTS (SELECT 1 FROM sys.partition_schemes WHERE name = '{t.PartitionScheme}') DROP PARTITION SCHEME {t.PartitionScheme};
IF EXISTS (SELECT 1 FROM sys.partition_functions WHERE name = '{t.PartitionFunction}') DROP PARTITION FUNCTION {t.PartitionFunction};
ALTER TABLE {table} DROP COLUMN IF EXISTS Bucket;{newShapeSql}
CREATE UNIQUE NONCLUSTERED INDEX {t.UniqueIndex} ON {table} ({t.UniqueIndexColumns}) ON [PRIMARY];";
            }

            return $@"
SET XACT_ABORT ON;
BEGIN TRANSACTION;{string.Concat(BucketedTables.Select(ReshapeSql))}
COMMIT TRANSACTION;";
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