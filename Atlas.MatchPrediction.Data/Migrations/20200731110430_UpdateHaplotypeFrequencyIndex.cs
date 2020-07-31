using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Data.Migrations
{
    public partial class UpdateHaplotypeFrequencyIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HaplotypeFrequencies_A_B_C_DQB1_DRB1_Set_Id",
                table: "HaplotypeFrequencies");

            migrationBuilder.CreateIndex(
                name: "IX_HaplotypeFrequencies_A_B_C_DQB1_DRB1_Set_Id",
                table: "HaplotypeFrequencies",
                columns: new[] { "A", "B", "C", "DQB1", "DRB1", "Set_Id" },
                unique: true)
                .Annotation("SqlServer:Include", new[] { "Frequency", "TypingCategory" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HaplotypeFrequencies_A_B_C_DQB1_DRB1_Set_Id",
                table: "HaplotypeFrequencies");

            migrationBuilder.CreateIndex(
                name: "IX_HaplotypeFrequencies_A_B_C_DQB1_DRB1_Set_Id",
                table: "HaplotypeFrequencies",
                columns: new[] { "A", "B", "C", "DQB1", "DRB1", "Set_Id" },
                unique: true)
                .Annotation("SqlServer:Include", new[] { "Frequency" });
        }
    }
}
