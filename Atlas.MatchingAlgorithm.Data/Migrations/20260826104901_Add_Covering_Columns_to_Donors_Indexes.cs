using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchingAlgorithm.Data.Migrations
{
    /// <summary>
    /// ATL-310. Adds covering columns to the two Donors indexes used by the donor join in
    /// DonorSearchRepository.MatchAtLocusSql, so that the join can be satisfied from an index alone. Without
    /// them SQL Server seeks the index and then does a clustered-index lookup for every candidate donor row -
    /// two random reads per row into a table that is tens of GB at live donor volumes - which saturates read IO
    /// and times searches out.
    ///
    /// The index shapes are declared by the EF model, in SearchAlgorithmContext. This migration only applies
    /// them. Its DDL is hand-written, not scaffolded, because it must do two things EF cannot express:
    ///
    /// 1. Skip the work when the covering columns are already there. dbo.Donors holds about 44.6 million rows
    ///    on live, so the rebuild is done ahead of the release by the manual script
    ///    SqlScripts/20260821-ATL-310-AddCoveringColumnsToDonorsIndexes.sql, in this project.
    ///    Where that script has run, this migration must find the work done and change nothing.
    /// 2. Keep the index in service while it is rebuilt. A scaffolded DropIndex plus CreateIndex pair leaves the
    ///    table without the index until the new one is built, and blocks every query on Donors for that whole
    ///    time. See RebuildIndexSql for what is used instead.
    ///
    /// This migration also adopts FI_DonorIdsWithoutLocusC and IX_DonorType__DonorId into the EF model, so that
    /// every index on Donors is now described by the model. Neither needs any DDL - see the note in Up().
    /// </summary>
    public partial class Add_Covering_Columns_to_Donors_Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Every statement here passes suppressTransaction: true. What that flag does depends on how the
            // migration is applied:
            //
            // - Through the EF runtime - Database.Migrate() in the integration and validation test projects,
            //   dotnet ef database update, or a migration bundle - EF puts each migration in its own
            //   transaction. suppressTransaction keeps these index builds out of it, so a long ONLINE build
            //   holds no locks and no transaction log until the migration commits.
            // - From a generated SQL script, which is what the release does - see build-pipeline.yml - the flag
            //   has no effect. A generated script carries no transaction of its own, so the release task that
            //   runs the script is what decides.
            //
            // Neither path needs a transaction. The shape check in RebuildIndexSql makes each statement
            // re-runnable, so if a later statement fails, every statement can simply be run again.

            // Restores the shape this index had before migration 20200424112903 dropped the RegistryCode
            // column. Migration 20240709124454 recreated it without the included column.
            migrationBuilder.Sql(
                RebuildIndexSql("IX_Donors_DonorType_RegistryCode", new[] { "DonorType", "RegistryCode" }, new[] { "DonorId" }),
                suppressTransaction: true);

            // IX_DonorId was created by raw SQL in migration 20190606091538, so it is absent from the old model
            // snapshot and EF cannot scaffold a change to it at all.
            migrationBuilder.Sql(
                RebuildIndexSql("IX_DonorId", new[] { "DonorId" }, new[] { "DonorType", "RegistryCode" }),
                suppressTransaction: true);

            // Note: FI_DonorIdsWithoutLocusC and IX_DonorType__DonorId are new to the model but not to the
            // database, and their existing shapes already match what the model now declares - see migrations
            // 20191114145946 and 20200424112903. EF scaffolded a CreateIndex for each, which would have failed
            // because both already exist; those calls were deliberately removed. Adopting them updates the
            // model snapshot and nothing else, so no rebuild is needed. A database built from scratch still
            // gets both from the earlier migrations.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restores both indexes to the shape they had before this migration: the same keys, no included
            // columns. EF scaffolded a bare drop of IX_DonorId, which would have left the database without an
            // index it has had since 2019.
            migrationBuilder.Sql(
                RebuildIndexSql("IX_Donors_DonorType_RegistryCode", new[] { "DonorType", "RegistryCode" }, Array.Empty<string>()),
                suppressTransaction: true);

            migrationBuilder.Sql(
                RebuildIndexSql("IX_DonorId", new[] { "DonorId" }, Array.Empty<string>()),
                suppressTransaction: true);

            // FI_DonorIdsWithoutLocusC and IX_DonorType__DonorId are intentionally untouched here. Up() made
            // no change to either, so there is nothing to reverse. EF scaffolded drops for both, which would
            // have deleted indexes that existed before this migration; those calls were removed.
        }

        /// <summary>
        /// Builds DDL that brings one index on Donors to the wanted shape, and does nothing when the index is
        /// already in that shape.
        ///
        /// The rebuild uses DROP_EXISTING = ON with ONLINE = ON. SQL Server then builds the new copy of the
        /// index while the old copy still serves queries, and drops the old copy only at the swap, at the end.
        /// The table is never without the index, and the index keeps its name. The key columns do not change,
        /// so the donor data is not sorted again. ONLINE = ON needs Azure SQL, or Enterprise or Developer
        /// edition - as migration 20240709124454 already does.
        ///
        /// The shape check makes the statement re-runnable, and it is what lets the covering columns be applied
        /// out of hours, ahead of the release, by 20260821-ATL-310-AddCoveringColumnsToDonorsIndexes.sql. It
        /// compares the whole included-column list, so a partly applied index is rebuilt rather than passed
        /// over. The script uses the same comparison, so the script and this migration always agree.
        /// </summary>
        /// <param name="indexName">Name of the index, which is also the name the EF model declares.</param>
        /// <param name="keyColumns">Key columns, in key order.</param>
        /// <param name="includedColumns">Included columns. Empty for an index that carries none.</param>
        private static string RebuildIndexSql(string indexName, string[] keyColumns, string[] includedColumns)
        {
            var keyColumnsSql = string.Join(", ", keyColumns.Select(column => $"[{column}]"));
            var includeSql = includedColumns.Any() ? $"INCLUDE ({string.Join(", ", includedColumns.Select(column => $"[{column}]"))}) " : "";

            // Both sides of the comparison are sorted by column name. STRING_AGG returns null, not an empty
            // string, for an index that carries no included columns. No variable is declared, so that the
            // statement stands on its own wherever it is run from - a migration, a generated SQL script, or a
            // query window.
            var expectedIncludedColumns = string.Join(",", includedColumns.OrderBy(column => column, StringComparer.Ordinal));

            return $@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Donors') AND name = '{indexName}')
                    -- Not the expected state on a released database, so there is no old copy to drop.
                    CREATE NONCLUSTERED INDEX [{indexName}] ON [dbo].[Donors] ({keyColumnsSql}) {includeSql}WITH (ONLINE = ON);
                ELSE IF ISNULL((
                        SELECT STRING_AGG(c.name, ',') WITHIN GROUP (ORDER BY c.name)
                        FROM sys.indexes i
                        INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                        INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                        WHERE i.object_id = OBJECT_ID('dbo.Donors')
                            AND i.name = '{indexName}'
                            AND ic.is_included_column = 1), '') <> '{expectedIncludedColumns}'
                    CREATE NONCLUSTERED INDEX [{indexName}] ON [dbo].[Donors] ({keyColumnsSql}) {includeSql}WITH (DROP_EXISTING = ON, ONLINE = ON);
                ";
        }
    }
}
