using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.MatchingAlgorithm.Data.Persistent.Migrations
{
    /// <inheritdoc />
    public partial class Add_Donor_Genotype_Precomputation_Completed_to_Data_Refresh_Record : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DonorGenotypePrecomputationCompleted",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DonorGenotypePrecomputationCompleted",
                schema: "MatchingAlgorithmPersistent",
                table: "DataRefreshHistory");
        }
    }
}
