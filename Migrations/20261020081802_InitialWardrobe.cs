using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LEMORA.Migrations
{
    /// <inheritdoc />
    public partial class InitialWardrobe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClothingItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Brand = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ColorName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ColorHex = table.Column<string>(type: "TEXT", nullable: false),
                    ImagePath = table.Column<string>(type: "TEXT", nullable: true),
                    OccasionTags = table.Column<string>(type: "TEXT", nullable: false),
                    SeasonTags = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClothingItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClothingItems_Category",
                table: "ClothingItems",
                column: "Category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClothingItems");
        }
    }
}
