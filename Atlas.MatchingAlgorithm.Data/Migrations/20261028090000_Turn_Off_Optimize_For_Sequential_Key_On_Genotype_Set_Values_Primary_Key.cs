using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchingAlgorithm.Data.Migrations
{
    /// <summary>
    /// ATL-491. Sets <c>OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF</c> on the clustered primary key of
    /// <c>SubjectGenotypeSetValues</c>, which ATL-472 set to ON.
    ///
    /// With the option on, SQL Server lets the inserts into the last page of the key in one at a time
    /// (<c>BTREE_INSERT_FLOW_CONTROL</c>). After ATL-473 removed the lock waits, this became the limit of the
    /// precomputation stage of the data refresh: more concurrent workers did not make it faster, and the database had
    /// CPU and log write to spare. <c>PK_DonorSubjectGenotypeSets</c> keeps the option.
    ///
    /// Hand-written, not scaffolded: EF Core has no way to declare this option on an index, so the model and the
    /// snapshot do not change. <c>ALTER INDEX ... SET</c> changes only metadata, so it is quick on a full table.
    /// </summary>
    public partial class Turn_Off_Optimize_For_Sequential_Key_On_Genotype_Set_Values_Primary_Key : Migration
    {
        private const string PrimaryKey = "[PK_SubjectGenotypeSetValues] ON [dbo].[SubjectGenotypeSetValues]";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"ALTER INDEX {PrimaryKey} SET (OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"ALTER INDEX {PrimaryKey} SET (OPTIMIZE_FOR_SEQUENTIAL_KEY = ON);");
        }
    }
}
