using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchingAlgorithm.Data.Migrations
{
    /// <summary>
    /// ATL-472. Sets <c>OPTIMIZE_FOR_SEQUENTIAL_KEY = ON</c> on the clustered primary keys of the two genotype set
    /// tables that the precomputation stage of the data refresh writes.
    ///
    /// Both keys are identity columns, so every new row goes on the last page of the table, and all the workers that
    /// insert at once wait for that one page (<c>PAGELATCH_EX</c>). The option changes how SQL Server lets the
    /// inserting threads in, so that throughput holds up under that contention. It does not change the data or the
    /// index shape.
    ///
    /// Hand-written, not scaffolded: EF Core has no way to declare this option on an index, so the model and the
    /// snapshot do not change. <c>ALTER INDEX ... SET</c> changes only metadata: it does not rebuild the index, so it is
    /// quick on a full table. The data refresh empties these tables with <c>TRUNCATE</c>, which keeps the option.
    /// </summary>
    public partial class Optimize_For_Sequential_Key_On_Genotype_Set_Primary_Keys : Migration
    {
        private static readonly string[] PrimaryKeys =
        {
            "[PK_SubjectGenotypeSetValues] ON [dbo].[SubjectGenotypeSetValues]",
            "[PK_DonorSubjectGenotypeSets] ON [dbo].[DonorSubjectGenotypeSets]",
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var primaryKey in PrimaryKeys)
            {
                migrationBuilder.Sql($"ALTER INDEX {primaryKey} SET (OPTIMIZE_FOR_SEQUENTIAL_KEY = ON);");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var primaryKey in PrimaryKeys)
            {
                migrationBuilder.Sql($"ALTER INDEX {primaryKey} SET (OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);");
            }
        }
    }
}
