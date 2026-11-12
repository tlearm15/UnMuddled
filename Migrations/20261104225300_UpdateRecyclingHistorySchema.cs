using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GroupProject.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRecyclingHistorySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Temporarily commented out because these columns/constraints were already removed from the live DB:
            /*
             *           migrationBuilder.DropForeignKey(
             *               name: "FK_Items_ProductHistories_HistoryID",
             *               table: "Items");
             *
             *           migrationBuilder.DropTable(
             *               name: "ProductHistories");
             *
             *           migrationBuilder.DropIndex(
             *               name: "IX_Items_HistoryID",
             *               table: "Items");
             *
             *           migrationBuilder.DropColumn(
             *               name: "HistoryID",
             *               table: "Items");
             *
             *           migrationBuilder.DropColumn(
             *               name: "HistoryID",
             *               table: "AspNetUsers");
             */

            migrationBuilder.CreateTable(
                name: "UserHistories",
                columns: table => new
                {
                    UserHistoryId = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                                         UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserHistories", x => x.UserHistoryId);
                    table.ForeignKey(
                        name: "FK_UserHistories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserHistoryItems",
                columns: table => new
                {
                    UserHistoryItemId = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                                         DateDisposed = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                                         ItemId = table.Column<int>(type: "integer", nullable: false),
                                         UserHistoryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserHistoryItems", x => x.UserHistoryItemId);
                    table.ForeignKey(
                        name: "FK_UserHistoryItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserHistoryItems_UserHistories_UserHistoryId",
                        column: x => x.UserHistoryId,
                        principalTable: "UserHistories",
                        principalColumn: "UserHistoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserHistories_UserId",
                table: "UserHistories",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserHistoryItems_ItemId",
                table: "UserHistoryItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UserHistoryItems_UserHistoryId",
                table: "UserHistoryItems",
                column: "UserHistoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserHistoryItems");

            migrationBuilder.DropTable(
                name: "UserHistories");

            migrationBuilder.AddColumn<int>(
                name: "HistoryID",
                table: "Items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HistoryID",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductHistories",
                columns: table => new
                {
                    HistoryID = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                                         UserID = table.Column<string>(type: "text", nullable: false),
                                         DateDisposed = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductHistories", x => x.HistoryID);
                    table.ForeignKey(
                        name: "FK_ProductHistories_AspNetUsers_UserID",
                        column: x => x.UserID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_HistoryID",
                table: "Items",
                column: "HistoryID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductHistories_UserID",
                table: "ProductHistories",
                column: "UserID",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Items_ProductHistories_HistoryID",
                table: "Items",
                column: "HistoryID",
                principalTable: "ProductHistories",
                principalColumn: "HistoryID");
        }
    }
}
