using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Data.Migrations
{
    public partial class UpdateMpaIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HaplotypeFrequencies_Set_Id",
                schema: "MatchPrediction",
                table: "HaplotypeFrequencies");

            migrationBuilder.CreateIndex(
                name: "IX_HaplotypeFrequencies_Set_Id",
                schema: "MatchPrediction",
                table: "HaplotypeFrequencies",
                column: "Set_Id")
                .Annotation("SqlServer:Include", new[] { "Id", "A", "B", "C", "DQB1", "DRB1", "Frequency", "TypingCategory" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HaplotypeFrequencies_Set_Id",
                schema: "MatchPrediction",
                table: "HaplotypeFrequencies");

            migrationBuilder.CreateIndex(
                name: "IX_HaplotypeFrequencies_Set_Id",
                schema: "MatchPrediction",
                table: "HaplotypeFrequencies",
                column: "Set_Id");
        }
    }
}
