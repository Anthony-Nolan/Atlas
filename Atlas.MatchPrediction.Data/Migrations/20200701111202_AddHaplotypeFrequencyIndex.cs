using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchPrediction.Data.Migrations
{
    public partial class AddHaplotypeFrequencyIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HaplotypeFrequencies_HaplotypeFrequencySets_Set_Id",
                table: "HaplotypeFrequencies");

            migrationBuilder.AlterColumn<int>(
                name: "Set_Id",
                table: "HaplotypeFrequencies",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HaplotypeFrequencies_A_B_C_DQB1_DRB1_Set_Id",
                table: "HaplotypeFrequencies",
                columns: new[] { "A", "B", "C", "DQB1", "DRB1", "Set_Id" })
                .Annotation("SqlServer:Include", new[] { "Frequency" });

            migrationBuilder.AddForeignKey(
                name: "FK_HaplotypeFrequencies_HaplotypeFrequencySets_Set_Id",
                table: "HaplotypeFrequencies",
                column: "Set_Id",
                principalTable: "HaplotypeFrequencySets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HaplotypeFrequencies_HaplotypeFrequencySets_Set_Id",
                table: "HaplotypeFrequencies");

            migrationBuilder.DropIndex(
                name: "IX_HaplotypeFrequencies_A_B_C_DQB1_DRB1_Set_Id",
                table: "HaplotypeFrequencies");

            migrationBuilder.AlterColumn<int>(
                name: "Set_Id",
                table: "HaplotypeFrequencies",
                type: "int",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AddForeignKey(
                name: "FK_HaplotypeFrequencies_HaplotypeFrequencySets_Set_Id",
                table: "HaplotypeFrequencies",
                column: "Set_Id",
                principalTable: "HaplotypeFrequencySets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
