using Microsoft.EntityFrameworkCore.Migrations;

namespace Atlas.MatchingAlgorithm.Data.Migrations
{
    /// <summary>
    /// This migration has been manually edited since EF scaffolding:
    ///
    /// - Rename dropped indexes - the existing indexes include "dbo." as part of the table names, but the scaffolded migration assumes this is not the case
    /// - Remove references to index that was created in original EF scaffold, but deleted as part of manual index management
    /// </summary>
    public partial class NormaliseMatchingData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_dbo.MatchingHlaAtA_dbo.PGroupNames_PGroup_Id",
                table: "MatchingHlaAtA");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo.MatchingHlaAtB_dbo.PGroupNames_PGroup_Id",
                table: "MatchingHlaAtB");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo.MatchingHlaAtC_dbo.PGroupNames_PGroup_Id",
                table: "MatchingHlaAtC");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo.MatchingHlaAtDQB1_dbo.PGroupNames_PGroup_Id",
                table: "MatchingHlaAtDQB1");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo.MatchingHlaAtDRB1_dbo.PGroupNames_PGroup_Id",
                table: "MatchingHlaAtDRB1");

            // These indexes did not really exist

            // migrationBuilder.DropIndex(
            //     name: "IX_MatchingHlaAtDRB1_PGroup_Id",
            //     table: "MatchingHlaAtDRB1");
            //
            // migrationBuilder.DropIndex(
            //     name: "IX_MatchingHlaAtDQB1_PGroup_Id",
            //     table: "MatchingHlaAtDQB1");
            //
            // migrationBuilder.DropIndex(
            //     name: "IX_MatchingHlaAtC_PGroup_Id",
            //     table: "MatchingHlaAtC");
            //
            // migrationBuilder.DropIndex(
            //     name: "IX_MatchingHlaAtB_PGroup_Id",
            //     table: "MatchingHlaAtB");
            //
            // migrationBuilder.DropIndex(
            //     name: "IX_MatchingHlaAtA_PGroup_Id",
            //     table: "MatchingHlaAtA");

            // These indexes did exist but were not managed by EF

            migrationBuilder.DropIndex(name: "IX_PGroup_Id_DonorId__TypePosition", table: "MatchingHlaAtDRB1");
            migrationBuilder.DropIndex(name: "IX_PGroup_Id_DonorId__TypePosition", table: "MatchingHlaAtDQB1");
            migrationBuilder.DropIndex(name: "IX_PGroup_Id_DonorId__TypePosition", table: "MatchingHlaAtC");
            migrationBuilder.DropIndex(name: "IX_PGroup_Id_DonorId__TypePosition", table: "MatchingHlaAtB");
            migrationBuilder.DropIndex(name: "IX_PGroup_Id_DonorId__TypePosition", table: "MatchingHlaAtA");
            
            migrationBuilder.DropIndex(name: "IX_DonorId__PGroup_Id_TypePosition", table: "MatchingHlaAtDRB1");
            migrationBuilder.DropIndex(name: "IX_DonorId__PGroup_Id_TypePosition", table: "MatchingHlaAtDQB1");
            migrationBuilder.DropIndex(name: "IX_DonorId__PGroup_Id_TypePosition", table: "MatchingHlaAtC");
            migrationBuilder.DropIndex(name: "IX_DonorId__PGroup_Id_TypePosition", table: "MatchingHlaAtB");
            migrationBuilder.DropIndex(name: "IX_DonorId__PGroup_Id_TypePosition", table: "MatchingHlaAtA");
            
            migrationBuilder.DropColumn(
                name: "PGroup_Id",
                table: "MatchingHlaAtDRB1");

            migrationBuilder.DropColumn(
                name: "PGroup_Id",
                table: "MatchingHlaAtDQB1");

            migrationBuilder.DropColumn(
                name: "PGroup_Id",
                table: "MatchingHlaAtC");

            migrationBuilder.DropColumn(
                name: "PGroup_Id",
                table: "MatchingHlaAtB");

            migrationBuilder.DropColumn(
                name: "PGroup_Id",
                table: "MatchingHlaAtA");

            migrationBuilder.AddColumn<int>(
                name: "HlaNameId",
                table: "MatchingHlaAtDRB1",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HlaNameId",
                table: "MatchingHlaAtDQB1",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HlaNameId",
                table: "MatchingHlaAtC",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HlaNameId",
                table: "MatchingHlaAtB",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HlaNameId",
                table: "MatchingHlaAtA",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "HlaNames",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_HlaNames", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "HlaNamePGroupRelations",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HlaName_Id = table.Column<int>(nullable: true),
                    PGroup_Id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HlaNamePGroupRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HlaNamePGroupRelations_HlaNames_HlaName_Id",
                        column: x => x.HlaName_Id,
                        principalTable: "HlaNames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HlaNamePGroupRelations_PGroupNames_PGroup_Id",
                        column: x => x.PGroup_Id,
                        principalTable: "PGroupNames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "IX_HlaNamePGroupRelations_HlaName_Id",
                table: "HlaNamePGroupRelations",
                column: "HlaName_Id");

            migrationBuilder.CreateIndex(
                name: "IX_HlaNamePGroupRelations_PGroup_Id",
                table: "HlaNamePGroupRelations",
                column: "PGroup_Id");

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.DropTable(
                name: "HlaNamePGroupRelations");

            migrationBuilder.DropTable(
                name: "HlaNames");

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

            migrationBuilder.DropColumn(
                name: "HlaNameId",
                table: "MatchingHlaAtDRB1");

            migrationBuilder.DropColumn(
                name: "HlaNameId",
                table: "MatchingHlaAtDQB1");

            migrationBuilder.DropColumn(
                name: "HlaNameId",
                table: "MatchingHlaAtC");

            migrationBuilder.DropColumn(
                name: "HlaNameId",
                table: "MatchingHlaAtB");

            migrationBuilder.DropColumn(
                name: "HlaNameId",
                table: "MatchingHlaAtA");

            migrationBuilder.AddColumn<int>(
                name: "PGroup_Id",
                table: "MatchingHlaAtDRB1",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PGroup_Id",
                table: "MatchingHlaAtDQB1",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PGroup_Id",
                table: "MatchingHlaAtC",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PGroup_Id",
                table: "MatchingHlaAtB",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PGroup_Id",
                table: "MatchingHlaAtA",
                type: "int",
                nullable: true);

            // migrationBuilder.CreateIndex(
            //     name: "IX_MatchingHlaAtDRB1_PGroup_Id",
            //     table: "MatchingHlaAtDRB1",
            //     column: "PGroup_Id");
            //
            // migrationBuilder.CreateIndex(
            //     name: "IX_MatchingHlaAtDQB1_PGroup_Id",
            //     table: "MatchingHlaAtDQB1",
            //     column: "PGroup_Id");
            //
            // migrationBuilder.CreateIndex(
            //     name: "IX_MatchingHlaAtC_PGroup_Id",
            //     table: "MatchingHlaAtC",
            //     column: "PGroup_Id");
            //
            // migrationBuilder.CreateIndex(
            //     name: "IX_MatchingHlaAtB_PGroup_Id",
            //     table: "MatchingHlaAtB",
            //     column: "PGroup_Id");
            //
            // migrationBuilder.CreateIndex(
            //     name: "IX_MatchingHlaAtA_PGroup_Id",
            //     table: "MatchingHlaAtA",
            //     column: "PGroup_Id");

            migrationBuilder.AddForeignKey(
                name: "FK_dbo.MatchingHlaAtA_dbo.PGroupNames_PGroup_Id",
                table: "MatchingHlaAtA",
                column: "PGroup_Id",
                principalTable: "PGroupNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dbo.MatchingHlaAtB_dbo.PGroupNames_PGroup_Id",
                table: "MatchingHlaAtB",
                column: "PGroup_Id",
                principalTable: "PGroupNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dbo.MatchingHlaAtC_dbo.PGroupNames_PGroup_Id",
                table: "MatchingHlaAtC",
                column: "PGroup_Id",
                principalTable: "PGroupNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dbo.MatchingHlaAtDQB1_dbo.PGroupNames_PGroup_Id",
                table: "MatchingHlaAtDQB1",
                column: "PGroup_Id",
                principalTable: "PGroupNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dbo.MatchingHlaAtDRB1_dbo.PGroupNames_PGroup_Id",
                table: "MatchingHlaAtDRB1",
                column: "PGroup_Id",
                principalTable: "PGroupNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}