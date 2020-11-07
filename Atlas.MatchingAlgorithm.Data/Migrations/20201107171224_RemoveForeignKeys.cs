using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchingAlgorithm.Data.Migrations
{
    public partial class RemoveForeignKeys : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HlaNamePGroupRelationAtA_HlaNames_HlaName_Id",
                table: "HlaNamePGroupRelationAtA");

            migrationBuilder.DropForeignKey(
                name: "FK_HlaNamePGroupRelationAtA_PGroupNames_PGroup_Id",
                table: "HlaNamePGroupRelationAtA");

            migrationBuilder.DropForeignKey(
                name: "FK_HlaNamePGroupRelationAtB_HlaNames_HlaName_Id",
                table: "HlaNamePGroupRelationAtB");

            migrationBuilder.DropForeignKey(
                name: "FK_HlaNamePGroupRelationAtB_PGroupNames_PGroup_Id",
                table: "HlaNamePGroupRelationAtB");

            migrationBuilder.DropForeignKey(
                name: "FK_HlaNamePGroupRelationAtC_HlaNames_HlaName_Id",
                table: "HlaNamePGroupRelationAtC");

            migrationBuilder.DropForeignKey(
                name: "FK_HlaNamePGroupRelationAtC_PGroupNames_PGroup_Id",
                table: "HlaNamePGroupRelationAtC");

            migrationBuilder.DropForeignKey(
                name: "FK_HlaNamePGroupRelationAtDQB1_HlaNames_HlaName_Id",
                table: "HlaNamePGroupRelationAtDQB1");

            migrationBuilder.DropForeignKey(
                name: "FK_HlaNamePGroupRelationAtDQB1_PGroupNames_PGroup_Id",
                table: "HlaNamePGroupRelationAtDQB1");

            migrationBuilder.DropForeignKey(
                name: "FK_HlaNamePGroupRelationAtDRB1_HlaNames_HlaName_Id",
                table: "HlaNamePGroupRelationAtDRB1");

            migrationBuilder.DropForeignKey(
                name: "FK_HlaNamePGroupRelationAtDRB1_PGroupNames_PGroup_Id",
                table: "HlaNamePGroupRelationAtDRB1");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchingHlaAtA_Donors_DonorId",
                table: "MatchingHlaAtA");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchingHlaAtA_HlaNames_HlaNameId",
                table: "MatchingHlaAtA");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchingHlaAtB_Donors_DonorId",
                table: "MatchingHlaAtB");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchingHlaAtB_HlaNames_HlaNameId",
                table: "MatchingHlaAtB");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchingHlaAtC_Donors_DonorId",
                table: "MatchingHlaAtC");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchingHlaAtC_HlaNames_HlaNameId",
                table: "MatchingHlaAtC");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchingHlaAtDQB1_Donors_DonorId",
                table: "MatchingHlaAtDQB1");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchingHlaAtDQB1_HlaNames_HlaNameId",
                table: "MatchingHlaAtDQB1");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchingHlaAtDRB1_Donors_DonorId",
                table: "MatchingHlaAtDRB1");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchingHlaAtDRB1_HlaNames_HlaNameId",
                table: "MatchingHlaAtDRB1");

            migrationBuilder.DropIndex(
                name: "IX_MatchingHlaAtDRB1_DonorId",
                table: "MatchingHlaAtDRB1");

            migrationBuilder.DropIndex(
                name: "IX_MatchingHlaAtDRB1_HlaNameId",
                table: "MatchingHlaAtDRB1");

            migrationBuilder.DropIndex(
                name: "IX_MatchingHlaAtDQB1_DonorId",
                table: "MatchingHlaAtDQB1");

            migrationBuilder.DropIndex(
                name: "IX_MatchingHlaAtDQB1_HlaNameId",
                table: "MatchingHlaAtDQB1");

            migrationBuilder.DropIndex(
                name: "IX_MatchingHlaAtC_DonorId",
                table: "MatchingHlaAtC");

            migrationBuilder.DropIndex(
                name: "IX_MatchingHlaAtC_HlaNameId",
                table: "MatchingHlaAtC");

            migrationBuilder.DropIndex(
                name: "IX_MatchingHlaAtB_DonorId",
                table: "MatchingHlaAtB");

            migrationBuilder.DropIndex(
                name: "IX_MatchingHlaAtB_HlaNameId",
                table: "MatchingHlaAtB");

            migrationBuilder.DropIndex(
                name: "IX_MatchingHlaAtA_DonorId",
                table: "MatchingHlaAtA");

            migrationBuilder.DropIndex(
                name: "IX_MatchingHlaAtA_HlaNameId",
                table: "MatchingHlaAtA");

            migrationBuilder.DropIndex(
                name: "IX_HlaNamePGroupRelationAtDRB1_HlaName_Id",
                table: "HlaNamePGroupRelationAtDRB1");

            migrationBuilder.DropIndex(
                name: "IX_HlaNamePGroupRelationAtDRB1_PGroup_Id",
                table: "HlaNamePGroupRelationAtDRB1");

            migrationBuilder.DropIndex(
                name: "IX_HlaNamePGroupRelationAtDQB1_HlaName_Id",
                table: "HlaNamePGroupRelationAtDQB1");

            migrationBuilder.DropIndex(
                name: "IX_HlaNamePGroupRelationAtDQB1_PGroup_Id",
                table: "HlaNamePGroupRelationAtDQB1");

            migrationBuilder.DropIndex(
                name: "IX_HlaNamePGroupRelationAtC_HlaName_Id",
                table: "HlaNamePGroupRelationAtC");

            migrationBuilder.DropIndex(
                name: "IX_HlaNamePGroupRelationAtC_PGroup_Id",
                table: "HlaNamePGroupRelationAtC");

            migrationBuilder.DropIndex(
                name: "IX_HlaNamePGroupRelationAtB_HlaName_Id",
                table: "HlaNamePGroupRelationAtB");

            migrationBuilder.DropIndex(
                name: "IX_HlaNamePGroupRelationAtB_PGroup_Id",
                table: "HlaNamePGroupRelationAtB");

            migrationBuilder.DropIndex(
                name: "IX_HlaNamePGroupRelationAtA_HlaName_Id",
                table: "HlaNamePGroupRelationAtA");

            migrationBuilder.DropIndex(
                name: "IX_HlaNamePGroupRelationAtA_PGroup_Id",
                table: "HlaNamePGroupRelationAtA");

            migrationBuilder.DropColumn(
                name: "HlaName_Id",
                table: "HlaNamePGroupRelationAtDRB1");

            migrationBuilder.DropColumn(
                name: "PGroup_Id",
                table: "HlaNamePGroupRelationAtDRB1");

            migrationBuilder.DropColumn(
                name: "HlaName_Id",
                table: "HlaNamePGroupRelationAtDQB1");

            migrationBuilder.DropColumn(
                name: "PGroup_Id",
                table: "HlaNamePGroupRelationAtDQB1");

            migrationBuilder.DropColumn(
                name: "HlaName_Id",
                table: "HlaNamePGroupRelationAtC");

            migrationBuilder.DropColumn(
                name: "PGroup_Id",
                table: "HlaNamePGroupRelationAtC");

            migrationBuilder.DropColumn(
                name: "HlaName_Id",
                table: "HlaNamePGroupRelationAtB");

            migrationBuilder.DropColumn(
                name: "PGroup_Id",
                table: "HlaNamePGroupRelationAtB");

            migrationBuilder.DropColumn(
                name: "HlaName_Id",
                table: "HlaNamePGroupRelationAtA");

            migrationBuilder.DropColumn(
                name: "PGroup_Id",
                table: "HlaNamePGroupRelationAtA");

            migrationBuilder.AddColumn<int>(
                name: "HlaNameId",
                table: "HlaNamePGroupRelationAtDRB1",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PGroupId",
                table: "HlaNamePGroupRelationAtDRB1",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HlaNameId",
                table: "HlaNamePGroupRelationAtDQB1",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PGroupId",
                table: "HlaNamePGroupRelationAtDQB1",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HlaNameId",
                table: "HlaNamePGroupRelationAtC",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PGroupId",
                table: "HlaNamePGroupRelationAtC",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HlaNameId",
                table: "HlaNamePGroupRelationAtB",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PGroupId",
                table: "HlaNamePGroupRelationAtB",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HlaNameId",
                table: "HlaNamePGroupRelationAtA",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PGroupId",
                table: "HlaNamePGroupRelationAtA",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HlaNameId",
                table: "HlaNamePGroupRelationAtDRB1");

            migrationBuilder.DropColumn(
                name: "PGroupId",
                table: "HlaNamePGroupRelationAtDRB1");

            migrationBuilder.DropColumn(
                name: "HlaNameId",
                table: "HlaNamePGroupRelationAtDQB1");

            migrationBuilder.DropColumn(
                name: "PGroupId",
                table: "HlaNamePGroupRelationAtDQB1");

            migrationBuilder.DropColumn(
                name: "HlaNameId",
                table: "HlaNamePGroupRelationAtC");

            migrationBuilder.DropColumn(
                name: "PGroupId",
                table: "HlaNamePGroupRelationAtC");

            migrationBuilder.DropColumn(
                name: "HlaNameId",
                table: "HlaNamePGroupRelationAtB");

            migrationBuilder.DropColumn(
                name: "PGroupId",
                table: "HlaNamePGroupRelationAtB");

            migrationBuilder.DropColumn(
                name: "HlaNameId",
                table: "HlaNamePGroupRelationAtA");

            migrationBuilder.DropColumn(
                name: "PGroupId",
                table: "HlaNamePGroupRelationAtA");

            migrationBuilder.AddColumn<int>(
                name: "HlaName_Id",
                table: "HlaNamePGroupRelationAtDRB1",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PGroup_Id",
                table: "HlaNamePGroupRelationAtDRB1",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HlaName_Id",
                table: "HlaNamePGroupRelationAtDQB1",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PGroup_Id",
                table: "HlaNamePGroupRelationAtDQB1",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HlaName_Id",
                table: "HlaNamePGroupRelationAtC",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PGroup_Id",
                table: "HlaNamePGroupRelationAtC",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HlaName_Id",
                table: "HlaNamePGroupRelationAtB",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PGroup_Id",
                table: "HlaNamePGroupRelationAtB",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HlaName_Id",
                table: "HlaNamePGroupRelationAtA",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PGroup_Id",
                table: "HlaNamePGroupRelationAtA",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchingHlaAtDRB1_DonorId",
                table: "MatchingHlaAtDRB1",
                column: "DonorId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingHlaAtDRB1_HlaNameId",
                table: "MatchingHlaAtDRB1",
                column: "HlaNameId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingHlaAtDQB1_DonorId",
                table: "MatchingHlaAtDQB1",
                column: "DonorId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingHlaAtDQB1_HlaNameId",
                table: "MatchingHlaAtDQB1",
                column: "HlaNameId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingHlaAtC_DonorId",
                table: "MatchingHlaAtC",
                column: "DonorId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingHlaAtC_HlaNameId",
                table: "MatchingHlaAtC",
                column: "HlaNameId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingHlaAtB_DonorId",
                table: "MatchingHlaAtB",
                column: "DonorId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingHlaAtB_HlaNameId",
                table: "MatchingHlaAtB",
                column: "HlaNameId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingHlaAtA_DonorId",
                table: "MatchingHlaAtA",
                column: "DonorId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingHlaAtA_HlaNameId",
                table: "MatchingHlaAtA",
                column: "HlaNameId");

            migrationBuilder.CreateIndex(
                name: "IX_HlaNamePGroupRelationAtDRB1_HlaName_Id",
                table: "HlaNamePGroupRelationAtDRB1",
                column: "HlaName_Id");

            migrationBuilder.CreateIndex(
                name: "IX_HlaNamePGroupRelationAtDRB1_PGroup_Id",
                table: "HlaNamePGroupRelationAtDRB1",
                column: "PGroup_Id");

            migrationBuilder.CreateIndex(
                name: "IX_HlaNamePGroupRelationAtDQB1_HlaName_Id",
                table: "HlaNamePGroupRelationAtDQB1",
                column: "HlaName_Id");

            migrationBuilder.CreateIndex(
                name: "IX_HlaNamePGroupRelationAtDQB1_PGroup_Id",
                table: "HlaNamePGroupRelationAtDQB1",
                column: "PGroup_Id");

            migrationBuilder.CreateIndex(
                name: "IX_HlaNamePGroupRelationAtC_HlaName_Id",
                table: "HlaNamePGroupRelationAtC",
                column: "HlaName_Id");

            migrationBuilder.CreateIndex(
                name: "IX_HlaNamePGroupRelationAtC_PGroup_Id",
                table: "HlaNamePGroupRelationAtC",
                column: "PGroup_Id");

            migrationBuilder.CreateIndex(
                name: "IX_HlaNamePGroupRelationAtB_HlaName_Id",
                table: "HlaNamePGroupRelationAtB",
                column: "HlaName_Id");

            migrationBuilder.CreateIndex(
                name: "IX_HlaNamePGroupRelationAtB_PGroup_Id",
                table: "HlaNamePGroupRelationAtB",
                column: "PGroup_Id");

            migrationBuilder.CreateIndex(
                name: "IX_HlaNamePGroupRelationAtA_HlaName_Id",
                table: "HlaNamePGroupRelationAtA",
                column: "HlaName_Id");

            migrationBuilder.CreateIndex(
                name: "IX_HlaNamePGroupRelationAtA_PGroup_Id",
                table: "HlaNamePGroupRelationAtA",
                column: "PGroup_Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HlaNamePGroupRelationAtA_HlaNames_HlaName_Id",
                table: "HlaNamePGroupRelationAtA",
                column: "HlaName_Id",
                principalTable: "HlaNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HlaNamePGroupRelationAtA_PGroupNames_PGroup_Id",
                table: "HlaNamePGroupRelationAtA",
                column: "PGroup_Id",
                principalTable: "PGroupNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HlaNamePGroupRelationAtB_HlaNames_HlaName_Id",
                table: "HlaNamePGroupRelationAtB",
                column: "HlaName_Id",
                principalTable: "HlaNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HlaNamePGroupRelationAtB_PGroupNames_PGroup_Id",
                table: "HlaNamePGroupRelationAtB",
                column: "PGroup_Id",
                principalTable: "PGroupNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HlaNamePGroupRelationAtC_HlaNames_HlaName_Id",
                table: "HlaNamePGroupRelationAtC",
                column: "HlaName_Id",
                principalTable: "HlaNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HlaNamePGroupRelationAtC_PGroupNames_PGroup_Id",
                table: "HlaNamePGroupRelationAtC",
                column: "PGroup_Id",
                principalTable: "PGroupNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HlaNamePGroupRelationAtDQB1_HlaNames_HlaName_Id",
                table: "HlaNamePGroupRelationAtDQB1",
                column: "HlaName_Id",
                principalTable: "HlaNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HlaNamePGroupRelationAtDQB1_PGroupNames_PGroup_Id",
                table: "HlaNamePGroupRelationAtDQB1",
                column: "PGroup_Id",
                principalTable: "PGroupNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HlaNamePGroupRelationAtDRB1_HlaNames_HlaName_Id",
                table: "HlaNamePGroupRelationAtDRB1",
                column: "HlaName_Id",
                principalTable: "HlaNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HlaNamePGroupRelationAtDRB1_PGroupNames_PGroup_Id",
                table: "HlaNamePGroupRelationAtDRB1",
                column: "PGroup_Id",
                principalTable: "PGroupNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchingHlaAtA_Donors_DonorId",
                table: "MatchingHlaAtA",
                column: "DonorId",
                principalTable: "Donors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchingHlaAtA_HlaNames_HlaNameId",
                table: "MatchingHlaAtA",
                column: "HlaNameId",
                principalTable: "HlaNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchingHlaAtB_Donors_DonorId",
                table: "MatchingHlaAtB",
                column: "DonorId",
                principalTable: "Donors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchingHlaAtB_HlaNames_HlaNameId",
                table: "MatchingHlaAtB",
                column: "HlaNameId",
                principalTable: "HlaNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchingHlaAtC_Donors_DonorId",
                table: "MatchingHlaAtC",
                column: "DonorId",
                principalTable: "Donors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchingHlaAtC_HlaNames_HlaNameId",
                table: "MatchingHlaAtC",
                column: "HlaNameId",
                principalTable: "HlaNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchingHlaAtDQB1_Donors_DonorId",
                table: "MatchingHlaAtDQB1",
                column: "DonorId",
                principalTable: "Donors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchingHlaAtDQB1_HlaNames_HlaNameId",
                table: "MatchingHlaAtDQB1",
                column: "HlaNameId",
                principalTable: "HlaNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchingHlaAtDRB1_Donors_DonorId",
                table: "MatchingHlaAtDRB1",
                column: "DonorId",
                principalTable: "Donors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchingHlaAtDRB1_HlaNames_HlaNameId",
                table: "MatchingHlaAtDRB1",
                column: "HlaNameId",
                principalTable: "HlaNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
