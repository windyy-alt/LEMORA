using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LEMORA.Migrations
{
    /// <inheritdoc />
    public partial class AddOutfitPlanner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutfitPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OutfitId = table.Column<int>(type: "INTEGER", nullable: false),
                    Activity = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutfitPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutfitPlans_Outfits_OutfitId",
                        column: x => x.OutfitId,
                        principalTable: "Outfits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutfitPlans_Date",
                table: "OutfitPlans",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutfitPlans_OutfitId",
                table: "OutfitPlans",
                column: "OutfitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutfitPlans");
        }
    }
}
