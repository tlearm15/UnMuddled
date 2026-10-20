using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupProject.Migrations
{
    /// <inheritdoc />
    public partial class TQRequestCreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Items_ItemID",
                table: "Requests");

            migrationBuilder.AlterColumn<int>(
                name: "ItemID",
                table: "Requests",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "CategoryID",
                table: "Requests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Requests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                table: "Requests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecyclingLocations",
                table: "Requests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Requests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedOn",
                table: "Requests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Requests_CategoryID",
                table: "Requests",
                column: "CategoryID");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Categories_CategoryID",
                table: "Requests",
                column: "CategoryID",
                principalTable: "Categories",
                principalColumn: "CategoryID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Items_ItemID",
                table: "Requests",
                column: "ItemID",
                principalTable: "Items",
                principalColumn: "ItemID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Categories_CategoryID",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Items_ItemID",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_CategoryID",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "CategoryID",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ItemName",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "RecyclingLocations",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "SubmittedOn",
                table: "Requests");

            migrationBuilder.AlterColumn<int>(
                name: "ItemID",
                table: "Requests",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Items_ItemID",
                table: "Requests",
                column: "ItemID",
                principalTable: "Items",
                principalColumn: "ItemID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
