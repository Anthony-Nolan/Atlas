using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchingAlgorithm.Data.Migrations
{
    /// <summary>
    /// ATL-495. Splits <c>SubjectGenotypeSetValues</c> and <c>DonorSubjectGenotypeSets</c> into 16 hash partitions, so
    /// that the inserts of the precomputation stage of the data refresh go to 16 ends of each clustered key, not one.
    ///
    /// <para>
    /// Each table gets a persisted column <c>Bucket = Id % 16</c>. The clustered primary key becomes <c>(Id, Bucket)</c>,
    /// partitioned on <c>Bucket</c>, so each bucket is its own B-tree. The ids of one bucket still go up, so each bucket is
    /// filled at its end: no page splits in the middle, and no extra log. <c>OPTIMIZE_FOR_SEQUENTIAL_KEY</c> is off on
    /// both keys: with 16 ends, the inserts do not need it, and on <c>DonorSubjectGenotypeSets</c> it let the inserts in a
    /// few at a time. The unique indexes are not partitioned, so each still makes its key unique over the whole table.
    /// </para>
    ///
    /// <para>
    /// <b>Empties both tables first.</b> Rebuilding a full table would move hundreds of GB on the active database. The
    /// rows are a store of computed results: every data refresh wipes and refills them, and search computes a donor with
    /// no row live. Before 4.1.0 nothing writes to these tables, so this deletes nothing outside test environments.
    /// </para>
    ///
    /// <para>
    /// Hand-written, not scaffolded: EF Core has no way to declare a partitioned key, so the model and the snapshot do not
    /// change. <c>Bucket</c> is read only by hand-written SQL (<c>SubjectGenotypeSetRepository</c>). Down puts back the
    /// shape of the earlier migrations, and empties both tables too.
    /// </para>
    /// </summary>
    public partial class Hash_Partition_Genotype_Set_Tables : Migration
    {
        private const string PartitionFunction = "PF_GenotypeSetBucket";
        private const string PartitionScheme = "PS_GenotypeSetBucket";

        private record Table(string Name, string UniqueIndex, string UniqueIndexColumns, bool SequentialKeyBefore);

        private static readonly Table[] Tables =
        {
            new("SubjectGenotypeSetValues", "IX_SubjectGenotypeSetValues_HlaTypingKey_HaplotypeFrequencySetId_AllowedLociKey",
                "HlaTypingKey, HaplotypeFrequencySetId, AllowedLociKey", SequentialKeyBefore: false),
            new("DonorSubjectGenotypeSets", "IX_DonorSubjectGenotypeSets_DonorId_AllowedLociKey",
                "DonorId, AllowedLociKey", SequentialKeyBefore: true),
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
CREATE PARTITION FUNCTION {PartitionFunction} (tinyint) AS RANGE RIGHT FOR VALUES (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
CREATE PARTITION SCHEME {PartitionScheme} AS PARTITION {PartitionFunction} ALL TO ([PRIMARY]);");

            foreach (var table in Tables)
            {
                migrationBuilder.Sql($@"
TRUNCATE TABLE [dbo].[{table.Name}];
DROP INDEX [{table.UniqueIndex}] ON [dbo].[{table.Name}];
ALTER TABLE [dbo].[{table.Name}] DROP CONSTRAINT [PK_{table.Name}];
ALTER TABLE [dbo].[{table.Name}] ADD [Bucket] AS CAST([Id] % 16 AS tinyint) PERSISTED NOT NULL;
ALTER TABLE [dbo].[{table.Name}] ADD CONSTRAINT [PK_{table.Name}] PRIMARY KEY CLUSTERED ([Id], [Bucket])
    WITH (OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON {PartitionScheme} ([Bucket]);
CREATE UNIQUE NONCLUSTERED INDEX [{table.UniqueIndex}] ON [dbo].[{table.Name}] ({table.UniqueIndexColumns}) ON [PRIMARY];");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                // MOVE TO: without it, the heap left by dropping the clustered key stays on the partition scheme, and the
                // scheme cannot be dropped.
                migrationBuilder.Sql($@"
TRUNCATE TABLE [dbo].[{table.Name}];
DROP INDEX [{table.UniqueIndex}] ON [dbo].[{table.Name}];
ALTER TABLE [dbo].[{table.Name}] DROP CONSTRAINT [PK_{table.Name}] WITH (MOVE TO [PRIMARY]);
ALTER TABLE [dbo].[{table.Name}] DROP COLUMN [Bucket];
ALTER TABLE [dbo].[{table.Name}] ADD CONSTRAINT [PK_{table.Name}] PRIMARY KEY CLUSTERED ([Id])
    WITH (OPTIMIZE_FOR_SEQUENTIAL_KEY = {(table.SequentialKeyBefore ? "ON" : "OFF")}) ON [PRIMARY];
CREATE UNIQUE NONCLUSTERED INDEX [{table.UniqueIndex}] ON [dbo].[{table.Name}] ({table.UniqueIndexColumns}) ON [PRIMARY];");
            }

            migrationBuilder.Sql($@"
DROP PARTITION SCHEME {PartitionScheme};
DROP PARTITION FUNCTION {PartitionFunction};");
        }
    }
}
