using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchingAlgorithm.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_Donor_Genotype_Precomputation_Tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DonorGenotypePrecomputationBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunId = table.Column<int>(type: "int", nullable: false),
                    BatchNumber = table.Column<int>(type: "int", nullable: false),
                    FirstGroupId = table.Column<int>(type: "int", nullable: false),
                    LastGroupId = table.Column<int>(type: "int", nullable: false),
                    GroupCount = table.Column<int>(type: "int", nullable: false),
                    DonorAssignmentCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    FailureMessage = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    FailureException = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FailedGroupCount = table.Column<int>(type: "int", nullable: false),
                    LeaseOwner = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseExpiresUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DispatchedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonorGenotypePrecomputationBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DonorGenotypePrecomputationGroupDonors",
                columns: table => new
                {
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    DonorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonorGenotypePrecomputationGroupDonors", x => new { x.GroupId, x.DonorId });
                });

            migrationBuilder.CreateTable(
                name: "DonorGenotypePrecomputationGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    RunId = table.Column<int>(type: "int", nullable: false),
                    AllowedLociKey = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    RepresentativeDonorId = table.Column<int>(type: "int", nullable: false),
                    DonorCount = table.Column<int>(type: "int", nullable: false),
                    SubjectGenotypeSetValueId = table.Column<int>(type: "int", nullable: true),
                    FailureMessage = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonorGenotypePrecomputationGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DonorGenotypePrecomputationRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataRefreshRecordId = table.Column<int>(type: "int", nullable: false),
                    HlaNomenclatureVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    GroupsPerBatch = table.Column<int>(type: "int", nullable: false),
                    TotalGroupCount = table.Column<int>(type: "int", nullable: true),
                    TotalBatchCount = table.Column<int>(type: "int", nullable: true),
                    TotalDonorAssignmentCount = table.Column<int>(type: "int", nullable: true),
                    TotalDonorCount = table.Column<int>(type: "int", nullable: true),
                    ManualRetryCount = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StatusDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonorGenotypePrecomputationRuns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonorGenotypePrecomputationBatches_RunId_BatchNumber",
                table: "DonorGenotypePrecomputationBatches",
                columns: new[] { "RunId", "BatchNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DonorGenotypePrecomputationBatches_RunId_Status",
                table: "DonorGenotypePrecomputationBatches",
                columns: new[] { "RunId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DonorGenotypePrecomputationRuns_DataRefreshRecordId",
                table: "DonorGenotypePrecomputationRuns",
                column: "DataRefreshRecordId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DonorGenotypePrecomputationBatches");

            migrationBuilder.DropTable(
                name: "DonorGenotypePrecomputationGroupDonors");

            migrationBuilder.DropTable(
                name: "DonorGenotypePrecomputationGroups");

            migrationBuilder.DropTable(
                name: "DonorGenotypePrecomputationRuns");
        }
    }
}
