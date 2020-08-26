using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    public partial class ModifySimulantIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Simulants_TestHarness_Id",
                table: "Simulants");

            migrationBuilder.DropIndex(
                name: "IX_Simulants_TestIndividualCategory_SimulatedHlaTypingCategory",
                table: "Simulants");

            migrationBuilder.CreateIndex(
                name: "IX_Simulants_TestHarness_Id_TestIndividualCategory_SimulatedHlaTypingCategory",
                table: "Simulants",
                columns: new[] { "TestHarness_Id", "TestIndividualCategory", "SimulatedHlaTypingCategory" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Simulants_TestHarness_Id_TestIndividualCategory_SimulatedHlaTypingCategory",
                table: "Simulants");

            migrationBuilder.CreateIndex(
                name: "IX_Simulants_TestHarness_Id",
                table: "Simulants",
                column: "TestHarness_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Simulants_TestIndividualCategory_SimulatedHlaTypingCategory",
                table: "Simulants",
                columns: new[] { "TestIndividualCategory", "SimulatedHlaTypingCategory" });
        }
    }
}
